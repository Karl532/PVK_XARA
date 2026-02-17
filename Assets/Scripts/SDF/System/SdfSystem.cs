using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Orchestrates:
/// - converting world-space points -> workspace space
/// - converting model mesh -> triangle space in workspace
/// - building local/global TSDF volumes via Core (black box)
/// - optional visualization (slice + overlay)
///
/// ASSUMPTIONS (current project):
/// 1) Point cloud buffer positions are in Unity WORLD space (OXDepth uses _TrackingToWorld).
/// 2) Workspace space is Workspace.localSpace (workspaceRoot).
/// 3) Workspace pivot is centered (Unity cube), so workspaceCornerWS = -size/2.
/// 4) Model instance is placed in the same anchored world (ideally parented under CalibrationOrigin).
/// </summary>
public class SdfSystem : MonoBehaviour
{
    [Header("Visualization (optional)")]
    [SerializeField] private bool enableSliceDebug = false;

    [Header("SDF Debug/Perf")]
    [SerializeField] private bool disableFilterReadback = false;
    [SerializeField] private bool disableLocalVolume = false;
    [SerializeField] private float localRebuildMinIntervalSeconds = 0f;
    [SerializeField] private float globalRebuildMinIntervalSeconds = 0f;
    [SerializeField] private int globalResolution = 128;
    [SerializeField] private int localResolution = 256;
    [SerializeField] private int maxStagesPerFrame = 1;

    [Header("Debug")]
    [SerializeField] private bool enableDebug = true;
    [SerializeField] private bool verboseDebug = true;
    [SerializeField] private bool timingDebug = false;
    [SerializeField] private float debugLogIntervalSeconds = 1f;

    private readonly SdfWorkspaceState _workspace = new SdfWorkspaceState();
    private readonly SdfModelState _model = new SdfModelState();

    private WorkspacePointConverter _pointConverter;
    private SdfGenerator _core;
    private SdfSliceDebugger _sliceDbg;
    private SdfShaderSet _shaders;

    private bool _initialized;
    private ComputeBuffer _lastFilteredPoints;
    private int _lastFilteredCount;
    private Matrix4x4 _lastInputToWorld = Matrix4x4.identity;
    private Matrix4x4 _lastInputToWorkspace = Matrix4x4.identity;
    private bool _suppressSculptGuide;
    private RenderTexture _cachedGlobalTsdf;
    private RenderTexture _cachedLocalTsdf;
    private SdfVolumeData _cachedGlobal;
    private SdfVolumeData _cachedLocal;

    public ComputeBuffer PointsWS => _pointConverter?.OutputBuffer;
    public ComputeBuffer LastFilteredPoints => _lastFilteredPoints;
    public int LastFilteredCount => _lastFilteredCount;
    public SdfVolumeData Global => _core != null ? _core.Global : default;
    public SdfVolumeData Local => _core != null ? _core.Local : default;
    public SdfVolumeData CachedGlobal => _cachedGlobal;
    public SdfVolumeData CachedLocal => _cachedLocal;
    public bool IsBuilding => _core != null && _core.IsBuilding;
    public Transform WorkspaceRoot => _workspace.Root;
    public Vector3 WorkspaceCorner => _workspace.Corner;
    public Vector3 WorkspaceSize => _workspace.Size;

    public void ConfigureDebug(bool enabled, bool verbose, bool timing, float logIntervalSeconds)
    {
        enableDebug = enabled;
        verboseDebug = verbose;
        timingDebug = timing;
        debugLogIntervalSeconds = Mathf.Max(0f, logIntervalSeconds);
        SdfDebug.Configure(enableDebug, verboseDebug, timingDebug, debugLogIntervalSeconds);
        SdfDebug.Log("[SdfSystem] ConfigureDebug applied.", this);
    }


