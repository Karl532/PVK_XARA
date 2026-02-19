public interface ISdfRenderer
{
    void UpdateRenderer(in SdfRendererContext context);
}

public readonly struct SdfRendererContext
{
    public readonly SdfVisualizationData Data;
    public readonly bool HasPointCloud;
    public readonly global::Assets.Scripts.Depth.Quest3.OXDepth.PointCloudData PointCloud;
    public readonly bool HasDepthFrame;
    public readonly DepthFrameData DepthFrame;

    public SdfRendererContext(
        SdfVisualizationData data,
        bool hasPointCloud,
        global::Assets.Scripts.Depth.Quest3.OXDepth.PointCloudData pointCloud,
        bool hasDepthFrame,
        DepthFrameData depthFrame)
    {
        Data = data;
        HasPointCloud = hasPointCloud;
        PointCloud = pointCloud;
        HasDepthFrame = hasDepthFrame;
        DepthFrame = depthFrame;
    }
}
