using UnityEngine;
using UnityEngine.Rendering;

public sealed class RayHelperRenderer : MonoBehaviour, ISdfRenderer
{
    [Header("Rendering")]
    [SerializeField] private Shader depthErrorShader;

    private Material _material;
    private bool _initialized;

    private SdfVolumeData _global;
    private Matrix4x4 _worldToWorkspace = Matrix4x4.identity;
    private float _worldToWorkspaceScale = 1f;
    private DepthFrameData _depthFrame;
    private bool _hasDepthFrame;
    private RayHelperSettings _settings = RayHelperSettings.Default;

    SdfVisualizationData _data;

    private void Awake()
    {
        EnsureInitialized();
        CalibrationOriginUtility.AttachToOrigin(transform, worldPositionStays: true);
    }

    private void OnEnable()
    {
        EnsureInitialized();
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    private void OnDestroy()
    {
        if (_material != null)
        {
            Destroy(_material);
            _material = null;
        }
    }

    public void UpdateData(SdfVisualizationData data, DepthFrameData depthFrame, RayHelperSettings settings)
    {
        _global = data.Global;
        _worldToWorkspace = data.WorkspaceRoot != null ? data.WorkspaceRoot.worldToLocalMatrix : Matrix4x4.identity;
        //_worldToWorkspace = data.UnscaledWorldToWorkspaceMatrix;

        _worldToWorkspaceScale = 1f;
        if (data.WorkspaceRoot != null)
        {
            Vector3 lossy = data.WorkspaceRoot.lossyScale;
            float avg = (Mathf.Abs(lossy.x) + Mathf.Abs(lossy.y) + Mathf.Abs(lossy.z)) / 3f;
            if (avg > 1e-6f)
                _worldToWorkspaceScale = 1f / avg;
        }
        _settings = settings;
        _depthFrame = depthFrame;
        _hasDepthFrame = depthFrame.DepthTexture != null && depthFrame.DepthResolution.x > 0 && depthFrame.DepthResolution.y > 0;
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

        var depthSettings = RayHelperSettings.FromConfig(config);
        enabled = depthSettings.Enabled;
        if (!enabled)
            return;

        _data = context.Data;
        if (_data.Global.IsValid && _data.WorkspaceRoot != null && context.HasDepthFrame)
            UpdateData(_data, context.DepthFrame, depthSettings);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        if (!depthErrorShader)
            depthErrorShader = Shader.Find("Hidden/RayHelper");
        if (!depthErrorShader)
            depthErrorShader = Resources.Load<Shader>("SDF/Shaders/RayHelper");

        if (!depthErrorShader)
        {
            Debug.LogError("[RayHelperRenderer] Missing RayHelper shader.");
            enabled = false;
            return;
        }

        _material = new Material(depthErrorShader)
        {
            name = "RayHelperMaterial"
        };

        _initialized = true;
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!_initialized || !_settings.Enabled)
            return;

        if (!_global.IsValid || !_hasDepthFrame || camera == null)
            return;

        if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.VR)
            return;

        int step = Mathf.Max(1, _settings.Step);
        int gridW = Mathf.Max(2, _depthFrame.DepthResolution.x / step);
        int gridH = Mathf.Max(2, _depthFrame.DepthResolution.y / step);
        int cellCount = (gridW - 1) * (gridH - 1);
        int vertexCount = cellCount * 6;
        if (vertexCount <= 0)
            return;

