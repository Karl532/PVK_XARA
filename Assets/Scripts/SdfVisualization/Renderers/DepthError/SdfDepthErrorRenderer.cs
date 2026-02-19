using UnityEngine;
using UnityEngine.Rendering;

public sealed class SdfDepthErrorRenderer : SdfRendererBase
{
    [Header("Rendering")]
    [SerializeField] private Shader depthErrorShader;

    private Material _material;
    private bool _initialized;

    private SdfVolumeData _global;
    private Matrix4x4 _worldToWorkspace = Matrix4x4.identity;
    private DepthFrameData _depthFrame;
    private bool _hasDepthFrame;
    private SdfDepthErrorSettings _settings = SdfDepthErrorSettings.Default;

    private void Awake()
    {
        EnsureInitialized();
        CalibrationOriginUtility.AttachToOrigin(transform, worldPositionStays: true);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EnsureInitialized();
    }

    private void OnDestroy()
    {
        if (_material != null)
        {
            Destroy(_material);
            _material = null;
        }
    }

    public void UpdateData(SdfVisualizationData data, DepthFrameData depthFrame, SdfDepthErrorSettings settings)
    {
        _global = data.Global;
        _worldToWorkspace = data.WorkspaceRoot != null ? data.WorkspaceRoot.worldToLocalMatrix : Matrix4x4.identity;
        _settings = settings;
        _depthFrame = depthFrame;
        _hasDepthFrame = depthFrame.DepthTexture != null && depthFrame.DepthResolution.x > 0 && depthFrame.DepthResolution.y > 0;
    }

    public override void UpdateRenderer(in SdfRendererContext context)
    {
        if (!TryResolveSettings())
        {
            enabled = false;
            return;
        }

        var depthSettings = SdfDepthErrorSettings.FromConfig(Config);
        enabled = depthSettings.Enabled;
        if (!enabled)
            return;

        var data = context.Data;
        if (data.Global.IsValid && data.WorkspaceRoot != null && context.HasDepthFrame)
            UpdateData(data, context.DepthFrame, depthSettings);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        if (!depthErrorShader)
            depthErrorShader = Shader.Find("Hidden/SdfDepthError");
        if (!depthErrorShader)
            depthErrorShader = Resources.Load<Shader>("SDF/Shaders/SdfDepthError");

        if (!depthErrorShader)
        {
            Debug.LogError("[SdfDepthErrorRenderer] Missing SdfDepthError shader.");
            enabled = false;
            return;
        }

        _material = new Material(depthErrorShader)
        {
            name = "SdfDepthErrorMaterial"
        };

        _initialized = true;
    }

    protected override void OnRenderCamera(ScriptableRenderContext context, Camera camera)
    {
        if (!_initialized || !_settings.Enabled)
            return;

        if (!_global.IsValid || !_hasDepthFrame || camera == null)
            return;

        int step = Mathf.Max(1, _settings.Step);
        int gridW = Mathf.Max(2, _depthFrame.DepthResolution.x / step);
        int gridH = Mathf.Max(2, _depthFrame.DepthResolution.y / step);
        int cellCount = (gridW - 1) * (gridH - 1);
        int vertexCount = cellCount * 6;
        if (vertexCount <= 0)
            return;

        _material.SetTexture(SdfDepthErrorShaderIds.DepthTex, _depthFrame.DepthTexture);
        _material.SetVector(SdfDepthErrorShaderIds.DepthSize, new Vector4(_depthFrame.DepthResolution.x, _depthFrame.DepthResolution.y, 0f, 0f));
        _material.SetInt(SdfDepthErrorShaderIds.EyeSlice, _depthFrame.EyeSlice);
        _material.SetInt(SdfDepthErrorShaderIds.FlipY, _depthFrame.FlipY ? 1 : 0);
        _material.SetInt(SdfDepthErrorShaderIds.Step, step);
        _material.SetFloat(SdfDepthErrorShaderIds.Alpha, _settings.Alpha);
        _material.SetFloat(SdfDepthErrorShaderIds.ErrorScale, Mathf.Max(1e-4f, _settings.ErrorScale));
        _material.SetFloat(SdfDepthErrorShaderIds.RayStep, Mathf.Max(1e-4f, _settings.RayStepWorld));
        _material.SetFloat(SdfDepthErrorShaderIds.MaxDistance, Mathf.Max(0.01f, _settings.MaxDistance));
        _material.SetInt(SdfDepthErrorShaderIds.MaxSteps, Mathf.Max(1, _settings.MaxSteps));
        _material.SetFloat(SdfDepthErrorShaderIds.HitThreshold, Mathf.Max(1e-4f, _settings.HitThreshold));
        _material.SetMatrix(SdfDepthErrorShaderIds.InvDepthViewProj, _depthFrame.InvDepthViewProj);
        _material.SetMatrix(SdfDepthErrorShaderIds.TrackingToWorld, _depthFrame.TrackingToWorld);
        _material.SetMatrix(SdfDepthErrorShaderIds.WorldToWorkspace, _worldToWorkspace);
        _material.SetVector(SdfDepthErrorShaderIds.CameraOriginWS, _depthFrame.TrackingToWorld.MultiplyPoint3x4(Vector3.zero));
        _material.SetTexture(SdfDepthErrorShaderIds.GlobalTsdf3D, _global.Tsdf);
        _material.SetVector(SdfDepthErrorShaderIds.GlobalCorner, _global.Corner);
        _material.SetVector(SdfDepthErrorShaderIds.GlobalSize, _global.Size);
        _material.SetFloat(SdfDepthErrorShaderIds.GlobalMu, _global.Mu);

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
