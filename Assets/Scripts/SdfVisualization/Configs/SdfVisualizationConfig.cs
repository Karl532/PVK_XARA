using UnityEngine;

[CreateAssetMenu(fileName = "SdfVisualizationConfig", menuName = "SDF/SdfVisualizationConfig")]
public class SdfVisualizationConfig : ScriptableObject
{
    [Header("Grid Overlay")]
    public int overlayFullSdfGridResolution = 12;
    public float overlayFullSdfAlpha = 1.0f;
    public float overlayGridPointSizePx = 3f;

    [Header("Sculpt Guide")]
    public float sculptGuideAlpha = 0.5f;
    public bool sculptGuideRenderPoints = true;
    [Range(1f, 12f)]
    public float sculptGuidePointSizePx = 3f;
    [Range(10000, 500000)]
    public int sculptGuidePointRenderMaxCount = 50000;
    [Range(1, 10)]
    public int sculptGuidePointRenderInterval = 1;

    [Header("Sculpt Guide Cache")]
    public bool sculptGuideEnableCache = true;
    [Range(32, 256)]
    public int sculptGuideCacheResolution = 128;
    [Range(1, 8)]
    public int sculptGuideCacheDownsample = 2;
    [Range(0, 4)]
    public int sculptGuideCacheMinVoxelSpacing = 1;
    [Min(0f)]
    public float sculptGuideCacheUpdateSeconds = 0f;
    [Min(0f)]
    public float sculptGuideCacheAccumulationSeconds = 0f;
    [Range(1000, 200000)]
    public int sculptGuideCachePointBatchSize = 50000;
    public bool sculptGuideCacheCarveUseDepthTexture = true;
    public bool sculptGuideCacheCarveEnabled = true;
    [Range(1, 10)]
    public int sculptGuideCacheCarveInterval = 2;
    [Range(1, 8)]
    public int sculptGuideCacheCarveDownsample = 2;
    [Range(8, 512)]
    public int sculptGuideCacheCarveMaxSteps = 96;

    [Header("Sculpt Guide Between Volume")]
    public bool sculptGuideBetweenEnabled = true;
    [Range(1, 10)]
    public int sculptGuideBetweenUpdateInterval = 2;
    [Range(1, 8)]
    public int sculptGuideBetweenDownsample = 2;
    [Range(8, 512)]
    public int sculptGuideBetweenMaxSteps = 96;
    [Min(0f)]
    public float sculptGuideBetweenStepWorld = 0f;
    [Min(0f)]
    public float sculptGuideBetweenMaxDistance = 0f;
    [Min(0f)]
    public float sculptGuideBetweenNearDistance = 0f;
    [Range(1000, 200000)]
    public int sculptGuideBetweenMaxSegments = 50000;

    [Header("Sculpt Guide Depth Mesh")]
    public bool sculptGuideMeshEnabled = true;
    [Range(1, 16)]
    public int sculptGuideMeshStep = 4;
    [Range(0f, 1f)]
    public float sculptGuideMeshSmoothingAlpha = 0.8f;

    [Header("Bounds")]
    public float boundsLineWidth = 0.002f;
    public Color boundsColor = new Color(0f, 1f, 1f, 0.8f);
}
