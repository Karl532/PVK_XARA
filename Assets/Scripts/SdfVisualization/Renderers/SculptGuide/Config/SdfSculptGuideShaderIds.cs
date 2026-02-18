using UnityEngine;

public static class SdfSculptGuideShaderIds
{
    public static readonly int GlobalTsdf3D = Shader.PropertyToID("_GlobalTsdf3D");
    public static readonly int GlobalCorner = Shader.PropertyToID("_GlobalCorner");
    public static readonly int GlobalSize = Shader.PropertyToID("_GlobalSize");
    public static readonly int GlobalMu = Shader.PropertyToID("_GlobalMu");

    public static readonly int Alpha = Shader.PropertyToID("_Alpha");
    public static readonly int InsideColor = Shader.PropertyToID("_InsideColor");
    public static readonly int OutsideColor = Shader.PropertyToID("_OutsideColor");
    public static readonly int SurfaceColor = Shader.PropertyToID("_SurfaceColor");
    public static readonly int CacheTex = Shader.PropertyToID("_CacheTex");
    public static readonly int CacheResolution = Shader.PropertyToID("_CacheResolution");
    public static readonly int PointSizePx = Shader.PropertyToID("_PointSizePx");
    public static readonly int WorkspaceToWorld = Shader.PropertyToID("_WorkspaceToWorld");
    public static readonly int WorldToWorkspace = Shader.PropertyToID("_WorldToWorkspace");
    public static readonly int Points = Shader.PropertyToID("_Points");
    public static readonly int PointCount = Shader.PropertyToID("_PointCount");
    public static readonly int PointStride = Shader.PropertyToID("_PointStride");
}
