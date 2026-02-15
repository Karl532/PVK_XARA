using UnityEngine;

public static class SdfShaderLoader
{
    private const string PathPointsToWorkspace = "Assets/Resources/SDF/Compute/Converters/PointsToWorkspace.compute";
    private const string PathPointAabb = "Assets/Resources/SDF/Compute/Core/PointAABB.compute";
    private const string PathSdfClear = "Assets/Resources/SDF/Compute/Core/SdfClear.compute";
    private const string PathSdfVoxelize = "Assets/Resources/SDF/Compute/Core/SdfVoxeliseSeeds.compute";
    private const string PathSdfJumpFlood = "Assets/Resources/SDF/Compute/Core/SdfJumpFlood.compute";
    private const string PathSdfFinalize = "Assets/Resources/SDF/Compute/Core/SdfFinalize.compute";
    private const string PathSdfSlice = "Assets/Resources/SDF/Compute/Visualization/SdfSlice.compute";
    private const string PathSdfOverlay = "Assets/Resources/SDF/Compute/Visualization/SdfOverlay.compute";

    public static SdfShaderSet LoadAll()
    {
        return new SdfShaderSet
        {
            PointsToWorkspace = LoadComputeShader(PathPointsToWorkspace, "SDF/Compute/Converters/PointsToWorkspace"),
            PointAabb = LoadComputeShader(PathPointAabb, "SDF/Compute/Core/PointAABB"),
            SdfClear = LoadComputeShader(PathSdfClear, "SDF/Compute/Core/SdfClear"),
            SdfVoxelizeSeeds = LoadComputeShader(PathSdfVoxelize, "SDF/Compute/Core/SdfVoxeliseSeeds"),
            SdfJumpFlood = LoadComputeShader(PathSdfJumpFlood, "SDF/Compute/Core/SdfJumpFlood"),
            SdfFinalize = LoadComputeShader(PathSdfFinalize, "SDF/Compute/Core/SdfFinalize"),
            SdfSlice = LoadComputeShader(PathSdfSlice, "SDF/Compute/Visualization/SdfSlice"),
            SdfOverlay = LoadComputeShader(PathSdfOverlay, "SDF/Compute/Visualization/SdfOverlay")
        };
    }

    private static ComputeShader LoadComputeShader(string assetPath, string resourceName)
    {
#if UNITY_EDITOR
        var editorCs = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(assetPath);
        if (editorCs != null) return editorCs;
#endif
        var cs = Resources.Load<ComputeShader>(resourceName);
        if (cs == null)
        {
                Debug.LogWarning($"[SdfSystem] Could not load compute shader '{resourceName}'. " +
                                 "Ensure it exists under Assets/Resources.");
        }
        return cs;
    }
}

public struct SdfShaderSet
{
    public ComputeShader PointsToWorkspace;
    public ComputeShader PointAabb;
    public ComputeShader SdfClear;
    public ComputeShader SdfVoxelizeSeeds;
    public ComputeShader SdfJumpFlood;
    public ComputeShader SdfFinalize;
    public ComputeShader SdfSlice;
    public ComputeShader SdfOverlay;
}
