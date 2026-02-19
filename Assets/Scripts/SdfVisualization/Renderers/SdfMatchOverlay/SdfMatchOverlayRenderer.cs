using System.Collections.Generic;
using Assets.Scripts.Debug;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class SdfMatchOverlayRenderer : SdfRendererBase
{
    [Header("Shaders")]
    [SerializeField] private ComputeShader matchCompute;
    [SerializeField] private Shader overlayShader;

    private Material _overlayMaterial;
    private RenderTexture _maskTex;
    private Vector2Int _depthSize;
    private int _kClear2D = -1;
    private int _kBuild2D = -1;

    private SdfMatchOverlaySettings _settings = SdfMatchOverlaySettings.Default;
    private SdfVolumeData _global;
    private Matrix4x4 _worldToWorkspace = Matrix4x4.identity;
    private Vector3 _workspaceCorner;
    private Vector3 _workspaceSize;
    private Vector3 _lastWorkspaceCorner;
    private Vector3 _lastWorkspaceSize;
    private Matrix4x4 _lastWorldToWorkspace = Matrix4x4.identity;

    private DepthFrameData _depthFrame;
    private bool _hasDepthFrame;

    private Transform _modelRoot;
    private readonly List<Renderer> _modelRenderers = new List<Renderer>();

    private void Awake()
    {
        EnsureInitialized();
        CalibrationOriginUtility.AttachToOrigin(transform, worldPositionStays: true);
    }

    private void OnDestroy()
    {
        if (_overlayMaterial != null)
        {
            Destroy(_overlayMaterial);
            _overlayMaterial = null;
        }

        if (_maskTex != null)
        {
            _maskTex.Release();
            _maskTex = null;
        }
    }

    public void UpdateData(SdfVisualizationData data, SdfMatchOverlaySettings settings)
    {
        _settings = settings;
        _global = data.Global;
        _worldToWorkspace = data.WorkspaceRoot != null ? data.WorkspaceRoot.worldToLocalMatrix : Matrix4x4.identity;
        _workspaceCorner = data.WorkspaceCorner;
        _workspaceSize = data.WorkspaceSize;

        if (_settings.ResetOnMove)
        {
            if (_workspaceCorner != _lastWorkspaceCorner ||
                _workspaceSize != _lastWorkspaceSize ||
                _worldToWorkspace != _lastWorldToWorkspace)
            {
                ClearMask();
                _lastWorkspaceCorner = _workspaceCorner;
                _lastWorkspaceSize = _workspaceSize;
                _lastWorldToWorkspace = _worldToWorkspace;
            }
        }

        if (!_settings.Enabled || !_global.IsValid)
        {
            RemoveOverlayMaterial();
            return;
        }

        EnsureMask();
        EnsureModelOverlay();
        UpdateMask();
    }

    public void UpdateDepthFrame(DepthFrameData depthFrame)
    {
        _depthFrame = depthFrame;
        _depthSize = depthFrame.DepthResolution;
        _hasDepthFrame = depthFrame.DepthTexture != null && depthFrame.DepthResolution.x > 0 && depthFrame.DepthResolution.y > 0;
        DebugService.LogEvery(
            "SdfMatchOverlayRenderer.DepthFrame",
            $"[SdfMatchOverlayRenderer] DepthFrame valid={_hasDepthFrame} res={_depthSize} texNull={(_depthFrame.DepthTexture == null)}",
            1f,
            this);
    }

    public override void UpdateRenderer(in SdfRendererContext context)
    {
        if (!TryResolveSettings())
        {
            enabled = false;
            return;
        }

        var matchSettings = SdfMatchOverlaySettings.FromConfig(Config);
        enabled = matchSettings.Enabled;
        if (!enabled)
            return;

        var data = context.Data;
        if (data.Global.IsValid && data.WorkspaceRoot != null)
        {
            if (context.HasDepthFrame)
                UpdateDepthFrame(context.DepthFrame);
            if (!_hasDepthFrame)
                return;
            UpdateData(data, matchSettings);
        }
    }

    protected override void OnRenderCamera(ScriptableRenderContext context, Camera camera)
    {
        // Match overlay updates are driven by UpdateRenderer; no per-camera work needed.
    }

    private void EnsureInitialized()
    {
        if (matchCompute == null)
            matchCompute = Resources.Load<ComputeShader>("SDF/Compute/Visualization/SdfMatchOverlay");

        if (overlayShader == null)
            overlayShader = Shader.Find("Hidden/SdfMatchOverlay");
        if (overlayShader == null)
            overlayShader = Resources.Load<Shader>("SDF/Shaders/SdfMatchOverlay");

        if (matchCompute != null)
        {
            _kClear2D = matchCompute.FindKernel("CSClear2D");
            _kBuild2D = matchCompute.FindKernel("CSBuildMask2D");
        }

        if (_overlayMaterial == null && overlayShader != null)
        {
            _overlayMaterial = new Material(overlayShader)
            {
                name = "SdfMatchOverlayMaterial"
            };
        }
    }

    private void EnsureMask()
    {
        if (!_hasDepthFrame)
            return;

        if (_maskTex != null && _maskTex.width == _depthSize.x && _maskTex.height == _depthSize.y)
            return;

        if (_maskTex != null)
            _maskTex.Release();

        var desc = new RenderTextureDescriptor(_depthSize.x, _depthSize.y, RenderTextureFormat.RFloat, 0)
        {
            dimension = TextureDimension.Tex2D,
            enableRandomWrite = true
        };
        _maskTex = new RenderTexture(desc)
        {
            name = "SdfMatchOverlayMask",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        _maskTex.Create();

        ClearMask();
    }

    private void ClearMask()
    {
        if (matchCompute == null || _kClear2D < 0 || _maskTex == null)
            return;

        matchCompute.SetVector("_DepthSize", new Vector4(_depthSize.x, _depthSize.y, 0f, 0f));
        matchCompute.SetTexture(_kClear2D, "_MatchMask2D", _maskTex);
        int gx = Mathf.CeilToInt(_depthSize.x / 8f);
        int gy = Mathf.CeilToInt(_depthSize.y / 8f);
        matchCompute.Dispatch(_kClear2D, gx, gy, 1);
    }

    private void UpdateMask()
    {
        if (matchCompute == null || _maskTex == null)
            return;
        if (!_hasDepthFrame)
            return;

        ClearMask();

        if (_kBuild2D < 0)
            return;

        matchCompute.SetTexture(_kBuild2D, "_MatchMask2D", _maskTex);
        matchCompute.SetTexture(_kBuild2D, "_GlobalTsdf3D", _global.Tsdf);
        matchCompute.SetTexture(_kBuild2D, "_DepthTex", _depthFrame.DepthTexture);
        matchCompute.SetVector("_DepthSize", new Vector4(_depthSize.x, _depthSize.y, 0f, 0f));
        matchCompute.SetInt("_EyeSlice", _depthFrame.EyeSlice);
        matchCompute.SetInt("_FlipY", _depthFrame.FlipY ? 1 : 0);
        matchCompute.SetMatrix("_InvDepthViewProj", _depthFrame.InvDepthViewProj);
        matchCompute.SetMatrix("_TrackingToWorld", _depthFrame.TrackingToWorld);
        matchCompute.SetMatrix("_WorldToWorkspace", _worldToWorkspace);
        matchCompute.SetVector("_GlobalCorner", _global.Corner);
        matchCompute.SetVector("_GlobalSize", _global.Size);
        matchCompute.SetFloat("_Tolerance", _settings.Tolerance);

        int gx = Mathf.CeilToInt(_depthSize.x / 8f);
        int gy = Mathf.CeilToInt(_depthSize.y / 8f);
        matchCompute.Dispatch(_kBuild2D, gx, gy, 1);
    }

    private void EnsureModelOverlay()
    {
        if (_overlayMaterial == null)
            return;

        var root = FindModelRoot();
        if (root == null)
            return;

        if (_modelRoot != root)
        {
            _modelRoot = root;
            _modelRenderers.Clear();
            _modelRoot.GetComponentsInChildren(true, _modelRenderers);
            ApplyOverlayMaterial();
        }

        UpdateOverlayMaterial();
    }

    private void ApplyOverlayMaterial()
    {
        if (_overlayMaterial == null)
            return;

        for (int i = 0; i < _modelRenderers.Count; i++)
        {
            var r = _modelRenderers[i];
            if (r == null)
                continue;

            var mats = r.sharedMaterials;
            bool already = false;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] == _overlayMaterial)
                {
                    already = true;
                    break;
                }
            }

            if (already)
                continue;

            var newMats = new Material[mats.Length + 1];
            for (int m = 0; m < mats.Length; m++)
                newMats[m] = mats[m];
            newMats[mats.Length] = _overlayMaterial;
            r.sharedMaterials = newMats;
        }
    }

    private void RemoveOverlayMaterial()
    {
        if (_modelRoot == null)
            return;

        for (int i = 0; i < _modelRenderers.Count; i++)
        {
            var r = _modelRenderers[i];
            if (r == null)
                continue;

            var mats = r.sharedMaterials;
            int count = 0;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] != _overlayMaterial)
                    count++;
            }

            if (count == mats.Length)
                continue;

            var newMats = new Material[count];
            int idx = 0;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] == _overlayMaterial)
                    continue;
                newMats[idx++] = mats[m];
            }
            r.sharedMaterials = newMats;
        }
    }

    private void UpdateOverlayMaterial()
    {
        _overlayMaterial.SetTexture("_MatchMask2D", _maskTex);
        _overlayMaterial.SetMatrix("_WorldToTracking", _depthFrame.TrackingToWorld.inverse);
        _overlayMaterial.SetMatrix("_DepthViewProj", _depthFrame.InvDepthViewProj.inverse);
        _overlayMaterial.SetVector("_DepthSize", new Vector4(_depthSize.x, _depthSize.y, 0f, 0f));
        _overlayMaterial.SetFloat("_DepthFlipY", _depthFrame.FlipY ? 1f : 0f);
        _overlayMaterial.SetColor("_MatchColor", _settings.MatchColor);
        _overlayMaterial.SetFloat("_MatchAlpha", _settings.Alpha);
        _overlayMaterial.SetFloat("_MatchSoftness", _settings.Softness);
    }

    private static Transform FindModelRoot()
    {
        var go = GameObject.Find("RuntimeModel");
        return go != null ? go.transform : null;
    }
}
