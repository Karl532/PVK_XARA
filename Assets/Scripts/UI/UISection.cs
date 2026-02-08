using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        // Create gradient background
        CreateGradientHeaderBackground(accentColor, cornerRadius);

        // Create title text with better styling
        CreateStyledHeaderText(title, textColor, fontSize);

        // Add glow and depth effects
        AddHeaderEffects(accentColor);
    }

    void CreateGradientHeaderBackground(Color accentColor, float cornerRadius)
    {
        Image background = gameObject.GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
        }

        // Use a richer gradient color
        Color gradientTop = accentColor;
        Color gradientBottom = new Color(
            accentColor.r * 0.7f,
            accentColor.g * 0.7f,
            accentColor.b * 0.7f,
            accentColor.a
        );

        background.color = gradientTop;
        background.type = Image.Type.Sliced;

        // Add rounded corners
        RoundedImage rounded = gameObject.GetComponent<RoundedImage>();
        if (rounded == null)
        {
            rounded = gameObject.AddComponent<RoundedImage>();
        }
        rounded.SetRadius(cornerRadius);

        // Add shine effect with outline
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
        text.raycastTarget = false;

        // Enhanced text shadow with better parameters
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
        // Add soft glow effect
        Shadow glow = gameObject.GetComponent<Shadow>();
        if (glow == null)
        {
            glow = gameObject.AddComponent<Shadow>();
        }
        glow.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.7f);
        glow.effectDistance = new Vector2(0, -4);
        glow.useGraphicAlpha = true;

        // Add second shadow for depth
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

        // Add rounded corners
        RoundedImage rounded = gameObject.GetComponent<RoundedImage>();
        if (rounded == null)
        {
            rounded = gameObject.AddComponent<RoundedImage>();
        }
        rounded.SetRadius(cornerRadius);

        // Add subtle border with better visibility
        Outline border = gameObject.GetComponent<Outline>();
        if (border == null)
        {
            border = gameObject.AddComponent<Outline>();
        }
        border.effectColor = new Color(1f, 1f, 1f, 0.15f);
        border.effectDistance = new Vector2(1, 1);

        // Add inner shadow for depth
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

        // Add rounded corners
        RoundedImage rounded = gameObject.GetComponent<RoundedImage>();
        if (rounded == null)
        {
            rounded = gameObject.AddComponent<RoundedImage>();
        }
        rounded.SetRadius(cornerRadius);

        // Add colored accent border
        Outline accentBorder = gameObject.GetComponent<Outline>();
        if (accentBorder == null)
        {
            accentBorder = gameObject.AddComponent<Outline>();
        }
        accentBorder.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.5f);
        accentBorder.effectDistance = new Vector2(2, 2);

        // Add drop shadow
        Shadow dropShadow = gameObject.AddComponent<Shadow>();
        dropShadow.effectColor = new Color(0, 0, 0, 0.4f);
        dropShadow.effectDistance = new Vector2(0, -5);
        dropShadow.useGraphicAlpha = true;
    }
}