    public void ConfigurePerf(bool disableFilterReadbackOption, bool disableLocalVolumeOption)
    {
        disableFilterReadback = disableFilterReadbackOption;
        disableLocalVolume = disableLocalVolumeOption;
        if (_core != null)
            _core.EnableLocalVolume = !disableLocalVolume;
        SdfDebug.Log($"[SdfSystem] ConfigurePerf applied. disableFilterReadback={disableFilterReadback} disableLocalVolume={disableLocalVolume}", this);
    }

    public void ConfigureLocalRebuildInterval(float seconds)
    {
        localRebuildMinIntervalSeconds = Mathf.Max(0f, seconds);
        if (_core != null)
            _core.LocalRebuildMinIntervalSeconds = localRebuildMinIntervalSeconds;
        SdfDebug.Log($"[SdfSystem] Local rebuild interval set to {localRebuildMinIntervalSeconds:0.###}s", this);
    }

    public void ConfigureGlobalRebuildInterval(float seconds)
    {
        globalRebuildMinIntervalSeconds = Mathf.Max(0f, seconds);
        if (_core != null)
            _core.GlobalRebuildMinIntervalSeconds = globalRebuildMinIntervalSeconds;
        SdfDebug.Log($"[SdfSystem] Global rebuild interval set to {globalRebuildMinIntervalSeconds:0.###}s", this);
    }

    public void ConfigureResolutions(int globalRes, int localRes)
    {
        globalResolution = globalRes;
        localResolution = localRes;
        if (_core != null)
            _core.SetResolutions(globalResolution, localResolution);
        SdfDebug.Log($"[SdfSystem] Resolutions set. global={globalResolution} local={localResolution}", this);
    }

    public void ConfigureBuildStages(int stagesPerFrame)
    {
        maxStagesPerFrame = Mathf.Max(1, stagesPerFrame);
        if (_core != null)
            _core.MaxStagesPerFrame = maxStagesPerFrame;
        SdfDebug.Log($"[SdfSystem] Build stages per frame set to {maxStagesPerFrame}", this);
    }

    /// <summary>
    /// Call this when workspace is created or moved/changed.
    /// </summary>
    public void SetWorkspace(Transform newWorkspaceRoot, Vector3 newWorkspaceSizeWS)
    {
        bool changed = _workspace.Update(newWorkspaceRoot, newWorkspaceSizeWS);
        if (changed)
        {
            _model.MarkWorkspaceChanged();
            SdfDebug.Log($"[SdfSystem] Workspace updated: root={(newWorkspaceRoot ? newWorkspaceRoot.name : "null")} size={newWorkspaceSizeWS}", this);
        }
    }

    /// <summary>
    /// Call this after instantiating the target sculpture model.
    /// </summary>
    public void SetModelInstance(GameObject newModelInstance)
    {
        _model.SetModel(newModelInstance);
        SdfDebug.Log($"[SdfSystem] Model instance set: {(newModelInstance ? newModelInstance.name : "null")}", this);
    }

    private void Awake()
    {
        SdfDebug.Configure(enableDebug, verboseDebug, timingDebug, debugLogIntervalSeconds);
        SdfDebug.Log("[SdfSystem] Awake: configured debug.", this);
        InitializeIfNeeded();
    }

    private void OnDestroy()
    {
        DisposeAll();
    }

    private void Update()
    {
        if (_core != null)
            _core.TickBuild();
    }

    private void InitializeIfNeeded()
    {
        if (_initialized) return;

        SdfDebug.LogVerbose("[SdfSystem] InitializeIfNeeded: loading shaders.", this);
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
        _core.BuildStarted += OnCoreBuildStarted;
        _core.BuildCompleted += OnCoreBuildCompleted;
        _core.EnableLocalVolume = !disableLocalVolume;
        _core.GlobalRebuildMinIntervalSeconds = Mathf.Max(0f, globalRebuildMinIntervalSeconds);
        _core.LocalRebuildMinIntervalSeconds = Mathf.Max(0f, localRebuildMinIntervalSeconds);
        _core.SetResolutions(globalResolution, localResolution);
        _core.MaxStagesPerFrame = maxStagesPerFrame;

        if (enableSliceDebug && _shaders.SdfSlice != null)
            _sliceDbg = new SdfSliceDebugger(_shaders.SdfSlice);

        _initialized = true;

        SdfDebug.Log("[SdfSystem] Initialized successfully.", this);
        TryInitializeModel();
    }

