using UnityEngine;
using Assets.Scripts.Debug;

public partial class SdfGenerationSystem
{
    /// <summary>
    /// Call this when workspace is created or moved/changed.
    /// </summary>
    public void SetWorkspace(Transform newWorkspaceRoot, Vector3 newWorkspaceSizeWS)
    {
        bool changed = _workspace.Update(newWorkspaceRoot, newWorkspaceSizeWS);
        if (changed)
        {
            _model.MarkWorkspaceChanged();
            DebugService.Log($"[SdfGenerationSystem] Workspace updated: root={(newWorkspaceRoot ? newWorkspaceRoot.name : "null")} size={newWorkspaceSizeWS}", this);
        }
    }

    /// <summary>
    /// Call this after instantiating the target sculpture model.
    /// </summary>
    public void SetModelInstance(GameObject newModelInstance)
    {
        _model.SetModel(newModelInstance);
        DebugService.Log($"[SdfGenerationSystem] Model instance set: {(newModelInstance ? newModelInstance.name : "null")}", this);
    }

    private void Awake()
    {
        DebugService.Configure(enableDebug, verboseDebug, timingDebug, debugLogIntervalSeconds);
        DebugService.Log("[SdfGenerationSystem] Awake: configured debug.", this);
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

        DebugService.LogVerbose("[SdfGenerationSystem] InitializeIfNeeded: loading shaders.", this);
        if (!SdfShaderLoader.TryLoadAll(out _shaders))
        {
            DebugService.Error("[SdfGenerationSystem] Missing one or more required compute shaders.");
            return;
        }

        _pointConverter = new WorkspacePointConverter(_shaders.PointsToWorkspace);
        _core = new SdfGenerator(_shaders);
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

        DebugService.Log("[SdfGenerationSystem] Initialized successfully.", this);
        TryInitializeModel();
    }

    private void DisposeAll()
    {
        DebugService.LogVerbose("[SdfGenerationSystem] Disposing resources.", this);
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

        using (DebugService.Sample("SdfGenerationSystem.TryInitializeModel", this))
        {
            var ws = new WorkspaceInfo(_workspace.Root, _workspace.Corner, _workspace.Size);
            _model.TryInitialize(_core, ws);
        }
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






