using UnityEngine;

public readonly struct SdfDepthErrorSettings
{
    public readonly bool Enabled;
    public readonly int Step;
    public readonly float Alpha;
    public readonly float RayStepWorld;
    public readonly int MaxSteps;
    public readonly float MaxDistance;
    public readonly float ErrorScale;
    public readonly float HitThreshold;

    public static readonly SdfDepthErrorSettings Default = new SdfDepthErrorSettings(
        enabled: false,
        step: 2,
        alpha: 0.8f,
        rayStepWorld: 0.01f,
        maxSteps: 96,
        maxDistance: 2f,
        errorScale: 0.05f,
        hitThreshold: 0.01f);

    public SdfDepthErrorSettings(
        bool enabled,
        int step,
        float alpha,
        float rayStepWorld,
        int maxSteps,
        float maxDistance,
        float errorScale,
        float hitThreshold)
    {
        Enabled = enabled;
        Step = step;
        Alpha = alpha;
        RayStepWorld = rayStepWorld;
        MaxSteps = maxSteps;
        MaxDistance = maxDistance;
        ErrorScale = errorScale;
        HitThreshold = hitThreshold;
    }

    public static SdfDepthErrorSettings FromConfig(SdfVisualizationConfig config)
    {
        if (config == null)
            return Default;

        return new SdfDepthErrorSettings(
            config.depthErrorEnabled,
            Mathf.Max(1, config.depthErrorStep),
            Mathf.Clamp01(config.depthErrorAlpha),
            Mathf.Max(1e-4f, config.depthErrorRayStep),
            Mathf.Max(1, config.depthErrorMaxSteps),
            Mathf.Max(0.01f, config.depthErrorMaxDistance),
            Mathf.Max(1e-4f, config.depthErrorErrorScale),
            Mathf.Max(1e-4f, config.depthErrorHitThreshold));
    }
}
