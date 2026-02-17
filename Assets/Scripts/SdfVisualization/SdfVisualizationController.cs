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
        _ready = true;
    }

    public void TickVisuals()
    {
        if (!_ready)
            return;

        if (_hasData)
            _boundsRenderer.UpdateBounds(visualizationConfig, settings, _lastData);
    }

    public void SetDepthFrame(DepthFrameData depthFrame)
    {
        _depthFrame = depthFrame;
        _hasDepthFrame = depthFrame.DepthTexture != null;
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
                SdfDebug.LogEvery(
                    "SdfVisualizationController.SculptGuideMissing",
                    "[SdfVisualizationController] Sculpt guide skipped: missing volume or workspace.",
                    1f,
                    this);
            }
            else
            {
                _sculptGuideRenderer.enabled = true;
                if (_hasDepthFrame)
                {
                    _sculptGuideRenderer.UpdateDepthFrame(_depthFrame, data, visualizationConfig);
                }
                else
                {
                    SdfDebug.LogEvery(
                        "SdfVisualizationController.SculptGuideNoDepth",
                        "[SdfVisualizationController] Sculpt guide waiting for depth frame.",
                        1f,
                        this);
                }
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