        _material.SetTexture(RayHelperShaderIds.DepthTex, _depthFrame.DepthTexture);
        _material.SetVector(RayHelperShaderIds.DepthSize, new Vector4(_depthFrame.DepthResolution.x, _depthFrame.DepthResolution.y, 0f, 0f));
        _material.SetInt(RayHelperShaderIds.EyeSlice, _depthFrame.EyeSlice);
        _material.SetInt(RayHelperShaderIds.FlipY, _depthFrame.FlipY ? 1 : 0);
        _material.SetInt(RayHelperShaderIds.Step, step);
        _material.SetFloat(RayHelperShaderIds.Alpha, _settings.Alpha);
        _material.SetFloat(RayHelperShaderIds.MinDepth01, Mathf.Clamp01(_depthFrame.MinDepth01));
        _material.SetFloat(RayHelperShaderIds.MaxDepth01, Mathf.Clamp01(_depthFrame.MaxDepth01));
        _material.SetFloat(RayHelperShaderIds.ErrorScale, Mathf.Max(1e-4f, _settings.ErrorScale));
        _material.SetFloat(RayHelperShaderIds.RayStep, Mathf.Max(1e-4f, _settings.RayStepWorld));
        _material.SetFloat(RayHelperShaderIds.MaxDistance, Mathf.Max(0.01f, _settings.MaxDistance));
        _material.SetInt(RayHelperShaderIds.MaxSteps, Mathf.Max(1, _settings.MaxSteps));
        _material.SetFloat(RayHelperShaderIds.HitThreshold, Mathf.Max(1e-4f, _settings.HitThreshold));
        _material.SetFloat(RayHelperShaderIds.WorldToWorkspaceScale, _worldToWorkspaceScale);
        _material.SetMatrix(RayHelperShaderIds.InvDepthViewProj, _depthFrame.InvDepthViewProj);
        _material.SetMatrix(RayHelperShaderIds.TrackingToWorld, _depthFrame.TrackingToWorld);
        _material.SetMatrix(RayHelperShaderIds.WorldToWorkspace, _worldToWorkspace);
        // Use the current camera position for ray origin to avoid fixed-origin raymarching.
        _material.SetVector(RayHelperShaderIds.CameraOriginWS, camera.transform.position);
        _material.SetTexture(RayHelperShaderIds.GlobalTsdf3D, _global.Tsdf);
        _material.SetVector(RayHelperShaderIds.GlobalCorner, _global.Corner);

        //var scale_thing = _global.Size;
        Vector3 lossyScale = _data.WorkspaceRoot.lossyScale;
        Vector3 scale_thing = new Vector3(
            _global.Size.x / Mathf.Max(lossyScale.x, 1e-6f),
            _global.Size.y / Mathf.Max(lossyScale.y, 1e-6f),
            _global.Size.z / Mathf.Max(lossyScale.z, 1e-6f)
        );
        //_material.SetVector(RayHelperShaderIds.WorkspaceLocalSize, scale_thing);

        _material.SetVector(RayHelperShaderIds.GlobalSize, _global.Size);
        _material.SetFloat(RayHelperShaderIds.GlobalMu, _global.Mu);

        // FIX: Calculate the unscaled "Local" bounds so the shader knows exactly how big 
        // the physical rendering box should be without clipping.
        //Vector3 lossyScale = _data.WorkspaceRoot != null ? _data.WorkspaceRoot.lossyScale : new Vector3(347895634.53f, 593487.3f, 2497.7f);
        Vector3 localCorner = new Vector3(
            _global.Corner.x / Mathf.Max(Mathf.Abs(lossyScale.x), 1e-6f),
            _global.Corner.y / Mathf.Max(Mathf.Abs(lossyScale.y), 1e-6f),
            _global.Corner.z / Mathf.Max(Mathf.Abs(lossyScale.z), 1e-6f)
        );
        Vector3 localSize = new Vector3(
            _global.Size.x / Mathf.Max(Mathf.Abs(lossyScale.x), 1e-6f),
            _global.Size.y / Mathf.Max(Mathf.Abs(lossyScale.y), 1e-6f),
            _global.Size.z / Mathf.Max(Mathf.Abs(lossyScale.z), 1e-6f)
        );

        _material.SetVector("_WorkspaceLocalCorner", localCorner);
        _material.SetVector("_WorkspaceLocalSize", localSize);


        Bounds bounds = new Bounds(camera.transform.position, Vector3.one * 2000f);
        Graphics.DrawProcedural(
            _material,
            bounds,
            MeshTopology.Triangles,
            vertexCount,
            1,
            camera);
    }
}
