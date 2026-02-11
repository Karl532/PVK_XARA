using System;
using UnityEngine;
using UnityEngine.UI;

public static class UIContentBuilder
{
    public static GameObject BuildContent(GameObject canvasObject, UIThemeConfig themeConfig, bool useLightMode)
    {
        if (canvasObject == null) throw new ArgumentNullException(nameof(canvasObject));
        if (themeConfig == null) throw new ArgumentNullException(nameof(themeConfig));

        var theme = themeConfig.GetTheme(useLightMode);

        CreateBackgroundPanel(canvasObject.transform, themeConfig, theme);
        GameObject contentPanel = CreateContentPanel(canvasObject.transform, themeConfig);

        // Header
        GameObject headerGO = UILayoutFactory.CreateLayoutSection(contentPanel.transform, "Header", themeConfig.headerHeight);
        UILayoutFactory.CreateHeader(
            headerGO,
            themeConfig.headerTitle,
            themeConfig.headerHeight,
            theme.accentColor,
            theme.textColor,
            themeConfig.cornerRadius);

        return contentPanel;
    }

    private static void CreateBackgroundPanel(Transform parent, UIThemeConfig config, UIThemeConfig.ThemeVariant theme)
    {
        GameObject bgPanel = new GameObject("BackgroundPanel");
        bgPanel.transform.SetParent(parent);

        RectTransform bgRect = bgPanel.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = config.backgroundSize;
        bgRect.localPosition = Vector3.zero;
        bgRect.localScale = Vector3.one;

        Image bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = theme.backgroundColor;

        RoundedImage rounded = bgPanel.AddComponent<RoundedImage>();
        rounded.SetRadius(config.cornerRadius);

        if (config.useShadows)
        {
            Shadow shadow = bgPanel.AddComponent<Shadow>();
            shadow.effectColor = theme.shadowColor;
            shadow.effectDistance = config.shadowEffectDistance;
            shadow.useGraphicAlpha = true;
        }

        Outline glow = bgPanel.AddComponent<Outline>();
        glow.effectColor = new Color(theme.accentColor.r, theme.accentColor.g, theme.accentColor.b, 0.3f);
        glow.effectDistance = config.glowEffectDistance;
    }

    private static GameObject CreateContentPanel(Transform parent, UIThemeConfig config)
    {
        GameObject contentPanel = new GameObject("ContentPanel");
        contentPanel.transform.SetParent(parent);

        RectTransform rect = contentPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = config.contentSize;
        rect.localPosition = Vector3.zero;
        rect.localScale = Vector3.one;

        VerticalLayoutGroup layout = contentPanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = config.contentSpacing;
        layout.padding = config.contentPadding;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return contentPanel;
    }
}

