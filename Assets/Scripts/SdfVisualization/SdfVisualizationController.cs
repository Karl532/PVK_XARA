using UnityEngine;
using Assets.Scripts.Debug;
using Assets.Scripts.Depth.Quest3.OXDepth;

public class SdfVisualizationController : MonoBehaviour
{
    [Header("References")]

    private SdfBoundsRenderer _boundsRenderer;
    private SdfFullSdfGridRenderer _gridRenderer;
    private SdfSculptGuideRenderer _sculptGuideRenderer;
    private SdfDepthErrorRenderer _depthErrorRenderer;
    private SdfMatchOverlayRenderer _matchOverlayRenderer;
    private bool _ready;
    private SdfVisualizationData _lastData;
    private bool _hasData;
    private global::Assets.Scripts.Depth.Quest3.OXDepth.PointCloudData _pointCloud;
    private bool _hasPointCloud;
    private DepthFrameData _depthFrame;
    private bool _hasDepthFrame;

    private void Awake()
    {
        CreateRenderers();
    }

    public void Initialize()
    {
        var activeSettings = Settings.GetActive();
        if (activeSettings == null)
        {
            enabled = false;
            return;
        }

        _ready = true;
    }

    public void TickVisuals()
    {
        if (!_ready)
            return;

        if (_hasData)
        {
            var context = BuildContext(_lastData);
            _boundsRenderer.UpdateRenderer(context);
        }
    }

    public void SetPointCloud(global::Assets.Scripts.Depth.Quest3.OXDepth.PointCloudData data)
    {
        _pointCloud = data;
        _hasPointCloud = data.pointBuffer != null && data.pointCount > 0;
    }

    public void SetDepthFrame(DepthFrameData data)
    {
        _depthFrame = data;
        _hasDepthFrame = data.DepthTexture != null && data.DepthResolution.x > 0 && data.DepthResolution.y > 0;
        if (_matchOverlayRenderer != null)
            _matchOverlayRenderer.UpdateDepthFrame(_depthFrame);
    }

    public void OnVisualizationData(SdfVisualizationData data)
    {
        if (!_ready)
            return;

        DebugService.LogEvery(
            "SdfVisualizationController.OnVisualizationData",
            $"[SG_DEBUG] [SdfVisualizationController] OnVisualizationData: globalValid={data.Global.IsValid} workspaceRoot={(data.WorkspaceRoot != null)} size={data.WorkspaceSize}",
            1f,
            this);

        _lastData = data;
        _hasData = true;
        var context = BuildContext(data);
        UpdateRenderers(context);
    }

    private void CreateRenderers()
    {
        _sculptGuideRenderer = SdfRendererRegistry.GetOrCreate<SdfSculptGuideRenderer>("SdfSculptGuideRenderer", true, this);
        _gridRenderer = SdfRendererRegistry.GetOrCreate<SdfFullSdfGridRenderer>("SdfFullSdfGridRenderer", true, this);
        _depthErrorRenderer = SdfRendererRegistry.GetOrCreate<SdfDepthErrorRenderer>("SdfDepthErrorRenderer", true, this);
        _matchOverlayRenderer = SdfRendererRegistry.GetOrCreate<SdfMatchOverlayRenderer>("SdfMatchOverlayRenderer", true, this);
        _boundsRenderer = SdfRendererRegistry.GetOrCreate<SdfBoundsRenderer>("SdfBoundsRenderer", false, this);
    }

    private SdfRendererContext BuildContext(SdfVisualizationData data)
    {
        return new SdfRendererContext(
            data,
            _hasPointCloud,
            _pointCloud,
            _hasDepthFrame,
            _depthFrame);
    }

    private void UpdateRenderers(in SdfRendererContext context)
    {
        _gridRenderer.UpdateRenderer(context);
        _sculptGuideRenderer.UpdateRenderer(context);
        _depthErrorRenderer.UpdateRenderer(context);
        _matchOverlayRenderer.UpdateRenderer(context);
        _boundsRenderer.UpdateRenderer(context);
    }
}





