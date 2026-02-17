using UnityEngine;

public class SdfVisualizationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SdfVisualizationConfig visualizationConfig;
    [SerializeField] private Settings settings;

    private SdfBoundsRenderer _boundsRenderer;
    private SdfFullSdfGridRenderer _gridRenderer;
    private SdfSculptGuideRenderer _sculptGuideRenderer;
    private bool _ready;
    private SdfVisualizationData _lastData;
    private bool _hasData;

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
        _ready = true;
    }

    public void TickVisuals()
    {
        if (!_ready)
            return;

        if (_hasData)
            _boundsRenderer.UpdateBounds(visualizationConfig, settings, _lastData);
    }

    public void OnVisualizationData(SdfVisualizationData data)
    {
        if (!_ready)
            return;

        _gridRenderer.enabled = settings.sdfRenderFullSdfGrid;
        _sculptGuideRenderer.enabled = settings.sdfRenderSculptGuide;

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
            }
            else
            {
                _sculptGuideRenderer.enabled = true;
                _sculptGuideRenderer.Configure(
                    visualizationConfig.sculptGuidePointSizePx,
                    visualizationConfig.sculptGuideAlpha,
                    visualizationConfig.sculptGuideSurfaceDistanceMeters);
                _sculptGuideRenderer.UpdatePoints(
                    data.PointsWS,
                    data.PointCount,
                    volume,
                    data.WorkspaceRoot);
            }
        }
        else
        {
            _sculptGuideRenderer.enabled = false;
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

        _boundsRenderer = ComponentUtility.GetOrAddComponent<SdfBoundsRenderer>(gameObject, this);
    }
}
