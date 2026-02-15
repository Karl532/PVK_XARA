using Assets.Scripts.Depth.Quest3.OXDepth;
using UnityEngine;

public class SdfDepthOrchestrator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DepthProviderFactory depthFactory;
    [SerializeField] private SdfSystem sdfSystem;
    [SerializeField] private Settings settings;

    private Transform _workspaceRoot;
    private const string WorkspaceRootName = "PlacementBlock";
    private const string RuntimeModelRootName = "RuntimeModel";
    private Vector3 _workspaceSizeWS = Vector3.one;
    private Transform _modelParent;

    [Header("Overlay")]
    [SerializeField] private bool renderOverlay = false;

    private IDepthProvider _provider;
    private bool _subscribed;
    private Vector3 _lastWorkspaceSize;
    private Transform _lastWorkspaceRoot;
    private GameObject _currentModel;
    private GameObject _instantiatedFromSettings;
    private GameObject _lastSettingsPrefab;

    private void Awake()
    {
        if (!sdfSystem)
            sdfSystem = GetComponent<SdfSystem>();

        if (!sdfSystem)
            sdfSystem = gameObject.AddComponent<SdfSystem>();

        if (!depthFactory)
            depthFactory = GetComponent<DepthProviderFactory>();

        if (!depthFactory)
            depthFactory = gameObject.AddComponent<DepthProviderFactory>();

        if (!settings)
            settings = FindAnySettingsAsset();
    }

    private void Start()
    {
        if (!sdfSystem)
        {
            Debug.LogError("[SdfDepthOrchestrator] SdfSystem component could not be created.");
            return;
        }

        if (!depthFactory)
        {
            Debug.LogError("[SdfDepthOrchestrator] DepthProviderFactory component could not be created.");
            return;
        }

        // Workspace
        SyncWorkspace(force: true);
        SyncModel(force: true);

        _provider = depthFactory.GetProvider();
        if (_provider == null)
        {
            Debug.LogError("[SdfDepthOrchestrator] Failed to create depth provider.");
            return;
        }

        _provider.OnPointCloudUpdated += HandlePointCloudUpdated;
        _provider.OnDepthInvalid += HandleDepthInvalid;
        _subscribed = true;
    }

    private void Update()
    {
        SyncWorkspace(force: false);
        SyncModel(force: false);
    }

    private void OnDestroy()
    {
        if (_subscribed && _provider != null)
        {
            _provider.OnPointCloudUpdated -= HandlePointCloudUpdated;
            _provider.OnDepthInvalid -= HandleDepthInvalid;
        }
    }

    private void HandlePointCloudUpdated(PointCloudData data)
    {
        if (sdfSystem == null)
            return;

        sdfSystem.UpdateWithWorldPoints(data.pointBuffer, data.pointCount);

        if (!renderOverlay)
            return;

        sdfSystem.RenderOverlay(data.pointBuffer, data.pointCount);
    }

    private void HandleDepthInvalid()
    {
        // Optional: add UI or logging here
    }

    private void SyncWorkspace(bool force)
    {
        if (_workspaceRoot == null && !string.IsNullOrEmpty(WorkspaceRootName))
        {
            var go = GameObject.Find(WorkspaceRootName);
            if (go != null)
                _workspaceRoot = go.transform;
        }

        if (_workspaceRoot == null)
        {
            if (force)
                Debug.LogWarning("[SdfDepthOrchestrator] workspaceRoot is not assigned.");
            return;
        }

        Vector3 size = _workspaceSizeWS;
        if (settings)
            size = settings.stoneBlockDimensions;

        // If workspace root has a scale, prefer it as live source of truth.
        if (_workspaceRoot.localScale != Vector3.zero)
            size = _workspaceRoot.localScale;

        bool rootChanged = _lastWorkspaceRoot != _workspaceRoot;
        bool sizeChanged = _lastWorkspaceSize != size;

        if (force || rootChanged || sizeChanged || _workspaceRoot.hasChanged)
        {
            sdfSystem.SetWorkspace(_workspaceRoot, size);
            _lastWorkspaceRoot = _workspaceRoot;
            _lastWorkspaceSize = size;
            _workspaceRoot.hasChanged = false;
        }
    }

    private void SyncModel(bool force)
    {
        GameObject model = null;

        if (!string.IsNullOrEmpty(RuntimeModelRootName))
        {
            var go = GameObject.Find(RuntimeModelRootName);
            if (go != null)
                model = go;
        }

        if (model == null && settings && settings.targetSculpturePrefab)
        {
            if (_instantiatedFromSettings == null || _lastSettingsPrefab != settings.targetSculpturePrefab)
            {
                if (_instantiatedFromSettings != null)
                    Destroy(_instantiatedFromSettings);

                _instantiatedFromSettings = Instantiate(settings.targetSculpturePrefab, _modelParent);
                _lastSettingsPrefab = settings.targetSculpturePrefab;
            }

            model = _instantiatedFromSettings;
        }

        if (model == null)
        {
            if (force)
                Debug.LogWarning("[SdfDepthOrchestrator] No model instance assigned or created.");
            return;
        }

        if (force || _currentModel != model)
        {
            sdfSystem.SetModelInstance(model);
            _currentModel = model;
        }
    }

    private static Settings FindAnySettingsAsset()
    {
        var assets = Resources.FindObjectsOfTypeAll<Settings>();
        if (assets != null && assets.Length > 0)
            return assets[0];
        return null;
    }
}
