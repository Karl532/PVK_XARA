using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Orchestrates sculpt guide rendering across point, mesh, and between passes.
/// </summary>
public class SdfSculptGuideRenderer : MonoBehaviour, ISdfRenderer
{
    [Header("Rendering")]
    [SerializeField] private int maxPointRenderCount = 50000;

    private SdfSculptGuidePointRenderer _pointRenderer;
    // Between renderer removed.
    private readonly SdfSculptGuideDepthMeshRenderer _depthMeshRenderer = new SdfSculptGuideDepthMeshRenderer();
    private readonly SdfSculptGuideMaterials _materials = new SdfSculptGuideMaterials();

    private bool _initialized;
    private Matrix4x4 _worldToWorkspace = Matrix4x4.identity;
    private Vector3 _workspaceSize = Vector3.one;
    private Vector3 _workspaceCorner;
    private Vector3 _lastWorkspaceCorner;
    private Vector3 _lastWorkspaceSize;

    private SdfVolumeData _global;
    private SculptGuideSettings _settings = SculptGuideSettings.Default;
    private DepthFrameData _depthFrame;
    private bool _hasDepthFrame;

    private void Awake()
    {
        EnsureInitialized();
        CalibrationOriginUtility.AttachToOrigin(transform, worldPositionStays: true);
        if (_pointRenderer != null)
            _pointRenderer.CacheCleared += OnCacheCleared;
    }

