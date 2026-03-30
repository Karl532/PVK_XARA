using UnityEngine;

public readonly struct DepthFrameData
{
    public readonly RenderTexture DepthTexture;
    public readonly Vector2Int DepthResolution;
    public readonly Matrix4x4 InvDepthViewProj;
    public readonly Matrix4x4 TrackingToWorld;
    public readonly int EyeSlice;
    public readonly bool FlipY;
    public readonly float MinDepth01;
    public readonly float MaxDepth01;

    public DepthFrameData(
        RenderTexture depthTexture,
        Vector2Int depthResolution,
        Matrix4x4 invDepthViewProj,
        Matrix4x4 trackingToWorld,
        int eyeSlice,
        bool flipY,
        float minDepth01,
        float maxDepth01)
    {
        DepthTexture = depthTexture;
        DepthResolution = depthResolution;
        InvDepthViewProj = invDepthViewProj;
        TrackingToWorld = trackingToWorld;
        EyeSlice = eyeSlice;
        FlipY = flipY;
        MinDepth01 = minDepth01;
        MaxDepth01 = maxDepth01;
    }
}
