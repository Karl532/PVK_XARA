using UnityEngine;

public readonly struct SculptGuideSettings
{
    public readonly float Alpha;
    public readonly bool RenderPoints;
    public readonly float PointSizePx;
    public readonly int PointRenderMaxCount;
    public readonly int PointRenderInterval;
    public readonly bool EnableCache;
    public readonly int CacheResolution;
    public readonly int CacheDownsample;
    public readonly int CacheMinVoxelSpacing;
    public readonly float CacheUpdateSeconds;
    public readonly float CacheAccumulationSeconds;
    public readonly int CachePointBatchSize;
    public readonly bool CacheCarveEnabled;
    public readonly int CacheCarveInterval;
    public readonly int CacheCarveDownsample;
    public readonly int CacheCarveMaxSteps;
    public readonly bool CacheCarveUseDepthTexture;
    public readonly bool BetweenEnabled;
    public readonly int BetweenUpdateInterval;
    public readonly int BetweenDownsample;
    public readonly int BetweenMaxSteps;
    public readonly float BetweenStepWorld;
    public readonly float BetweenMaxDistance;
    public readonly float BetweenNearDistance;
    public readonly int BetweenMaxSegments;
    public readonly bool MeshEnabled;
    public readonly int MeshStep;
    public readonly float MeshSmoothingAlpha;

    public static SculptGuideSettings Default => new SculptGuideSettings(
        0.5f,
        true,
        3f,
        50000,
        2,
        false,
        128,
        2,
        1,
        1f,
        1f,
        50000,
        false,
        2,
        2,
        96,
        true,
        false,
        2,
        2,
        96,
        0f,
        0f,
        0f,
        50000,
        false,
        4,
        0.8f);

    private SculptGuideSettings(
        float alpha,
        bool renderPoints,
        float pointSizePx,
        int pointRenderMaxCount,
        int pointRenderInterval,
        bool enableCache,
        int cacheResolution,
        int cacheDownsample,
        int cacheMinVoxelSpacing,
        float cacheUpdateSeconds,
        float cacheAccumulationSeconds,
        int cachePointBatchSize,
        bool cacheCarveEnabled,
        int cacheCarveInterval,
        int cacheCarveDownsample,
        int cacheCarveMaxSteps,
        bool cacheCarveUseDepthTexture,
        bool betweenEnabled,
        int betweenUpdateInterval,
        int betweenDownsample,
        int betweenMaxSteps,
        float betweenStepWorld,
        float betweenMaxDistance,
        float betweenNearDistance,
        int betweenMaxSegments,
        bool meshEnabled,
        int meshStep,
        float meshSmoothingAlpha)
    {
        Alpha = alpha;
        RenderPoints = renderPoints;
        PointSizePx = pointSizePx;
        PointRenderMaxCount = pointRenderMaxCount;
        PointRenderInterval = pointRenderInterval;
        EnableCache = enableCache;
        CacheResolution = cacheResolution;
        CacheDownsample = cacheDownsample;
        CacheMinVoxelSpacing = cacheMinVoxelSpacing;
        CacheUpdateSeconds = cacheUpdateSeconds;
        CacheAccumulationSeconds = cacheAccumulationSeconds;
        CachePointBatchSize = cachePointBatchSize;
        CacheCarveEnabled = cacheCarveEnabled;
        CacheCarveInterval = cacheCarveInterval;
        CacheCarveDownsample = cacheCarveDownsample;
        CacheCarveMaxSteps = cacheCarveMaxSteps;
        CacheCarveUseDepthTexture = cacheCarveUseDepthTexture;
        BetweenEnabled = betweenEnabled;
        BetweenUpdateInterval = betweenUpdateInterval;
        BetweenDownsample = betweenDownsample;
        BetweenMaxSteps = betweenMaxSteps;
        BetweenStepWorld = betweenStepWorld;
        BetweenMaxDistance = betweenMaxDistance;
        BetweenNearDistance = betweenNearDistance;
        BetweenMaxSegments = betweenMaxSegments;
        MeshEnabled = meshEnabled;
        MeshStep = meshStep;
        MeshSmoothingAlpha = meshSmoothingAlpha;
    }

    public static SculptGuideSettings FromConfig(SdfVisualizationConfig config, int fallbackMeshStep)
    {
        if (config == null)
        {
            return new SculptGuideSettings(
                Default.Alpha,
                Default.RenderPoints,
                Default.PointSizePx,
                Default.PointRenderMaxCount,
                Default.PointRenderInterval,
                Default.EnableCache,
                Default.CacheResolution,
                Default.CacheDownsample,
                Default.CacheMinVoxelSpacing,
                Default.CacheUpdateSeconds,
                Default.CacheAccumulationSeconds,
                Default.CachePointBatchSize,
                Default.CacheCarveEnabled,
                Default.CacheCarveInterval,
                Default.CacheCarveDownsample,
                Default.CacheCarveMaxSteps,
                Default.CacheCarveUseDepthTexture,
                Default.BetweenEnabled,
                Default.BetweenUpdateInterval,
                Default.BetweenDownsample,
                Default.BetweenMaxSteps,
                Default.BetweenStepWorld,
                Default.BetweenMaxDistance,
                Default.BetweenNearDistance,
                Default.BetweenMaxSegments,
                Default.MeshEnabled,
                Default.MeshStep,
                Default.MeshSmoothingAlpha);
        }

        return new SculptGuideSettings(
            Mathf.Clamp01(config.sculptGuideAlpha),
            config.sculptGuideRenderPoints,
            Mathf.Clamp(config.sculptGuidePointSizePx, 1f, 12f),
            Mathf.Clamp(config.sculptGuidePointRenderMaxCount, 10000, 500000),
            Mathf.Clamp(config.sculptGuidePointRenderInterval, 1, 10),
            config.sculptGuideEnableCache,
            Mathf.Clamp(config.sculptGuideCacheResolution, 32, 256),
            Mathf.Clamp(config.sculptGuideCacheDownsample, 1, 8),
            Mathf.Clamp(config.sculptGuideCacheMinVoxelSpacing, 0, 4),
            Mathf.Max(0f, config.sculptGuideCacheUpdateSeconds),
            Mathf.Max(0f, config.sculptGuideCacheAccumulationSeconds),
            Mathf.Clamp(config.sculptGuideCachePointBatchSize, 1000, 200000),
            config.sculptGuideCacheCarveEnabled,
            Mathf.Clamp(config.sculptGuideCacheCarveInterval, 1, 10),
            Mathf.Clamp(config.sculptGuideCacheCarveDownsample, 1, 8),
            Mathf.Clamp(config.sculptGuideCacheCarveMaxSteps, 8, 512),
            config.sculptGuideCacheCarveUseDepthTexture,
            config.sculptGuideBetweenEnabled,
            Mathf.Clamp(config.sculptGuideBetweenUpdateInterval, 1, 10),
            Mathf.Clamp(config.sculptGuideBetweenDownsample, 1, 8),
            Mathf.Clamp(config.sculptGuideBetweenMaxSteps, 8, 512),
            Mathf.Max(0f, config.sculptGuideBetweenStepWorld),
            Mathf.Max(0f, config.sculptGuideBetweenMaxDistance),
            Mathf.Max(0f, config.sculptGuideBetweenNearDistance),
            Mathf.Clamp(config.sculptGuideBetweenMaxSegments, 1000, 200000),
            config.sculptGuideMeshEnabled,
            Mathf.Clamp(config.sculptGuideMeshStep, 1, 16),
            Mathf.Clamp01(config.sculptGuideMeshSmoothingAlpha));
    }
}
