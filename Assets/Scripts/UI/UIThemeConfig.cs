using System;
using UnityEngine;

[CreateAssetMenu(menuName = "UI/UI Theme Config", fileName = "UIThemeConfig")]
public class UIThemeConfig : ScriptableObject
{
    [Serializable]
    public struct ThemeVariant
    {
        public Color backgroundColor;
        public Color accentColor;
        public Color secondaryColor;
        public Color textColor;
        public Color inactiveColor;
        public Color shadowColor;
    }

    [Header("Theme Variants")]
    public ThemeVariant darkTheme = new ThemeVariant
    {
        backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.95f),
        accentColor = new Color(0.3f, 0.5f, 0.9f, 1f),
        secondaryColor = new Color(0.15f, 0.15f, 0.2f, 0.9f),
        textColor = Color.white,
        inactiveColor = new Color(0.18f, 0.18f, 0.23f, 0.85f),
        shadowColor = new Color(0, 0, 0, 0.5f)
    };

    public ThemeVariant lightTheme = new ThemeVariant
    {
        backgroundColor = new Color(0.96f, 0.96f, 0.98f, 0.98f),
        accentColor = new Color(0.15f, 0.35f, 0.75f, 1f),
        secondaryColor = new Color(0.88f, 0.88f, 0.91f, 0.95f),
        textColor = new Color(0.08f, 0.08f, 0.12f, 1f),
        inactiveColor = new Color(0.75f, 0.75f, 0.8f, 0.95f),
        shadowColor = new Color(0, 0, 0, 0.15f)
    };

    [Header("Common Style")]
    public float cornerRadius = 25f;
    public bool useShadows = true;

    [Header("Canvas")]
    public Vector2 canvasSize = new Vector2(3000f, 3000f);
    public Vector3 canvasPosition = new Vector3(-0.4f, 1f, 0f);
    public float canvasScale = 0.001f;
    public float canvasDepth = 10f;
    public float dynamicPixelsPerUnit = 100f;

    [Header("Background Panel")]
    public Vector2 backgroundSize = new Vector2(3100f, 3100f);
    public Vector2 shadowEffectDistance = new Vector2(5f, -5f);
    public Vector2 glowEffectDistance = new Vector2(2f, 2f);

    [Header("Content Layout")]
    public Vector2 contentSize = new Vector2(3000f, 3000f);
    public float contentSpacing = 30f;
    public RectOffset contentPadding = new RectOffset(80, 80, 100, 100);

    [Header("Header")]
    public float headerHeight = 140f;
    public string headerTitle = "Settings";

    public ThemeVariant GetTheme(bool useLightTheme)
    {
        return useLightTheme ? lightTheme : darkTheme;
    }
}

