using UnityEngine;

public partial class SdfGenerationSystem
{
    public void ConfigureDebug(bool enabled, bool verbose, bool timing, float logIntervalSeconds)
    {
        enableDebug = enabled;
        verboseDebug = verbose;
        timingDebug = timing;
        debugLogIntervalSeconds = Mathf.Max(0f, logIntervalSeconds);
        SdfDebug.Configure(enableDebug, verboseDebug, timingDebug, debugLogIntervalSeconds);
        SdfDebug.Log("[SdfGenerationSystem] ConfigureDebug applied.", this);
    }

    public void ConfigurePerf(bool disableLocalVolumeOption)
    {
        disableLocalVolume = disableLocalVolumeOption;
        if (_core != null)
            _core.EnableLocalVolume = !disableLocalVolume;
        SdfDebug.Log($"[SdfGenerationSystem] ConfigurePerf applied. disableLocalVolume={disableLocalVolume}", this);
    }

    public void ConfigureLocalRebuildInterval(float seconds)
    {
        localRebuildMinIntervalSeconds = Mathf.Max(0f, seconds);
        if (_core != null)
            _core.LocalRebuildMinIntervalSeconds = localRebuildMinIntervalSeconds;
        SdfDebug.Log($"[SdfGenerationSystem] Local rebuild interval set to {localRebuildMinIntervalSeconds:0.###}s", this);
    }

    public void ConfigureGlobalRebuildInterval(float seconds)
    {
        globalRebuildMinIntervalSeconds = Mathf.Max(0f, seconds);
        if (_core != null)
            _core.GlobalRebuildMinIntervalSeconds = globalRebuildMinIntervalSeconds;
        SdfDebug.Log($"[SdfGenerationSystem] Global rebuild interval set to {globalRebuildMinIntervalSeconds:0.###}s", this);
    }

    public void ConfigureResolutions(int globalRes, int localRes)
    {
        globalResolution = globalRes;
        localResolution = localRes;
        if (_core != null)
            _core.SetResolutions(globalResolution, localResolution);
        SdfDebug.Log($"[SdfGenerationSystem] Resolutions set. global={globalResolution} local={localResolution}", this);
    }

    public void ConfigureBuildStages(int stagesPerFrame)
    {
        maxStagesPerFrame = Mathf.Max(1, stagesPerFrame);
        if (_core != null)
            _core.MaxStagesPerFrame = maxStagesPerFrame;
        SdfDebug.Log($"[SdfGenerationSystem] Build stages per frame set to {maxStagesPerFrame}", this);
    }
}
