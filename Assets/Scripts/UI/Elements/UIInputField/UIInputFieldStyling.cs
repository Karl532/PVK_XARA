using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.Elements.UIInputField
{
    /// <summary>
    /// Handles visual construction and styling for <see cref="UIInputField"/>.
    /// </summary>
    public static class UIInputFieldStyling
    {
    public const float DefaultLabelFontSize = 42f;
    public const float DefaultInputFontSize = 38f;

    /// <summary>
    /// Creates the label above the input area.
    /// </summary>
    public static void CreateLabel(Transform parent, string labelText, Color accentColor, float fontSize)
    {
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(parent, false);

        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.7f);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.offsetMin = new Vector2(20, 0);
        labelRect.offsetMax = new Vector2(-20, 0);
        labelRect.localScale = Vector3.one;

        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.color = accentColor;
        label.alignment = TextAlignmentOptions.Left;
    }

    /// <summary>
    /// Creates the text area and placeholder, and assigns them to the input field.
    /// </summary>
    public static void CreateTextArea(GameObject parent, string placeholder, bool isPassword, float fontSize, TMP_InputField targetInputField)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20, 10);
        textRect.offsetMax = new Vector2(-20, -10);
        textRect.localScale = Vector3.one;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;

        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(parent.transform, false);

        RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(20, 10);
        placeholderRect.offsetMax = new Vector2(-20, -10);
        placeholderRect.localScale = Vector3.one;

        TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = fontSize;
        placeholderText.color = new Color(1f, 1f, 1f, 0.4f);
        placeholderText.alignment = TextAlignmentOptions.Left;
        placeholderText.fontStyle = FontStyles.Italic;

        targetInputField.textComponent = text;
        targetInputField.placeholder = placeholderText;

        if (isPassword)
            targetInputField.contentType = TMP_InputField.ContentType.Password;
    }

    /// <summary>
    /// Applies color transition, caret, and selection styling to the input field.
    /// </summary>
    public static void StyleInputField(TMP_InputField inputField, Color accentColor)
    {
        inputField.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = inputField.colors;
        colors.normalColor = new Color(0.12f, 0.12f, 0.18f, 0.95f);
        colors.highlightedColor = new Color(0.18f, 0.18f, 0.25f, 0.95f);
        colors.pressedColor = new Color(0.15f, 0.15f, 0.22f, 0.95f);
        colors.selectedColor = new Color(0.18f, 0.18f, 0.25f, 0.95f);
        colors.disabledColor = new Color(0.08f, 0.08f, 0.12f, 0.5f);
        colors.colorMultiplier = 1.2f;
        inputField.colors = colors;

        inputField.caretWidth = 3;
        inputField.customCaretColor = true;
        inputField.caretColor = accentColor;
        inputField.selectionColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.5f);
    }

    /// <summary>
    /// Adds outline and shadow to the input container.
    /// </summary>
    public static void AddInteractionEffects(GameObject inputContainer, Color accentColor)
    {
        Outline outline = inputContainer.AddComponent<Outline>();
        outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.4f);
        outline.effectDistance = new Vector2(2, 2);

        Shadow shadow = inputContainer.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(3, -3);
    }
}
}