    private void OnEnable()
    {
        EnsureInitialized();
        SdfDebug.LogEvery(
            "SdfSculptGuideRenderer.OnEnable",
            $"[SG_DEBUG] [SdfSculptGuideRenderer] OnEnable init={_initialized}",
            1f,
            this);
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    private void OnDestroy()
    {
        if (_pointRenderer != null)
            _pointRenderer.CacheCleared -= OnCacheCleared;
        _materials.Release();
        _pointRenderer?.Release();
        _depthMeshRenderer.Release();
    }

    public void UpdateVisualizationData(SdfVisualizationData data, SculptGuideSettings settings)
    {
        SdfDebug.LogEvery(
            "SdfSculptGuideRenderer.UpdateVisualizationData",
            $"[SG_DEBUG] [SdfSculptGuideRenderer] UpdateVisualizationData globalValid={data.Global.IsValid} mesh={settings.MeshEnabled} points={settings.RenderPoints} cache={settings.EnableCache} between={settings.BetweenEnabled}",
            1f,
            this);
        UpdateState(data, settings);
        _pointRenderer?.UpdateVisualizationData(data, settings, _worldToWorkspace);
    }

    public void UpdatePointCloud(ComputeBuffer points, int count)
    {
        _pointRenderer?.UpdatePointCloud(points, count, _settings);
        SdfDebug.LogEvery(
            "SdfSculptGuideRenderer.UpdatePointCloud",
            $"[SG_DEBUG] [SdfSculptGuideRenderer] UpdatePointCloud count={count} bufferNull={(points == null)}",
            1f,
            this);
    }

    public void UpdateDepthFrame(DepthFrameData depthFrame)
    {
        _depthFrame = depthFrame;
        _hasDepthFrame = depthFrame.DepthTexture != null && depthFrame.DepthResolution.x > 0 && depthFrame.DepthResolution.y > 0;
        SdfDebug.LogEvery(
            "SdfSculptGuideRenderer.UpdateDepthFrame",
            $"[SG_DEBUG] [SdfSculptGuideRenderer] UpdateDepthFrame valid={_hasDepthFrame} res={depthFrame.DepthResolution} texNull={(depthFrame.DepthTexture == null)}",
            1f,
            this);
    }

    public void UpdateRenderer(in SdfRendererContext context)
    {
        var settings = Settings.GetActive();
        var config = settings != null ? settings.sdfVisualizationConfig : null;
        if (settings == null || config == null)
        {
            enabled = false;
            return;
        }

        enabled = settings.sdfRenderSculptGuide;
        if (!enabled)
            return;

        var data = context.Data;
        var volume = data.Global;
        if (!volume.IsValid || data.WorkspaceRoot == null)
        {
            enabled = false;
            SdfDebug.LogEvery(
                "SdfSculptGuideRenderer.MissingData",
                "[SdfSculptGuideRenderer] Sculpt guide skipped: missing volume or workspace.",
                1f,
                this);
            return;
        }

        var sculptSettings = SculptGuideSettings.FromConfig(config, 4);
        SdfDebug.LogEvery(
            "SdfSculptGuideRenderer.Settings",
            $"[SG_DEBUG] [SdfSculptGuideRenderer] SculptGuide settings: mesh={sculptSettings.MeshEnabled} points={sculptSettings.RenderPoints} cache={sculptSettings.EnableCache} between={sculptSettings.BetweenEnabled}",
            1f,
            this);

        UpdateVisualizationData(data, sculptSettings);

        if (context.HasPointCloud)
            UpdatePointCloud(context.PointCloud.pointBuffer, context.PointCloud.pointCount);
        if (context.HasDepthFrame)
            UpdateDepthFrame(context.DepthFrame);

        if (!context.HasPointCloud)
        {
            SdfDebug.LogEvery(
                "SdfSculptGuideRenderer.NoPoints",
                "[SdfSculptGuideRenderer] Sculpt guide waiting for point cloud.",
                1f,
                this);
        }
    }

    private void OnCacheCleared()
    {
        _depthMeshRenderer.Reset();
        SdfDebug.Log("[SdfSculptGuideRenderer] Cleared cache due to workspace movement.", this);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        Shader cachePointsShader = Shader.Find("Hidden/SdfSculptGuideCachePoints");
        cachePointsShader ??= Resources.Load<Shader>("SDF/Shaders/SdfSculptGuideCachePoints");
        Shader pointCloudShader = Shader.Find("Hidden/SdfSculptGuidePointCloud");
        pointCloudShader ??= Resources.Load<Shader>("SDF/Shaders/SdfSculptGuidePointCloud");
        Shader meshShader = Shader.Find("Hidden/SdfSculptGuideDepthMesh");
        meshShader ??= Resources.Load<Shader>("SDF/Shaders/SdfSculptGuideDepthMesh");
        if (!_materials.EnsureMaterials(cachePointsShader, pointCloudShader, null, meshShader))
        {
            Debug.LogError("[SdfSculptGuideRenderer] Missing sculpt guide shaders.");
            enabled = false;
            return;
        }

        _pointRenderer = new SdfSculptGuidePointRenderer(maxPointRenderCount);
        _pointRenderer.CacheCleared += OnCacheCleared;

        SdfDebug.LogEvery(
            "SdfSculptGuideRenderer.Initialized",
            "[SG_DEBUG] [SdfSculptGuideRenderer] Initialized.",
            1f,
            this);
        _initialized = true;
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        SdfDebug.LogEvery(
            "SdfSculptGuideRenderer.BeginCamera",
            $"[SG_DEBUG] [SdfSculptGuideRenderer] BeginCamera cam={(camera != null ? camera.name : "null")} enabled={enabled} init={_initialized} " +
            $"globalValid={_global.IsValid} points={(_pointRenderer != null ? _pointRenderer.PointCount : 0)} depthFrame={_hasDepthFrame} mesh={_settings.MeshEnabled} " +
            $"renderPoints={_settings.RenderPoints} cache={_settings.EnableCache} between={_settings.BetweenEnabled}",
            1f,
            this);

        if (!HasRenderData())
        {
            SdfDebug.LogEvery(
                "SdfSculptGuideRenderer.MissingData",
                "[SdfSculptGuideRenderer] Missing data for rendering.",
                1f,
                this);
            return;
        }

        _pointRenderer?.Render(
            context,
            camera,
            _settings,
            _global,
            _worldToWorkspace,
            _materials.PointCloudMaterial,
            _materials.CachePointsMaterial,
            _depthFrame,
            _hasDepthFrame);

        // Between rendering disabled.

        if (_settings.MeshEnabled && _materials.MeshMaterial != null && _hasDepthFrame)
        {
            _depthMeshRenderer.Render(
                camera,
                _depthFrame,
                _settings,
                _global,
                _materials.MeshMaterial,
                _worldToWorkspace);
        }
    }

    private bool HasRenderData()
    {
        if (!_initialized || !_global.IsValid)
            return false;

        bool hasPoints = _pointRenderer != null && _pointRenderer.HasPoints;
        bool hasCache = _pointRenderer != null && _pointRenderer.HasCache(_settings);
        bool hasBetween = false;
        bool hasMesh = _settings.MeshEnabled && _hasDepthFrame;
        return hasPoints || hasCache || hasBetween || hasMesh;
    }

    private void UpdateState(SdfVisualizationData data, SculptGuideSettings settings)
    {
        _global = data.Global;
        _worldToWorkspace = data.WorkspaceRoot != null ? data.WorkspaceRoot.worldToLocalMatrix : Matrix4x4.identity;
        _workspaceSize = data.WorkspaceSize;
        _workspaceCorner = data.WorkspaceCorner;
        _settings = settings;
        if (data.WorkspaceRoot != null && _settings.EnableCache && _pointRenderer != null)
            _pointRenderer.MarkCacheDirtyIfWorkspaceMoved(data.WorkspaceRoot);

        if (_lastWorkspaceCorner != _workspaceCorner || _lastWorkspaceSize != _workspaceSize)
        {
            _lastWorkspaceCorner = _workspaceCorner;
            _lastWorkspaceSize = _workspaceSize;
            _pointRenderer?.Reset();
            _depthMeshRenderer.Reset();
        }
    }
}
