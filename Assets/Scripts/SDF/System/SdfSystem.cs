using UnityEngine;

/// <summary>
/// Orchestrates:
/// - converting world-space points -> workspace space
/// - converting model mesh -> triangle space in workspace
/// - building local/global TSDF volumes via Core (black box)
/// - optional visualization (slice + overlay)
///
/// ASSUMPTIONS (current project):
/// 1) Point cloud buffer positions are in Unity WORLD space (OXDepth uses _TrackingToWorld).
/// 2) Workspace space is PlacementBlock.localSpace (workspaceRoot).
/// 3) PlacementBlock pivot is centered (Unity cube), so workspaceCornerWS = -size/2.
/// 4) Model instance is placed in the same anchored world (ideally parented under CalibrationOrigin).
/// </summary>
public class SdfSystem : MonoBehaviour
{
    [Header("Visualization (optional)")]
    [SerializeField] private bool enableSliceDebug = false;
    [SerializeField] private bool enableOverlay = false;

    private readonly SdfWorkspaceState _workspace = new SdfWorkspaceState();
    private readonly SdfModelState _model = new SdfModelState();
    private readonly SdfOverlayController _overlay = new SdfOverlayController();

    private WorkspacePointConverter _pointConverter;
    private SdfGenerator _core;
    private SdfSliceDebugger _sliceDbg;
    private SdfShaderSet _shaders;

    private bool _initialized;
    private ComputeBuffer _lastFilteredPoints;
    private int _lastFilteredCount;

    public ComputeBuffer PointsWS => _pointConverter?.OutputBuffer;
    public SdfVolumeData Global => _core != null ? _core.Global : default;
    public SdfVolumeData Local => _core != null ? _core.Local : default;
    public RenderTexture OverlayRT => _overlay.OverlayRT;

    /// <summary>
    /// Call this when workspace is created or moved/changed.
    /// </summary>
    public void SetWorkspace(Transform newWorkspaceRoot, Vector3 newWorkspaceSizeWS)
    {
        bool changed = _workspace.Update(newWorkspaceRoot, newWorkspaceSizeWS);
        if (changed)
            _model.MarkWorkspaceChanged();
    }

    /// <summary>
    /// Call this after instantiating the target sculpture model.
    /// </summary>
    public void SetModelInstance(GameObject newModelInstance)
    {
        _model.SetModel(newModelInstance);
    }

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void OnDestroy()
    {
        DisposeAll();
    }

    private void InitializeIfNeeded()
    {
        if (_initialized) return;

        _shaders = SdfShaderLoader.LoadAll();

        if (_shaders.PointsToWorkspace == null || _shaders.PointAabb == null ||
            _shaders.SdfClear == null || _shaders.SdfVoxelizeSeeds == null ||
            _shaders.SdfJumpFlood == null || _shaders.SdfFinalize == null)
        {
            Debug.LogError("[SdfSystem] Missing one or more required compute shaders.");
            return;
        }

        _pointConverter = new WorkspacePointConverter(_shaders.PointsToWorkspace);
        _core = new SdfGenerator(
            _shaders.PointAabb,
            _shaders.SdfClear,
            _shaders.SdfVoxelizeSeeds,
            _shaders.SdfJumpFlood,
            _shaders.SdfFinalize
        );

        if (enableSliceDebug && _shaders.SdfSlice != null)
            _sliceDbg = new SdfSliceDebugger(_shaders.SdfSlice);

        if (enableOverlay && _shaders.SdfOverlay != null)
            _overlay.Ensure(_shaders.SdfOverlay, Camera.main, gameObject);

        _initialized = true;

        TryInitializeModel();
    }

    private void DisposeAll()
    {
        _pointConverter?.Dispose();
        _pointConverter = null;

        _core?.Dispose();
        _core = null;

        _sliceDbg?.Dispose();
        _sliceDbg = null;
    }

    private void TryInitializeModel()
    {
        if (!_initialized || _core == null) return;
        if (!_workspace.IsValid) return;
        if (_model.ModelInstance == null) return;

        if (!_model.IsDirty && _model.IsInitialized)
            return;

        var ws = new WorkspaceInfo(_workspace.Root, _workspace.Corner, _workspace.Size);
        _model.TryInitialize(_core, ws);
    }

    /// <summary>
    /// Main entry point each depth frame:
    /// Feed world-space point cloud buffer in, update SDF (local/global).
    /// </summary>
    public void UpdateWithWorldPoints(ComputeBuffer worldPointsFloat4, int pointCount)
    {
        InitializeIfNeeded();
        if (!_initialized) return;

        TryInitializeModel();
        if (!_model.IsInitialized)
            return;

        if (!_workspace.IsValid)
        {
            Debug.LogWarning("[SdfSystem] workspaceRoot is null. Call SetWorkspace(...) first.");
            return;
        }

        if (worldPointsFloat4 == null || pointCount <= 0)
            return;

        Matrix4x4 worldToWorkspace = _workspace.Root.worldToLocalMatrix;
        ComputeBuffer pointsWS = _pointConverter.ConvertAndFilter(
            worldPointsFloat4,
            pointCount,
            worldToWorkspace,
            _workspace.Corner,
            _workspace.Size,
            out int filteredCount);

        if (filteredCount <= 0)
        {
            _lastFilteredPoints = null;
            _lastFilteredCount = 0;
            _core.Update(null, 0, _workspace.Corner, _workspace.Size);
            return;
        }

        _lastFilteredPoints = pointsWS;
        _lastFilteredCount = filteredCount;

        _core.Update(pointsWS, filteredCount, _workspace.Corner, _workspace.Size);
    }

    /// <summary>
    /// Optional debug: build a slice from the GLOBAL TSDF.
    /// axis: 0=X, 1=Y, 2=Z. slice01: 0..1.
    /// </summary>
    public RenderTexture BuildGlobalSlice(int axis, float slice01)
    {
        if (_sliceDbg == null || _core == null) return null;

        var g = _core.Global;
        if (!g.IsValid) return null;

        return _sliceDbg.BuildSlice(g.Tsdf, g.Resolution, axis, slice01, g.Mu);
    }

    /// <summary>
    /// Optional: render fullscreen overlay from point cloud.
    /// Uses world-space points to colorize proximity to the SDF.
    /// </summary>
    public void RenderOverlay(ComputeBuffer worldPointsFloat4, int pointCount)
    {
        if (!enableOverlay) return;
        if (_core == null) return;

        var g = _core.Global;
        if (!g.IsValid) return;

        Matrix4x4 worldToWorkspace = _workspace.Root.worldToLocalMatrix;

        ComputeBuffer points = _lastFilteredPoints;
        int count = _lastFilteredCount;

        if (points == null || count <= 0)
        {
            if (worldPointsFloat4 == null || pointCount <= 0) return;

            points = _pointConverter.ConvertAndFilter(
                worldPointsFloat4,
                pointCount,
                worldToWorkspace,
                _workspace.Corner,
                _workspace.Size,
                out count);

            if (count <= 0) return;
        }

        _overlay.Render(points, count, worldToWorkspace, _core.Local, _core.Global);
    }
}
