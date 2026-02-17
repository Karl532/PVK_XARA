using Assets.Scripts.Depth.Quest3.OXDepth;
using UnityEngine;

public class SdfDepthOrchestrator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DepthProviderFactory depthFactory;
    [SerializeField] private SdfSystem sdfSystem;
    [SerializeField] private Settings settings;
    [SerializeField] private SdfOrchestratorConfig config;

    private Transform _workspaceRoot;
    private const string WorkspaceRootName = "Workspace";
    private const string RuntimeModelRootName = "RuntimeModel";
    private Transform _modelParent;

    private bool _overlayFullSdfDebug;


    private IDepthProvider _provider;
    private bool _subscribed;
    private Vector3 _lastWorkspaceSize;
    private GameObject _currentModel;
    private WorkspaceMovementState _workspaceMovement;
    private SdfBoundsRenderer _boundsRenderer;
    private Material _boundsMaterial;
    private SdfFullSdfGridRenderer _gridRenderer;
    private SdfSculptGuideRenderer _sculptGuideRenderer;
    private bool _initialized;
    private Coroutine _initRoutine;

    public bool OverlayFullSdfDebugEnabled => _overlayFullSdfDebug;
    public void SetOverlayFullSdfDebug(bool enabled)
    {
        _overlayFullSdfDebug = enabled;
        if (_gridRenderer != null)
            _gridRenderer.enabled = _overlayFullSdfDebug;
    }

    public bool SculptGuideEnabled => config != null && config.sculptGuideEnabled;

    public void SetSculptGuideEnabled(bool enabled)
    {
        if (config != null)
            config.sculptGuideEnabled = enabled;
        if (_sculptGuideRenderer != null)
            _sculptGuideRenderer.enabled = enabled;
    }

    private void Start()
    {
        if (_initRoutine == null)
            _initRoutine = StartCoroutine(InitializeWhenReady());
    }

    private void Update()
    {
        if (!_initialized)
            return;

        SyncWorkspace();
        SyncModel();
        UpdateBoundsRenderer();
    }

    private void OnDestroy()
    {
        if (_subscribed && _provider != null)
        {
            _provider.OnPointCloudUpdated -= HandlePointCloudUpdated;
            _provider.OnDepthInvalid -= HandleDepthInvalid;
            SdfDebug.Log("[SdfDepthOrchestrator] Provider unsubscribed.", this);
        }
    }

    private System.Collections.IEnumerator InitializeWhenReady()
    {
        while (_workspaceRoot == null)
        {
            var go = WorkspaceBoundsUtility.FindWorkspace(WorkspaceRootName);
            if (go != null)
                _workspaceRoot = go.transform;
            yield return null;
        }

        if (_workspaceRoot != null)
            _workspaceMovement = _workspaceRoot.GetComponent<WorkspaceMovementState>()
                ?? _workspaceRoot.gameObject.AddComponent<WorkspaceMovementState>();

        while (!SdfModelUtility.IsModelReady(RuntimeModelRootName, settings))
            yield return null;

        InitializeOnce();
    }

    private void InitializeOnce()
    {
        if (_initialized)
            return;

        sdfSystem = sdfSystem != null ? sdfSystem : ComponentUtility.GetOrAddComponent<SdfSystem>(gameObject, this);
        depthFactory = depthFactory != null ? depthFactory : ComponentUtility.GetOrAddComponent<DepthProviderFactory>(gameObject, this);

        if (!settings)
            settings = Settings.FindAnySettingsAsset();
        if (config == null)
            Debug.LogWarning("[SdfDepthOrchestrator] Missing SdfOrchestratorConfig reference.", this);

        if (config != null)
        {
            SdfDebug.Configure(config.enableDebug, config.verboseDebug, config.timingDebug, config.debugLogIntervalSeconds);
            sdfSystem.ConfigureDebug(config.enableDebug, config.verboseDebug, config.timingDebug, config.debugLogIntervalSeconds);
            sdfSystem.ConfigurePerf(config.sdfDisableFilterReadback, config.sdfDisableLocalVolume);
            sdfSystem.ConfigureGlobalRebuildInterval(config.sdfGlobalRebuildMinIntervalSeconds);
            sdfSystem.ConfigureLocalRebuildInterval(config.sdfLocalRebuildMinIntervalSeconds);
            sdfSystem.ConfigureResolutions(config.sdfGlobalResolution, config.sdfLocalResolution);
            sdfSystem.ConfigureBuildStages(config.sdfStagesPerFrame);
        }

        SdfDebug.LogVerbose($"[SdfDepthOrchestrator] Start: settings={(settings ? settings.name : "null")}", this);

        SyncWorkspace();
        SyncModel();

        CreateRenderers();

        _provider = depthFactory.GetProvider();
        _provider.OnPointCloudUpdated += HandlePointCloudUpdated;
        _provider.OnDepthInvalid += HandleDepthInvalid;
        _subscribed = true;

        _initialized = true;
        SdfDebug.Log($"[SdfDepthOrchestrator] Provider subscribed: {_provider.GetType().Name}", this);
    }

    private void UpdateBoundsRenderer()
    {
        if (config == null || !config.showWorkspaceBounds)
            return;

        if (sdfSystem == null || sdfSystem.WorkspaceRoot == null)
            return;

        _boundsRenderer = _boundsRenderer != null ? _boundsRenderer : ComponentUtility.GetOrAddComponent<SdfBoundsRenderer>(gameObject, this);

        if (_boundsMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                _boundsMaterial = new Material(shader) { name = "SDF_BoundsLineMat" };
                _boundsMaterial.color = config.boundsColor;
                _boundsMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                if (_boundsMaterial.HasProperty("_Surface"))
                    _boundsMaterial.SetFloat("_Surface", 0f); // Opaque
                if (_boundsMaterial.HasProperty("_ZWrite"))
                    _boundsMaterial.SetInt("_ZWrite", 1);
            }
        }

        if (_boundsMaterial != null)
        {
            _boundsMaterial.color = config.boundsColor;
            // Force only the outer workspace bounds (no inner global/local boxes).
            _boundsRenderer.Configure(_boundsMaterial, config.boundsLineWidth, config.showWorkspaceBounds, false, false);
        }

        _boundsRenderer.Render(
            sdfSystem.WorkspaceRoot,
            sdfSystem.WorkspaceCorner, sdfSystem.WorkspaceSize,
            sdfSystem.Global.Corner, sdfSystem.Global.Size,
            sdfSystem.Local.Corner, sdfSystem.Local.Size);
    }


    private void CreateRenderers()
    {
        if (_sculptGuideRenderer == null)
        {
            var go = new GameObject("SdfSculptGuideRenderer");
            CalibrationOriginUtility.AttachToOrigin(go.transform, worldPositionStays: true);
            _sculptGuideRenderer = _sculptGuideRenderer != null ? _sculptGuideRenderer : ComponentUtility.GetOrAddComponent<SdfSculptGuideRenderer>(go, this);
            _sculptGuideRenderer.enabled = config != null && config.sculptGuideEnabled;
            SdfDebug.Log("[SdfDepthOrchestrator] Created SdfSculptGuideRenderer.", this);
        }

        if (_gridRenderer == null)
        {
            var go = new GameObject("SdfFullSdfGridRenderer");
            CalibrationOriginUtility.AttachToOrigin(go.transform, worldPositionStays: true);
            _gridRenderer = _gridRenderer != null ? _gridRenderer : ComponentUtility.GetOrAddComponent<SdfFullSdfGridRenderer>(go, this);
            SdfDebug.Log("[SdfDepthOrchestrator] Created SdfFullSdfGridRenderer.", this);
        }
    }


    private void HandlePointCloudUpdated(PointCloudData data)
    {
        if (_workspaceMovement != null && _workspaceMovement.IsMoving) return;

        SdfDebug.LogEvery(
            "SdfDepthOrchestrator.PointCloud",
            $"[SdfDepthOrchestrator] PointCloud: count={data.pointCount} bufferCount={(data.pointBuffer != null ? data.pointBuffer.count : 0)} overlay=true",
            config != null ? config.debugLogIntervalSeconds : 1f,
            this);

        Matrix4x4 inputToWorld;
        Matrix4x4 inputToWorkspace;
        WorkspaceUtility.ResolveInputMatrices(_workspaceRoot, out inputToWorld, out inputToWorkspace);
        sdfSystem.UpdateWithPoints(data.pointBuffer, data.pointCount, inputToWorld, inputToWorkspace);

        if (_overlayFullSdfDebug)
        {
            if (_gridRenderer != null)
            {
                float distanceScale = Mathf.Max(0.1f, sdfSystem.WorkspaceSize.magnitude);
                var volume = sdfSystem.CachedGlobal.IsValid ? sdfSystem.CachedGlobal : sdfSystem.Global;
                if (!volume.IsValid)
                    return;
                if (config != null)
                {
                    _gridRenderer.Configure(
                        config.overlayFullSdfGridResolution,
                        config.overlayFullSdfAlpha,
                        config.overlayGridPointSizePx,
                        distanceScale);
                }
                if (sdfSystem.WorkspaceRoot != null)
                    _gridRenderer.UpdateGrid(volume, sdfSystem.WorkspaceRoot);
            }
        }

        if (config != null && config.sculptGuideEnabled && _sculptGuideRenderer != null)
        {
            var volume = sdfSystem.CachedGlobal.IsValid ? sdfSystem.CachedGlobal : sdfSystem.Global;
            if (!volume.IsValid)
            {
                _sculptGuideRenderer.enabled = false;
            }
            else
            {
                _sculptGuideRenderer.enabled = true;
                _sculptGuideRenderer.Configure(
                    config.sculptGuidePointSizePx,
                    config.sculptGuideAlpha,
                    config.sculptGuideSurfaceDistanceMeters);
                _sculptGuideRenderer.UpdatePoints(
                    sdfSystem.LastFilteredPoints,
                    sdfSystem.LastFilteredCount,
                    volume,
                    sdfSystem.WorkspaceRoot);
            }
        }
        else if (_sculptGuideRenderer != null)
        {
            _sculptGuideRenderer.enabled = false;
        }
    }

    private void HandleDepthInvalid()
    {
        SdfDebug.WarnEvery("SdfDepthOrchestrator.DepthInvalid", "[SdfDepthOrchestrator] Depth provider reported invalid depth.", 1.5f, this);
    }

    private void SyncWorkspace()
    {
        if (_workspaceRoot == null)
            return;

        Vector3 size = settings != null ? settings.stoneBlockDimensions : Vector3.one;
        bool sizeChanged = _lastWorkspaceSize != size;
        if (sizeChanged || _workspaceRoot.hasChanged || (_workspaceMovement != null && _workspaceMovement.IsMoving))
        {
            sdfSystem.SetWorkspace(_workspaceRoot, size);
            _lastWorkspaceSize = size;
            _workspaceRoot.hasChanged = false;
        }
    }

    private void SyncModel()
    {
        var model = SdfModelUtility.FindRuntimeModel(RuntimeModelRootName);

        if (model == null)
        {
            return;
        }

        if (_currentModel != model)
        {
            sdfSystem.SetModelInstance(model);
            _currentModel = model;
            SdfDebug.Log($"[SdfDepthOrchestrator] Model synced: {_currentModel.name}", this);
        }
    }

    

}
