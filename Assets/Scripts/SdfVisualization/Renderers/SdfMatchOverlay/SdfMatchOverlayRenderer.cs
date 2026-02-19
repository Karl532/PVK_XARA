using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class SdfMatchOverlayRenderer : MonoBehaviour, ISdfRenderer
{
    [Header("Shaders")]
    [SerializeField] private ComputeShader matchCompute;
    [SerializeField] private Shader overlayShader;

    private Material _overlayMaterial;
    private RenderTexture _maskTex;
    private int _resolution;
    private int _kClear = -1;
    private int _kDecay = -1;
    private int _kAccumulate = -1;

    private SdfMatchOverlaySettings _settings = SdfMatchOverlaySettings.Default;
    private SdfVolumeData _global;
    private Matrix4x4 _worldToWorkspace = Matrix4x4.identity;
    private Vector3 _workspaceCorner;
    private Vector3 _workspaceSize;

    private ComputeBuffer _pointBuffer;
    private int _pointCount;

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

        if (!_settings.Enabled || !_global.IsValid)
        {
            RemoveOverlayMaterial();
            return;
        }

        EnsureMask(_settings.Resolution, _workspaceCorner, _workspaceSize);
        EnsureModelOverlay();
        UpdateMask();
    }

    public void UpdatePointCloud(ComputeBuffer points, int count)
    {
        _pointBuffer = points;
        _pointCount = Mathf.Max(0, count);
    }

    public void UpdateRenderer(in SdfRendererContext context)
    {
        var settings = Settings.GetActive();
        var config = settings != null ? settings.sdfVisualizationConfig : null;
        if (config == null)
        {
            enabled = false;
            return;
        }

        var matchSettings = SdfMatchOverlaySettings.FromConfig(config);
        enabled = matchSettings.Enabled;
        if (!enabled)
            return;

        var data = context.Data;
        if (data.Global.IsValid && data.WorkspaceRoot != null && context.HasPointCloud)
        {
            UpdateData(data, matchSettings);
            UpdatePointCloud(context.PointCloud.pointBuffer, context.PointCloud.pointCount);
        }
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
            _kClear = matchCompute.FindKernel("CSClear");
            _kDecay = matchCompute.FindKernel("CSDecay");
            _kAccumulate = matchCompute.FindKernel("CSAccumulate");
        }

        if (_overlayMaterial == null && overlayShader != null)
        {
            _overlayMaterial = new Material(overlayShader)
            {
                name = "SdfMatchOverlayMaterial"
            };
        }
    }

    private void EnsureMask(int resolution, Vector3 corner, Vector3 size)
    {
        if (_maskTex != null && _resolution == resolution)
            return;

        if (_maskTex != null)
            _maskTex.Release();

        var desc = new RenderTextureDescriptor(resolution, resolution, RenderTextureFormat.RFloat, 0)
        {
            dimension = TextureDimension.Tex3D,
            volumeDepth = resolution,
            enableRandomWrite = true
        };
        _maskTex = new RenderTexture(desc)
        {
            name = "SdfMatchOverlayMask",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        _maskTex.Create();
        _resolution = resolution;

        ClearMask();
    }

    private void ClearMask()
    {
        if (matchCompute == null || _kClear < 0 || _maskTex == null)
            return;

        matchCompute.SetInt("_Resolution", _resolution);
        matchCompute.SetTexture(_kClear, "_MatchMask", _maskTex);
        int g = Mathf.CeilToInt(_resolution / 8f);
        matchCompute.Dispatch(_kClear, g, g, g);
    }

    private void UpdateMask()
    {
        if (matchCompute == null || _maskTex == null)
            return;
        if (_pointBuffer == null || _pointCount <= 0)
            return;

        if (_settings.Decay < 0.999f && _kDecay >= 0)
        {
            matchCompute.SetInt("_Resolution", _resolution);
            matchCompute.SetFloat("_Decay", Mathf.Clamp01(_settings.Decay));
            matchCompute.SetTexture(_kDecay, "_MatchMask", _maskTex);
            int g = Mathf.CeilToInt(_resolution / 8f);
            matchCompute.Dispatch(_kDecay, g, g, g);
        }

        if (_kAccumulate < 0)
            return;

        matchCompute.SetTexture(_kAccumulate, "_MatchMask", _maskTex);
        matchCompute.SetTexture(_kAccumulate, "_GlobalTsdf3D", _global.Tsdf);
        matchCompute.SetBuffer(_kAccumulate, "_Points", _pointBuffer);
        matchCompute.SetInt("_PointCount", _pointCount);
        matchCompute.SetInt("_Resolution", _resolution);
        matchCompute.SetMatrix("_WorldToWorkspace", _worldToWorkspace);
        matchCompute.SetVector("_WorkspaceCorner", _workspaceCorner);
        matchCompute.SetVector("_WorkspaceSize", _workspaceSize);
        matchCompute.SetVector("_GlobalCorner", _global.Corner);
        matchCompute.SetVector("_GlobalSize", _global.Size);
        matchCompute.SetFloat("_Tolerance", _settings.Tolerance);

        int gx = Mathf.CeilToInt(_pointCount / 256f);
        matchCompute.Dispatch(_kAccumulate, gx, 1, 1);
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
        _overlayMaterial.SetTexture("_MatchMask", _maskTex);
        _overlayMaterial.SetVector("_MatchCorner", _workspaceCorner);
        _overlayMaterial.SetVector("_MatchSize", _workspaceSize);
        _overlayMaterial.SetMatrix("_WorldToWorkspace", _worldToWorkspace);
        _overlayMaterial.SetColor("_MatchColor", _settings.MatchColor);
        _overlayMaterial.SetFloat("_MatchAlpha", _settings.Alpha);
    }

    private static Transform FindModelRoot()
    {
        var go = GameObject.Find("RuntimeModel");
        return go != null ? go.transform : null;
    }
}
