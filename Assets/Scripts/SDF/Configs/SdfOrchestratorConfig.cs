using UnityEngine;

[CreateAssetMenu(fileName = "SdfOrchestratorConfig", menuName = "SDF/SdfOrchestratorConfig")]
public class SdfOrchestratorConfig : ScriptableObject
{
    [Header("Overlay")]
    public bool overlayForceSolid = false;
    public Color overlayForceColor = new Color(1f, 0f, 1f, 0.35f);
    public bool overlayBlitAllCameras = true;
    public bool sculptGuideEnabled = true;
    public int overlayFullSdfGridResolution = 12;
    public float overlayFullSdfAlpha = 1.0f;
    public float overlayGridPointSizePx = 3f;
    public float sculptGuidePointSizePx = 3f;
    public float sculptGuideAlpha = 0.35f;
    [Tooltip("Show only points within this distance to the surface (meters). 0 disables filtering.")]
    public float sculptGuideSurfaceDistanceMeters = 0.02f;

    [Header("Debug")]
    public bool enableDebug = true;
    public bool verboseDebug = true;
    public bool timingDebug = false;
    public float debugLogIntervalSeconds = 1f;

    [Header("SDF Debug/Perf (Forwarded to SdfSystem)")]
    public bool sdfDisableFilterReadback = false;
    public bool sdfDisableLocalVolume = false;
    public float sdfGlobalRebuildMinIntervalSeconds = 1f;
    public float sdfLocalRebuildMinIntervalSeconds = 1f;
    public int sdfGlobalResolution = 64;
    public int sdfLocalResolution = 128;
    public int sdfStagesPerFrame = 1;

    [Header("Bounds Debug")]
    public bool showWorkspaceBounds = true;
    public float boundsLineWidth = 0.002f;
    public Color boundsColor = new Color(0f, 1f, 1f, 0.8f);
}
