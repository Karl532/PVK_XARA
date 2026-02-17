using UnityEngine;

[CreateAssetMenu(fileName = "SdfVisualizationConfig", menuName = "SDF/SdfVisualizationConfig")]
public class SdfVisualizationConfig : ScriptableObject
{
    [Header("Grid Overlay")]
    public int overlayFullSdfGridResolution = 12;
    public float overlayFullSdfAlpha = 1.0f;
    public float overlayGridPointSizePx = 3f;

    [Header("Sculpt Guide")]
    public float sculptGuidePointSizePx = 3f;
    public float sculptGuideAlpha = 0.35f;

    [Header("Bounds")]
    public float boundsLineWidth = 0.002f;
    public Color boundsColor = new Color(0f, 1f, 1f, 0.8f);
}
