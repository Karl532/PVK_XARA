using UnityEngine;

public struct SdfShaderSet
{
    public ComputeShader PointsToWorkspace;
    public ComputeShader PointAabb;
    public ComputeShader SdfClear;
    public ComputeShader SdfVoxelizeSeeds;
    public ComputeShader SdfJumpFlood;
    public ComputeShader SdfFinalize;
    public ComputeShader SdfSlice;

    public bool IsValid =>
        PointsToWorkspace != null &&
        PointAabb != null &&
        SdfClear != null &&
        SdfVoxelizeSeeds != null &&
        SdfJumpFlood != null &&
        SdfFinalize != null;
}
