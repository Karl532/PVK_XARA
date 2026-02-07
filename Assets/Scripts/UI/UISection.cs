using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UISection : MonoBehaviour
{
    public void CreateHeader(string title, float height, Color accentColor, Color textColor)
    {
        // Ensure RectTransform exists first
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = gameObject.AddComponent<RectTransform>();
        }

        rect.sizeDelta = new Vector2(1700, height);
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;

        // Add layout element for proper sizing
        LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;

        // Create background with gradient effect
        CreateHeaderBackground(accentColor);

        // Create title text
        CreateHeaderText(title, textColor);

        // Add subtle glow effect
        AddGlowEffect(accentColor);
    }

    void CreateHeaderBackground(Color accentColor)
    {
        // Add Image component
        Image background = gameObject.GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
        }

        // Create a subtle gradient background
        Color gradientTop = accentColor;
        Color gradientBottom = new Color(
            accentColor.r * 0.5f,
            accentColor.g * 0.5f,
            accentColor.b * 0.5f,
            accentColor.a
        );

        background.color = gradientTop;
        background.type = Image.Type.Sliced;

        // Add rounded corners effect using outline
        Outline outline = gameObject.GetComponent<Outline>();
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(1f, 1f, 1f, 0.2f);
        outline.effectDistance = new Vector2(0, -2);
    }

    void CreateHeaderText(string title, Color textColor)
    {
        GameObject textObj = new GameObject("HeaderText");
        textObj.transform.SetParent(transform);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.localPosition = Vector3.zero;
        textRect.localScale = Vector3.one;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = title;
        text.fontSize = 48;
        text.fontStyle = FontStyles.Bold;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;

        // Add subtle shadow for depth
        text.fontSharedMaterial = new Material(text.fontSharedMaterial);
        text.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");
        text.fontSharedMaterial.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.5f));
        text.fontSharedMaterial.SetFloat("_UnderlayOffsetX", 0.1f);
        text.fontSharedMaterial.SetFloat("_UnderlayOffsetY", -0.1f);
        text.fontSharedMaterial.SetFloat("_UnderlayDilate", 0.2f);
        text.fontSharedMaterial.SetFloat("_UnderlaySoftness", 0.3f);
    }

    void AddGlowEffect(Color accentColor)
    {
        Shadow glow = gameObject.GetComponent<Shadow>();
        if (glow == null)
        {
            glow = gameObject.AddComponent<Shadow>();
        }
        glow.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.6f);
        glow.effectDistance = new Vector2(0, 0);
        glow.useGraphicAlpha = true;
    }

    public void CreateSectionContainer(string title, float height, Color backgroundColor)
    {
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = gameObject.AddComponent<RectTransform>();
        }

        rect.sizeDelta = new Vector2(1700, height);
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

        // Add subtle border
        Outline border = gameObject.GetComponent<Outline>();
        if (border == null)
        {
            border = gameObject.AddComponent<Outline>();
        }
        border.effectColor = new Color(1f, 1f, 1f, 0.1f);
        border.effectDistance = new Vector2(1, 1);
    }
}