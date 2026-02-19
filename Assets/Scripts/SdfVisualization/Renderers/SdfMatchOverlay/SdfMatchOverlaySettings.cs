using UnityEngine;

public readonly struct SdfMatchOverlaySettings
{
    public readonly bool Enabled;
    public readonly int Resolution;
    public readonly float Tolerance;
    public readonly float Alpha;
    public readonly float Decay;
    public readonly Color MatchColor;

    public static readonly SdfMatchOverlaySettings Default = new SdfMatchOverlaySettings(
        enabled: false,
        resolution: 128,
        tolerance: 0.01f,
        alpha: 0.6f,
        decay: 1f,
        matchColor: new Color(0.2f, 1f, 0.4f, 1f));

    public SdfMatchOverlaySettings(
        bool enabled,
        int resolution,
        float tolerance,
        float alpha,
        float decay,
        Color matchColor)
    {
        Enabled = enabled;
        Resolution = resolution;
        Tolerance = tolerance;
        Alpha = alpha;
        Decay = decay;
        MatchColor = matchColor;
    }

    public static SdfMatchOverlaySettings FromConfig(SdfVisualizationConfig config)
    {
        if (config == null)
            return Default;

        return new SdfMatchOverlaySettings(
            config.sdfMatchOverlayEnabled,
            Mathf.Clamp(config.sdfMatchOverlayResolution, 32, 256),
            Mathf.Max(1e-4f, config.sdfMatchOverlayTolerance),
            Mathf.Clamp01(config.sdfMatchOverlayAlpha),
            Mathf.Clamp01(config.sdfMatchOverlayDecay),
            config.sdfMatchOverlayColor);
    }
}
