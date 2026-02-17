using UnityEngine;

public readonly struct DepthFrameData
{
    public readonly RenderTexture DepthTexture;
    public readonly Vector2Int Resolution;
    public readonly Matrix4x4 InvDepthViewProj;
    public readonly Matrix4x4 TrackingToWorld;
    public readonly int EyeSlice;
    public readonly bool FlipY;

    public DepthFrameData(
        RenderTexture depthTexture,
        Vector2Int resolution,
        Matrix4x4 invDepthViewProj,
        Matrix4x4 trackingToWorld,
        int eyeSlice,
        bool flipY)
    {
        DepthTexture = depthTexture;
        Resolution = resolution;
        InvDepthViewProj = invDepthViewProj;
        TrackingToWorld = trackingToWorld;
        EyeSlice = eyeSlice;
        FlipY = flipY;
    }
}
