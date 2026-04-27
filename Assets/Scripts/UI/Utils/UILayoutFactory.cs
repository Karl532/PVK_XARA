using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UI.Elements.UICheckbox;
using UI.Elements.UIDropdown;
using UI.Elements.UIRebindableKeyBinding;
using UI.Elements.UISlider;
using UI.Utils;

/// <summary>
/// Static factory for creating layout containers and styled elements. All methods take a parent/target as first parameter.
/// </summary>
public static class UILayoutFactory
{
    // --- Layout containers ---

    public static GameObject CreateLayoutSection(Transform parent, string name, float height)
    {
        GameObject section = new GameObject($"Section_{name}");
        section.transform.SetParent(parent, false);

        RectTransform rect = section.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(1, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0, height);
        rect.localScale = Vector3.one;

        LayoutElement layoutElement = section.AddComponent<LayoutElement>();
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;

        return section;
    }

    public static GameObject CreateInputSection(Transform parent, string name, float height, float width = -1f)
    {
        GameObject section = new GameObject($"InputSection_{name}");
        section.transform.SetParent(parent, false);

        RectTransform rect = section.AddComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;

        LayoutElement layoutElement = section.AddComponent<LayoutElement>();
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;

        if (width > 0)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            layoutElement.minWidth = width;
            layoutElement.preferredWidth = width;
        }
        else
        {
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.sizeDelta = new Vector2(-100, height);
        }

