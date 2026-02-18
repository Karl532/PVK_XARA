using UnityEngine;

public readonly struct DepthFrameData
{
    public readonly RenderTexture DepthTexture;
    public readonly Vector2Int DepthResolution;
    public readonly Matrix4x4 InvDepthViewProj;
    public readonly Matrix4x4 TrackingToWorld;
    public readonly int EyeSlice;
    public readonly bool FlipY;

    public DepthFrameData(
        RenderTexture depthTexture,
        Vector2Int depthResolution,
        Matrix4x4 invDepthViewProj,
        Matrix4x4 trackingToWorld,
        int eyeSlice,
        bool flipY)
    {
        DepthTexture = depthTexture;
        DepthResolution = depthResolution;
        InvDepthViewProj = invDepthViewProj;
        TrackingToWorld = trackingToWorld;
        EyeSlice = eyeSlice;
        FlipY = flipY;
    }
}