    private void DisposeAll()
    {
        SdfDebug.LogVerbose("[SdfSystem] Disposing resources.", this);
        _pointConverter?.Dispose();
        _pointConverter = null;

        if (_core != null)
        {
            _core.BuildStarted -= OnCoreBuildStarted;
            _core.BuildCompleted -= OnCoreBuildCompleted;
        }
        _core?.Dispose();
        _core = null;

        _sliceDbg?.Dispose();
        _sliceDbg = null;

        ReleaseCachedVolume(ref _cachedGlobalTsdf, ref _cachedGlobal);
        ReleaseCachedVolume(ref _cachedLocalTsdf, ref _cachedLocal);

    }

    private void TryInitializeModel()
    {
        if (!_initialized || _core == null) return;
        if (!_workspace.IsValid) return;
        if (_model.ModelInstance == null) return;

        if (!_model.IsDirty && _model.IsInitialized)
            return;

        using (SdfDebug.Sample("SdfSystem.TryInitializeModel", this))
        {
            var ws = new WorkspaceInfo(_workspace.Root, _workspace.Corner, _workspace.Size);
            _model.TryInitialize(_core, ws);
        }
    }

    private void OnCoreBuildStarted(bool isGlobal)
    {
        if (isGlobal)
            _suppressSculptGuide = true;
    }

    private void OnCoreBuildCompleted(bool isGlobal)
    {
        if (isGlobal)
        {
            _suppressSculptGuide = false;
            CacheVolume(Global, ref _cachedGlobalTsdf, ref _cachedGlobal);
        }
        else
        {
            CacheVolume(Local, ref _cachedLocalTsdf, ref _cachedLocal);
        }
    }

    public bool SuppressSculptGuide => _suppressSculptGuide;

    private static void CacheVolume(SdfVolumeData source, ref RenderTexture cacheTex, ref SdfVolumeData cacheData)
    {
        if (!source.IsValid)
            return;

        EnsureCacheTexture(source, ref cacheTex);
        Graphics.CopyTexture(source.Tsdf, cacheTex);
        cacheData = new SdfVolumeData(cacheTex, source.Corner, source.Size, source.Resolution, source.Mu);
    }

