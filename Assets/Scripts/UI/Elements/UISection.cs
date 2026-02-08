using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UI.Elements.UICheckbox;
using UI.Elements.UIInputField;

public class UISection : MonoBehaviour
{
    public void CreateHeader(string title, float height, Color accentColor, Color textColor, float cornerRadius = 20f, float width = 2800f, float fontSize = 54f)
    {
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = gameObject.AddComponent<RectTransform>();
        }

        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;

        LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;

        CreateGradientHeaderBackground(accentColor, cornerRadius);
        CreateStyledHeaderText(title, textColor, fontSize);
        AddHeaderEffects(accentColor);
    }

    void CreateGradientHeaderBackground(Color accentColor, float cornerRadius)
    {
        Image background = gameObject.GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
        }

        Color gradientTop = accentColor;
        Color gradientBottom = new Color(
            accentColor.r * 0.7f,
            accentColor.g * 0.7f,
            accentColor.b * 0.7f,
            accentColor.a
        );

        background.color = gradientTop;
        background.type = Image.Type.Sliced;

        RoundedImage rounded = gameObject.GetComponent<RoundedImage>();
        if (rounded == null)
        {
            rounded = gameObject.AddComponent<RoundedImage>();
        }
        rounded.SetRadius(cornerRadius);

        Outline shine = gameObject.GetComponent<Outline>();
        if (shine == null)
        {
            shine = gameObject.AddComponent<Outline>();
        }
        shine.effectColor = new Color(1f, 1f, 1f, 0.25f);
        shine.effectDistance = new Vector2(0, 2);
    }

    void CreateStyledHeaderText(string title, Color textColor, float fontSize = 54f)
    {
        GameObject textObj = new GameObject("HeaderText");
        textObj.transform.SetParent(transform, false);

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

    void AddHeaderEffects(Color accentColor)
    {
        Shadow glow = gameObject.GetComponent<Shadow>();
        if (glow == null)
        {
            glow = gameObject.AddComponent<Shadow>();
        }
        glow.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.7f);
        glow.effectDistance = new Vector2(0, -4);
        glow.useGraphicAlpha = true;

        Shadow depth = gameObject.AddComponent<Shadow>();
        depth.effectColor = new Color(0, 0, 0, 0.4f);
        depth.effectDistance = new Vector2(0, -6);
        depth.useGraphicAlpha = true;
    }

    public void CreateSectionContainer(string title, float height, Color backgroundColor, float cornerRadius = 15f, float width = 1700f)
    {
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = gameObject.AddComponent<RectTransform>();
        }

        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;

        LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;

        Image background = gameObject.GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
        }
        background.color = backgroundColor;

        RoundedImage rounded = gameObject.GetComponent<RoundedImage>();
        if (rounded == null)
        {
            rounded = gameObject.AddComponent<RoundedImage>();
        }
        rounded.SetRadius(cornerRadius);

        Outline border = gameObject.GetComponent<Outline>();
        if (border == null)
        {
            border = gameObject.AddComponent<Outline>();
        }
        border.effectColor = new Color(1f, 1f, 1f, 0.15f);
        border.effectDistance = new Vector2(1, 1);

        Shadow innerShadow = gameObject.AddComponent<Shadow>();
        innerShadow.effectColor = new Color(0, 0, 0, 0.25f);
        innerShadow.effectDistance = new Vector2(0, 2);
    }

    public void CreateCard(string title, float height, Color cardColor, Color accentColor, float cornerRadius = 18f, float width = 1700f)
    {
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = gameObject.AddComponent<RectTransform>();
        }

        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;

        LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;

        Image background = gameObject.GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
        }
        background.color = cardColor;

        RoundedImage rounded = gameObject.GetComponent<RoundedImage>();
        if (rounded == null)
        {
            rounded = gameObject.AddComponent<RoundedImage>();
        }
        rounded.SetRadius(cornerRadius);

        Outline accentBorder = gameObject.GetComponent<Outline>();
        if (accentBorder == null)
        {
            accentBorder = gameObject.AddComponent<Outline>();
        }
        accentBorder.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.5f);
        accentBorder.effectDistance = new Vector2(2, 2);

        Shadow dropShadow = gameObject.AddComponent<Shadow>();
        dropShadow.effectColor = new Color(0, 0, 0, 0.4f);
        dropShadow.effectDistance = new Vector2(0, -5);
        dropShadow.useGraphicAlpha = true;
    }

    // --- Static layout factory methods (moved from UIManager) ---

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
    public static GameObject CreateCheckboxElement(
        Transform parent,
        string name,
        string label,
        Color accentColor,
        bool defaultValue,
        UnityEngine.Events.UnityAction<bool> onValueChanged,
        float height = 120f,
        float width = -1f
    )
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

    public static GameObject CreateDropdownElement(Transform parent, string name, string label, List<string> options, Color accentColor, float height = 220f, float width = -1f)
    {
        GameObject section = CreateInputSection(parent, name, height, width);
        UIDropdown dropdown = section.AddComponent<UIDropdown>();
        dropdown.CreateDropdown(label, options, accentColor);
        return section;
    }
}
