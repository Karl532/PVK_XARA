using UnityEngine;

public static class RayHelperShaderIds
{
    public static readonly int DepthTex = Shader.PropertyToID("_DepthTex");
    public static readonly int DepthSize = Shader.PropertyToID("_DepthSize");
    public static readonly int EyeSlice = Shader.PropertyToID("_EyeSlice");
    public static readonly int FlipY = Shader.PropertyToID("_FlipY");
    public static readonly int Step = Shader.PropertyToID("_Step");
    public static readonly int Alpha = Shader.PropertyToID("_Alpha");
    public static readonly int MinDepth01 = Shader.PropertyToID("_MinDepth01");
    public static readonly int MaxDepth01 = Shader.PropertyToID("_MaxDepth01");
    public static readonly int WorldToWorkspaceScale = Shader.PropertyToID("_WorldToWorkspaceScale");
    public static readonly int ErrorScale = Shader.PropertyToID("_ErrorScale");
    public static readonly int RayStep = Shader.PropertyToID("_RayStep");
    public static readonly int MaxDistance = Shader.PropertyToID("_MaxDistance");
    public static readonly int MaxSteps = Shader.PropertyToID("_MaxSteps");
    public static readonly int HitThreshold = Shader.PropertyToID("_HitThreshold");
    public static readonly int InvDepthViewProj = Shader.PropertyToID("_InvDepthViewProj");
    public static readonly int TrackingToWorld = Shader.PropertyToID("_TrackingToWorld");
    public static readonly int WorldToWorkspace = Shader.PropertyToID("_WorldToWorkspace");
    public static readonly int CameraOriginWS = Shader.PropertyToID("_CameraOriginWS");
    public static readonly int GlobalTsdf3D = Shader.PropertyToID("_GlobalTsdf3D");
    public static readonly int GlobalCorner = Shader.PropertyToID("_GlobalCorner");
    public static readonly int GlobalSize = Shader.PropertyToID("_GlobalSize");
    public static readonly int GlobalMu = Shader.PropertyToID("_GlobalMu");
}
