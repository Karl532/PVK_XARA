using UnityEngine;

[CreateAssetMenu(fileName = "SdfOrchestratorConfig", menuName = "SDF/SdfOrchestratorConfig")]
public class SdfOrchestratorConfig : ScriptableObject
{
    [Header("Debug")]
    public bool enableDebug = true;
    public bool verboseDebug = true;
    public bool timingDebug = false;
    public float debugLogIntervalSeconds = 1f;

    [Header("SDF Debug/Perf (Forwarded to SdfGenerationSystem)")]
    public bool sdfDisableLocalVolume = false;
    public float sdfGlobalRebuildMinIntervalSeconds = 1f;
    public float sdfLocalRebuildMinIntervalSeconds = 1f;
    public int sdfGlobalResolution = 64;
    public int sdfLocalResolution = 128;
    public int sdfStagesPerFrame = 1;

}