        Image bg = section.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0);

        return section;
    }

    public static GameObject CreateHorizontalRow(Transform parent, float height, float spacing = 20f, string name = "Row")
    {
        GameObject row = new GameObject($"HorizontalRow_{name}");
        row.transform.SetParent(parent, false);

        RectTransform rect = row.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(1, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0, height);
        rect.localScale = Vector3.one;

        LayoutElement layoutElement = row.AddComponent<LayoutElement>();
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return row;
    }

    public static GameObject CreateFixedRow(Transform parent, string name, float width, float height, float spacing = 24f, TextAnchor alignment = TextAnchor.UpperLeft)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);

        RectTransform rect = row.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.sizeDelta = new Vector2(width, height);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = alignment;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        LayoutElement le = row.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;

        return row;
    }

    public static GameObject CreateSpacer(Transform parent, float height, string name = "Spacer")
    {
        GameObject spacer = new GameObject(name);
        spacer.transform.SetParent(parent, false);
        LayoutElement le = spacer.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
        le.flexibleHeight = 0;
        return spacer;
    }

    public static GameObject CreateRowSpacer(Transform parent, float width = 20f, string name = "RowSpacer")
    {
        GameObject spacer = new GameObject(name);
        spacer.transform.SetParent(parent, false);
        LayoutElement le = spacer.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.flexibleWidth = 1f;
        return spacer;
    }

    // --- Element creation (layout + component wiring) ---

    public static GameObject CreateCheckboxElement(
        Transform parent,
        string name,
        string label,
        Color accentColor,
        bool defaultValue,
        UnityEngine.Events.UnityAction<bool> onValueChanged,
        float height = 120f,
        float width = -1f)
    {
        GameObject section = CreateInputSection(parent, name, height, width);

        UICheckbox checkbox = section.AddComponent<UICheckbox>();
        checkbox.CreateCheckbox(
            label,
            accentColor,
            defaultValue,
            onValueChanged
        );

        return section;
    }

    public static GameObject CreateCheckboxToggle(
        Transform parent,
        string name,
        string label,
        Color accentColor,
        Color textColor,
        System.Func<bool> getValue,
        System.Action<bool> setValue,
        float width,
        float height = 70f)
    {
        GameObject toggleGO = new GameObject(name);
        toggleGO.transform.SetParent(parent, false);

        LayoutElement le = toggleGO.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.minHeight = height;
        le.preferredHeight = height;

        UICheckbox checkbox = toggleGO.AddComponent<UICheckbox>();
        checkbox.CreateCheckbox(
            label,
            accentColor,
            getValue(),
            (isOn) => setValue(isOn),
            textColor
        );

        return toggleGO;
    }

    public static GameObject CreateDropdownElement(Transform parent, string name, string label, List<string> options, Color accentColor, float height = 220f, float width = -1f, UnityEngine.Events.UnityAction<int> onValueChanged = null)
    {
        GameObject section = CreateInputSection(parent, name, height, width);
        UIDropdown dropdown = section.AddComponent<UIDropdown>();
        dropdown.CreateDropdown(label, options, accentColor, onValueChanged);
        return section;
    }

    public static GameObject CreateKeyBindingElement(
        Transform parent,
        string name,
        string label,
        OVRInput.Button defaultButton,
        UnityEngine.Events.UnityAction onAction,
        Color accentColor,
        Color textColor,
        float height = 100f,
        float width = 1700f)
    {
        GameObject section = CreateInputSection(parent, name, height, -1f);
        LayoutElement le = section.GetComponent<LayoutElement>();
        RectTransform rect = section.GetComponent<RectTransform>();
        if (le != null)
            le.flexibleWidth = 1;
        if (rect != null)
            rect.sizeDelta = new Vector2(0, height);
        UIRebindableKeyBinding binding = section.AddComponent<UIRebindableKeyBinding>();
        binding.Create(label, defaultButton, onAction, accentColor, textColor);
        return section;
    }

    public static GameObject CreateSliderElement(
        Transform parent,
        string name,
        string label,
        float minValue,
        float maxValue,
        float defaultValue,
        UnityEngine.Events.UnityAction<float> onValueChanged,
        Color accentColor,
        Color textColor,
        float height = 120f,
        float width = -1f,
        float tickStep = 0f)
    {
        GameObject section = CreateInputSection(parent, name, height, width);
        UISlider slider = section.AddComponent<UISlider>();
        slider.Create(label, minValue, maxValue, defaultValue, onValueChanged, accentColor, textColor, height, true, tickStep);
        return section;
    }

    /// <summary>
    /// Creates a styled full-width action button.
    /// </summary>
    public static GameObject CreateButton(
        Transform parent,
        string name,
        string label,
        Color accentColor,
        Color textColor,
        System.Action onClick,
        float height = 100f,
        float width = -1f)
    {
        GameObject section = CreateInputSection(parent, name, height, width);

        // Background
        var bg = section.AddComponent<Image>();
        bg.color = new Color(accentColor.r * 0.3f, accentColor.g * 0.3f, accentColor.b * 0.3f, 0.95f);

        var rounded = section.AddComponent<RoundedImage>();
        rounded.SetRadius(14f);

        var outline = section.AddComponent<Outline>();
        outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.7f);
        outline.effectDistance = new Vector2(2, 2);

        // Button component
        var btn = section.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.3f);
        colors.pressedColor     = new Color(accentColor.r, accentColor.g, accentColor.b, 0.6f);
        colors.selectedColor    = Color.white;
        colors.fadeDuration     = 0.1f;
        btn.colors = colors;
        btn.targetGraphic = bg;
        btn.onClick.AddListener(() => onClick?.Invoke());

        // Label text
        var textGO = new GameObject("ButtonLabel");
        textGO.transform.SetParent(section.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16, 0);
        textRect.offsetMax = new Vector2(-16, 0);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 32f;
        tmp.color     = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = false;

        return section;
    }

    // --- Styling existing GameObjects ---

    public static void CreateHeader(GameObject target, string title, float height, Color accentColor, Color textColor, float cornerRadius = 20f, float width = 2800f, float fontSize = 54f)
    {
        RectTransform rect = UIComponentHelper.GetOrAddComponent<RectTransform>(target);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;

        LayoutElement layoutElement = UIComponentHelper.GetOrAddComponent<LayoutElement>(target);
        layoutElement.minWidth = width;
        layoutElement.preferredWidth = width;
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;

        CreateHeaderBackground(target, accentColor, cornerRadius);
        CreateHeaderText(target.transform, title, textColor, fontSize);
        AddHeaderEffects(target, accentColor);
    }

    public static void CreateSectionContainer(GameObject target, float height, Color backgroundColor, float cornerRadius = 15f, float width = 1700f)
    {
        RectTransform rect = UIComponentHelper.GetOrAddComponent<RectTransform>(target);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;

        LayoutElement layoutElement = UIComponentHelper.GetOrAddComponent<LayoutElement>(target);
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;

        Image background = UIComponentHelper.GetOrAddComponent<Image>(target);
        background.color = backgroundColor;

        RoundedImage rounded = UIComponentHelper.GetOrAddComponent<RoundedImage>(target);
        rounded.SetRadius(cornerRadius);

        Outline border = UIComponentHelper.GetOrAddComponent<Outline>(target);
        border.effectColor = new Color(1f, 1f, 1f, 0.15f);
        border.effectDistance = new Vector2(1, 1);

        Shadow innerShadow = target.AddComponent<Shadow>();
        innerShadow.effectColor = new Color(0, 0, 0, 0.25f);
        innerShadow.effectDistance = new Vector2(0, 2);
    }

    public static void CreateCard(GameObject target, float height, Color cardColor, Color accentColor, float cornerRadius = 18f, float width = 1700f)
    {
        RectTransform rect = UIComponentHelper.GetOrAddComponent<RectTransform>(target);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;

        LayoutElement layoutElement = UIComponentHelper.GetOrAddComponent<LayoutElement>(target);
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;

        Image background = UIComponentHelper.GetOrAddComponent<Image>(target);
        background.color = cardColor;

        RoundedImage rounded = UIComponentHelper.GetOrAddComponent<RoundedImage>(target);
        rounded.SetRadius(cornerRadius);

        Outline accentBorder = UIComponentHelper.GetOrAddComponent<Outline>(target);
        accentBorder.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.5f);
        accentBorder.effectDistance = new Vector2(2, 2);

        Shadow dropShadow = target.AddComponent<Shadow>();
        dropShadow.effectColor = new Color(0, 0, 0, 0.4f);
        dropShadow.effectDistance = new Vector2(0, -5);
        dropShadow.useGraphicAlpha = true;
    }

    static void CreateHeaderBackground(GameObject target, Color accentColor, float cornerRadius)
    {
        Image background = UIComponentHelper.GetOrAddComponent<Image>(target);
        background.color = accentColor;
        background.type = Image.Type.Sliced;

        RoundedImage rounded = UIComponentHelper.GetOrAddComponent<RoundedImage>(target);
        rounded.SetRadius(cornerRadius);

        Outline shine = UIComponentHelper.GetOrAddComponent<Outline>(target);
        shine.effectColor = new Color(1f, 1f, 1f, 0.25f);
        shine.effectDistance = new Vector2(0, 2);
    }

    static void CreateHeaderText(Transform parent, string title, Color textColor, float fontSize)
    {
        GameObject textObj = new GameObject("HeaderText");
        textObj.transform.SetParent(parent, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.localPosition = Vector3.zero;
        textRect.localScale = Vector3.one;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = title;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;

        text.fontSharedMaterial = new Material(text.fontSharedMaterial);
        text.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");
        text.fontSharedMaterial.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.6f));
        text.fontSharedMaterial.SetFloat("_UnderlayOffsetX", 0.15f);
        text.fontSharedMaterial.SetFloat("_UnderlayOffsetY", -0.15f);
        text.fontSharedMaterial.SetFloat("_UnderlayDilate", 0.3f);
        text.fontSharedMaterial.SetFloat("_UnderlaySoftness", 0.4f);
    }

    static void AddHeaderEffects(GameObject target, Color accentColor)
    {
        Shadow glow = UIComponentHelper.GetOrAddComponent<Shadow>(target);
        glow.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.7f);
        glow.effectDistance = new Vector2(0, -4);
        glow.useGraphicAlpha = true;

        Shadow depth = target.AddComponent<Shadow>();
        depth.effectColor = new Color(0, 0, 0, 0.4f);
        depth.effectDistance = new Vector2(0, -6);
        depth.useGraphicAlpha = true;
    }
}