    private static void EnsureCacheTexture(SdfVolumeData source, ref RenderTexture cacheTex)
    {
        if (cacheTex != null &&
            cacheTex.IsCreated() &&
            cacheTex.width == source.Resolution &&
            cacheTex.height == source.Resolution &&
            cacheTex.volumeDepth == source.Resolution)
        {
            return;
        }

        if (cacheTex != null)
            cacheTex.Release();

        var desc = new RenderTextureDescriptor(source.Resolution, source.Resolution, RenderTextureFormat.RHalf, 0)
        {
            dimension = TextureDimension.Tex3D,
            volumeDepth = source.Resolution,
            enableRandomWrite = false
        };
        cacheTex = new RenderTexture(desc)
        {
            name = "SdfTsdfCache",
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        cacheTex.Create();
    }

    private static void ReleaseCachedVolume(ref RenderTexture cacheTex, ref SdfVolumeData cacheData)
    {
        if (cacheTex != null)
        {
            cacheTex.Release();
            cacheTex = null;
        }
        cacheData = default;
    }

    /// <summary>
    /// Main entry point each depth frame:
    /// Feed world-space point cloud buffer in, update SDF (local/global).
    /// </summary>
    public void UpdateWithWorldPoints(ComputeBuffer worldPointsFloat4, int pointCount)
    {
        Matrix4x4 worldToWorkspace = _workspace.Root != null ? _workspace.Root.worldToLocalMatrix : Matrix4x4.identity;
        UpdateWithPoints(worldPointsFloat4, pointCount, Matrix4x4.identity, worldToWorkspace);
    }

    public void UpdateWithPoints(ComputeBuffer pointsFloat4, int pointCount, Matrix4x4 inputToWorld, Matrix4x4 inputToWorkspace)
    {
        InitializeIfNeeded();
        if (!_initialized) return;

        SdfDebug.Log(
            $"[SdfSystem] UpdateWithPoints enter: pointsNull={pointsFloat4 == null} pointCount={pointCount} workspaceValid={_workspace.IsValid} modelInit={_model.IsInitialized}"
        );
        if (_workspace.Root != null)
        {
            var t = _workspace.Root;
            SdfDebug.Log(
                $"[SdfSystem] Workspace transform: pos={t.position} rot={t.rotation.eulerAngles} scale={t.localScale}"
            );
        }

        TryInitializeModel();
        if (!_model.IsInitialized)
        {
            SdfDebug.Log("[SdfSystem] Model not initialized yet.");
            return;
        }

        if (!_workspace.IsValid)
        {
            Debug.LogWarning("[SdfSystem] workspaceRoot is null. Call SetWorkspace(...) first.");
            return;
        }

        if (pointsFloat4 == null || pointCount <= 0)
        {
            SdfDebug.Log("[SdfSystem] No points received this frame.");
            return;
        }

        SdfDebug.Log(
            $"[SdfSystem] inputToWorkspace m00={inputToWorkspace.m00:F3} m01={inputToWorkspace.m01:F3} m02={inputToWorkspace.m02:F3} m03={inputToWorkspace.m03:F3} " +
            $"m10={inputToWorkspace.m10:F3} m11={inputToWorkspace.m11:F3} m12={inputToWorkspace.m12:F3} m13={inputToWorkspace.m13:F3} " +
            $"m20={inputToWorkspace.m20:F3} m21={inputToWorkspace.m21:F3} m22={inputToWorkspace.m22:F3} m23={inputToWorkspace.m23:F3}"
        );

        _lastInputToWorld = inputToWorld;
        _lastInputToWorkspace = inputToWorkspace;

        ComputeBuffer pointsWS;
        int filteredCount;
        if (disableFilterReadback)
        {
            pointsWS = _pointConverter.Convert(pointsFloat4, pointCount, inputToWorkspace);
            filteredCount = pointCount;
            SdfDebug.Log("[SdfSystem] Filter readback disabled; using unfiltered point count.");
        }
        else
        {
            pointsWS = _pointConverter.ConvertAndFilter(
                pointsFloat4,
                pointCount,
                inputToWorkspace,
                _workspace.Corner,
                _workspace.Size,
                out filteredCount);
        }

        SdfDebug.Log($"[SdfSystem] Filter result: pointsWSNull={pointsWS == null} filteredCount={filteredCount}");
        SdfDebug.Log($"[SdfSystem] Points in={pointCount} filtered={filteredCount} workspaceSize={_workspace.Size}");

        if (filteredCount <= 0)
        {
            _lastFilteredPoints = null;
            _lastFilteredCount = 0;
            _core.Update(null, 0, _workspace.Corner, _workspace.Size);
            return;
        }

        _lastFilteredPoints = pointsWS;
        _lastFilteredCount = filteredCount;

        SdfDebug.Log($"[SdfSystem] Cached filtered: lastPointsNull={_lastFilteredPoints == null} lastCount={_lastFilteredCount}");

        _core.Update(pointsWS, filteredCount, _workspace.Corner, _workspace.Size);

        var g = _core.Global;
        var l = _core.Local;
        SdfDebug.Log(
            $"[SdfSystem] Global TSDF valid={g.IsValid} res={g.Resolution} size={g.Size} mu={g.Mu} corner={g.Corner}"
        );
        SdfDebug.Log(
            $"[SdfSystem] Local TSDF valid={l.IsValid} res={l.Resolution} size={l.Size} mu={l.Mu} corner={l.Corner}"
        );
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

}
