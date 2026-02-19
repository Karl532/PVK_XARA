using Assets.Scripts.Depth.Quest3.OXDepth;
using Assets.Scripts.Debug;
using UnityEngine;

public class SdfDepthOrchestrator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DepthProviderFactory depthFactory;
    [SerializeField] private SdfGenerationSystem sdfSystem;
    [SerializeField] private Settings settings;
    [SerializeField] private SdfOrchestratorConfig config;
    [SerializeField] private SdfVisualizationConfig visualizationConfig;

    private Transform _workspaceRoot;
    private const string WorkspaceRootName = "Workspace";
    private const string RuntimeModelRootName = "RuntimeModel";
    private Transform _modelParent;

    private IDepthProvider _provider;
    private bool _subscribed;
    private Vector3 _lastWorkspaceSize;
    private GameObject _currentModel;
    private WorkspaceMovementState _workspaceMovement;
    private SdfVisualizationController _visuals;
    private bool _initialized;
    private Coroutine _initRoutine;

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
        if (_visuals != null)
            _visuals.TickVisuals();
    }

    private void LateUpdate()
    {
        if (!_initialized)
            return;

        if (_provider != null && _provider.IsReady && _visuals != null)
        {
            var depthFrame = new DepthFrameData(
                _provider.GetDepthTexture(),
                _provider.DepthResolution,
                _provider.GetInvDepthViewProj(),
                _provider.GetTrackingToWorld(),
                _provider.GetDepthEyeSlice(),
                _provider.GetFlipY());
            _visuals.SetDepthFrame(depthFrame);
        }
    }

    private void OnDestroy()
    {
        if (_subscribed && _provider != null)
        {
            _provider.OnPointCloudUpdated -= HandlePointCloudUpdated;
            _provider.OnDepthInvalid -= HandleDepthInvalid;
            DebugService.Log("[SdfDepthOrchestrator] Provider unsubscribed.", this);
        }
        if (sdfSystem != null && _visuals != null)
            sdfSystem.VisualizationDataUpdated -= _visuals.OnVisualizationData;
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

        sdfSystem = sdfSystem != null ? sdfSystem : ComponentUtility.GetOrAddComponent<SdfGenerationSystem>(gameObject, this);
        depthFactory = depthFactory != null ? depthFactory : ComponentUtility.GetOrAddComponent<DepthProviderFactory>(gameObject, this);

        if (!settings)
            settings = Settings.FindAnySettingsAsset();
        if (config == null)
            DebugService.Warn("[SdfDepthOrchestrator] Missing SdfOrchestratorConfig reference.", this);
        if (visualizationConfig == null)
            DebugService.Warn("[SdfDepthOrchestrator] Missing SdfVisualizationConfig reference.", this);

        if (config != null)
        {
            DebugService.Configure(config.enableDebug, config.verboseDebug, config.timingDebug, config.debugLogIntervalSeconds);
            sdfSystem.ConfigureDebug(config.enableDebug, config.verboseDebug, config.timingDebug, config.debugLogIntervalSeconds);
            sdfSystem.ConfigurePerf(config.sdfDisableLocalVolume);
            sdfSystem.ConfigureGlobalRebuildInterval(config.sdfGlobalRebuildMinIntervalSeconds);
            sdfSystem.ConfigureLocalRebuildInterval(config.sdfLocalRebuildMinIntervalSeconds);
            sdfSystem.ConfigureResolutions(config.sdfGlobalResolution, config.sdfLocalResolution);
            sdfSystem.ConfigureBuildStages(config.sdfStagesPerFrame);
        }

        DebugService.LogVerbose($"[SdfDepthOrchestrator] Start: settings={(settings ? settings.name : "null")}", this);

        SyncWorkspace();
        SyncModel();

        _visuals = _visuals != null ? _visuals : ComponentUtility.GetOrAddComponent<SdfVisualizationController>(gameObject, this);
        if (_visuals != null)
            _visuals.Initialize();
        if (sdfSystem != null && _visuals != null)
            sdfSystem.VisualizationDataUpdated += _visuals.OnVisualizationData;

        _provider = depthFactory.GetProvider();
        _provider.OnPointCloudUpdated += HandlePointCloudUpdated;
        _provider.OnDepthInvalid += HandleDepthInvalid;
        _subscribed = true;

        _initialized = true;
        DebugService.Log($"[SdfDepthOrchestrator] Provider subscribed: {_provider.GetType().Name}", this);
    }

    private void HandlePointCloudUpdated(PointCloudData data)
    {
        if (_workspaceMovement != null && _workspaceMovement.IsMoving) return;

        if (data.pointBuffer == null || data.pointCount <= 0)
        {
            var stats = _provider != null ? _provider.GetStatistics() : default;
            DebugService.WarnEvery(
                "SdfDepthOrchestrator.EmptyPointCloud",
                $"[SdfDepthOrchestrator] Empty point cloud. count={data.pointCount} bufferNull={(data.pointBuffer == null)} " +
                $"providerReady={(_provider != null && _provider.IsReady)} depthTexNull={(_provider == null || _provider.GetDepthTexture() == null)} " +
                $"depthRes={(_provider != null ? _provider.DepthResolution.ToString() : "n/a")} stats={stats}",
                config != null ? config.debugLogIntervalSeconds : 1f,
                this);
            return;
        }

        DebugService.LogEvery(
            "SdfDepthOrchestrator.PointCloud",
            $"[SdfDepthOrchestrator] PointCloud: count={data.pointCount} bufferCount={(data.pointBuffer != null ? data.pointBuffer.count : 0)} overlay=true",
            config != null ? config.debugLogIntervalSeconds : 1f,
            this);

        Matrix4x4 inputToWorld;
        Matrix4x4 inputToWorkspace;
        SdfWorkspaceUtility.ResolveInputMatrices(_workspaceRoot, out inputToWorld, out inputToWorkspace);
        sdfSystem.UpdateWithPoints(data.pointBuffer, data.pointCount, inputToWorld, inputToWorkspace);
        // Visualization reacts to SdfGenerationSystem events.
        if (_visuals != null)
        {
            _visuals.SetPointCloud(data);
            var depthFrame = new DepthFrameData(
                _provider.GetDepthTexture(),
                _provider.DepthResolution,
                _provider.GetInvDepthViewProj(),
                _provider.GetTrackingToWorld(),
                _provider.GetDepthEyeSlice(),
                _provider.GetFlipY());
            _visuals.SetDepthFrame(depthFrame);
        }

    }

    private void HandleDepthInvalid()
    {
        DebugService.WarnEvery("SdfDepthOrchestrator.DepthInvalid", "[SdfDepthOrchestrator] Depth provider reported invalid depth.", 1.5f, this);
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
            DebugService.Log($"[SdfDepthOrchestrator] Model synced: {_currentModel.name}", this);
        }
    }

    

}






