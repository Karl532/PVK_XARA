using UnityEngine;
using Assets.Scripts.Depth.Quest3.OXDepth;

public class SdfVisualizationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SdfVisualizationConfig visualizationConfig;
    [SerializeField] private Settings settings;

    private SdfBoundsRenderer _boundsRenderer;
    private SdfFullSdfGridRenderer _gridRenderer;
    private SdfSculptGuideRenderer _sculptGuideRenderer;
    private SdfDepthErrorRenderer _depthErrorRenderer;
    private SdfMatchOverlayRenderer _matchOverlayRenderer;
    private bool _ready;
    private SdfVisualizationData _lastData;
    private bool _hasData;
    private PointCloudData _pointCloud;
    private bool _hasPointCloud;
    private DepthFrameData _depthFrame;
    private bool _hasDepthFrame;

    private void Awake()
    {
        CreateRenderers();
    }

    public void Initialize(SdfVisualizationConfig config, Settings settingsAsset)
    {
        if (visualizationConfig == null)
            visualizationConfig = config;
        if (settings == null)
            settings = settingsAsset;
        if (settings == null)
            settings = Settings.FindAnySettingsAsset();

        if (visualizationConfig == null)
        {
            Debug.LogError("[SdfVisualizationController] Missing SdfVisualizationConfig reference.", this);
            enabled = false;
            return;
        }

        if (settings == null)
        {
            Debug.LogError("[SdfVisualizationController] Missing Settings reference.", this);
            enabled = false;
            return;
        }

        _gridRenderer.enabled = settings.sdfRenderFullSdfGrid;
        _sculptGuideRenderer.enabled = settings.sdfRenderSculptGuide;
        if (_depthErrorRenderer != null)
            _depthErrorRenderer.enabled = visualizationConfig.depthErrorEnabled;
        if (_matchOverlayRenderer != null)
            _matchOverlayRenderer.enabled = visualizationConfig.sdfMatchOverlayEnabled;
        _ready = true;
    }

    public void TickVisuals()
    {
        if (!_ready)
            return;

        if (_hasData)
            _boundsRenderer.UpdateBounds(visualizationConfig, settings, _lastData);
    }

    public void SetPointCloud(PointCloudData data)
    {
        _pointCloud = data;
        _hasPointCloud = data.pointBuffer != null && data.pointCount > 0;
    }

    public void SetDepthFrame(DepthFrameData data)
    {
        _depthFrame = data;
        _hasDepthFrame = data.DepthTexture != null && data.DepthResolution.x > 0 && data.DepthResolution.y > 0;
    }

    public void OnVisualizationData(SdfVisualizationData data)
    {
        if (!_ready)
            return;

        SdfDebug.LogEvery(
            "SdfVisualizationController.OnVisualizationData",
            $"[SG_DEBUG] [SdfVisualizationController] OnVisualizationData: globalValid={data.Global.IsValid} workspaceRoot={(data.WorkspaceRoot != null)} size={data.WorkspaceSize}",
            1f,
            this);

        _gridRenderer.enabled = settings.sdfRenderFullSdfGrid;
        _sculptGuideRenderer.enabled = settings.sdfRenderSculptGuide;
        if (_depthErrorRenderer != null)
            _depthErrorRenderer.enabled = visualizationConfig.depthErrorEnabled;
        if (_matchOverlayRenderer != null)
            _matchOverlayRenderer.enabled = visualizationConfig.sdfMatchOverlayEnabled;

        _lastData = data;
        _hasData = true;

        if (settings.sdfRenderFullSdfGrid)
        {
            float distanceScale = Mathf.Max(0.1f, data.WorkspaceSize.magnitude);
            var volume = data.Global;
            if (!volume.IsValid || data.WorkspaceRoot == null)
                return;
            _gridRenderer.Configure(
                visualizationConfig.overlayFullSdfGridResolution,
                visualizationConfig.overlayFullSdfAlpha,
                visualizationConfig.overlayGridPointSizePx,
                distanceScale);
            _gridRenderer.UpdateGrid(volume, data.WorkspaceRoot);
        }

        if (settings.sdfRenderSculptGuide)
        {
            var volume = data.Global;
            if (!volume.IsValid || data.WorkspaceRoot == null)
            {
                _sculptGuideRenderer.enabled = false;
                SdfDebug.LogEvery(
                    "SdfVisualizationController.SculptGuideMissing",
                    "[SdfVisualizationController] Sculpt guide skipped: missing volume or workspace.",
                    1f,
                    this);
            }
            else
            {
                _sculptGuideRenderer.enabled = true;
                var sculptSettings = SculptGuideSettings.FromConfig(visualizationConfig, 4);
                SdfDebug.LogEvery(
                    "SdfVisualizationController.SculptGuideSettings",
                    $"[SG_DEBUG] [SdfVisualizationController] SculptGuide settings: mesh={sculptSettings.MeshEnabled} points={sculptSettings.RenderPoints} cache={sculptSettings.EnableCache} between={sculptSettings.BetweenEnabled}",
                    1f,
                    this);
                _sculptGuideRenderer.UpdateVisualizationData(data, sculptSettings);
                if (_hasPointCloud)
                    _sculptGuideRenderer.UpdatePointCloud(_pointCloud.pointBuffer, _pointCloud.pointCount);
                if (_hasDepthFrame)
                    _sculptGuideRenderer.UpdateDepthFrame(_depthFrame);

                if (!_hasPointCloud)
                {
                    SdfDebug.LogEvery(
                        "SdfVisualizationController.SculptGuideNoPoints",
                        "[SdfVisualizationController] Sculpt guide waiting for point cloud.",
                        1f,
                        this);
                }
            }
        }
        else
        {
            _sculptGuideRenderer.enabled = false;
        }

        if (_depthErrorRenderer != null)
        {
            var depthSettings = SdfDepthErrorSettings.FromConfig(visualizationConfig);
            _depthErrorRenderer.enabled = depthSettings.Enabled;
            if (depthSettings.Enabled && data.Global.IsValid && data.WorkspaceRoot != null && _hasDepthFrame)
                _depthErrorRenderer.UpdateData(data, _depthFrame, depthSettings);
        }

        if (_matchOverlayRenderer != null)
        {
            var matchSettings = SdfMatchOverlaySettings.FromConfig(visualizationConfig);
            _matchOverlayRenderer.enabled = matchSettings.Enabled;
            if (matchSettings.Enabled && data.Global.IsValid && data.WorkspaceRoot != null && _hasPointCloud)
            {
                _matchOverlayRenderer.UpdateData(data, matchSettings);
                _matchOverlayRenderer.UpdatePointCloud(_pointCloud.pointBuffer, _pointCloud.pointCount);
            }
        }
    }

    private void CreateRenderers()
    {
        if (_sculptGuideRenderer == null)
        {
            var go = new GameObject("SdfSculptGuideRenderer");
            CalibrationOriginUtility.AttachToOrigin(go.transform, worldPositionStays: true);
            _sculptGuideRenderer = ComponentUtility.GetOrAddComponent<SdfSculptGuideRenderer>(go, this);
            SdfDebug.Log("[SdfVisualizationController] Created SdfSculptGuideRenderer.", this);
        }

        if (_gridRenderer == null)
        {
            var go = new GameObject("SdfFullSdfGridRenderer");
            CalibrationOriginUtility.AttachToOrigin(go.transform, worldPositionStays: true);
            _gridRenderer = ComponentUtility.GetOrAddComponent<SdfFullSdfGridRenderer>(go, this);
            SdfDebug.Log("[SdfVisualizationController] Created SdfFullSdfGridRenderer.", this);
        }

        if (_depthErrorRenderer == null)
        {
            var go = new GameObject("SdfDepthErrorRenderer");
            CalibrationOriginUtility.AttachToOrigin(go.transform, worldPositionStays: true);
            _depthErrorRenderer = ComponentUtility.GetOrAddComponent<SdfDepthErrorRenderer>(go, this);
            SdfDebug.Log("[SdfVisualizationController] Created SdfDepthErrorRenderer.", this);
        }

        if (_matchOverlayRenderer == null)
        {
            var go = new GameObject("SdfMatchOverlayRenderer");
            CalibrationOriginUtility.AttachToOrigin(go.transform, worldPositionStays: true);
            _matchOverlayRenderer = ComponentUtility.GetOrAddComponent<SdfMatchOverlayRenderer>(go, this);
            SdfDebug.Log("[SdfVisualizationController] Created SdfMatchOverlayRenderer.", this);
        }

        _boundsRenderer = ComponentUtility.GetOrAddComponent<SdfBoundsRenderer>(gameObject, this);
    }
}
