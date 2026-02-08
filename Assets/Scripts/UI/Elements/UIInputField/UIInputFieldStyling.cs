using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI.Utils;

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
        UIStylingHelper.CreateAccentLabel(parent, labelText, accentColor, fontSize);
    }

    /// <summary>
    /// Creates the text area and placeholder, and assigns them to the input field.
    /// InputType is applied as ContentType so the keyboard/field only accepts valid characters.
    /// </summary>
    public static void CreateTextArea(GameObject parent, string placeholder, InputType inputType, float fontSize, TMP_InputField targetInputField)
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

        targetInputField.contentType = TMP_InputField.ContentType.Standard;
        targetInputField.onValidateInput += (string currentText, int charIndex, char addedChar) => ValidateInput(inputType, currentText, charIndex, addedChar);
    }

    /// <summary>
    /// Returns a string containing only characters allowed for the given InputType. Use when text is set in bulk (e.g. from Spatial Keyboard).
    /// </summary>
    public static string SanitizeForInputType(string text, InputType inputType)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if (inputType == InputType.Standard) return text;

        var sb = new System.Text.StringBuilder(text.Length);
        bool hasDecimal = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            switch (inputType)
            {
                case InputType.IntegerNumber:
                case InputType.Pin:
                    if (char.IsDigit(c)) sb.Append(c);
                    break;
                case InputType.DecimalNumber:
                    if (char.IsDigit(c)) sb.Append(c);
                    else if ((c == '.' || c == ',') && !hasDecimal) { sb.Append(c); hasDecimal = true; }
                    break;
                case InputType.Alphanumeric:
                    if (char.IsLetterOrDigit(c)) sb.Append(c);
                    break;
                case InputType.Name:
                    if (char.IsLetter(c) || c == '\'' || char.IsWhiteSpace(c)) sb.Append(c);
                    break;
                case InputType.EmailAddress:
                    if (char.IsLetterOrDigit(c) || c == '@' || c == '.' || c == '_' || c == '-') sb.Append(c);
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Validates each character so only allowed characters can be entered (keyboard and paste). Return '\0' to reject.
    /// </summary>
    private static char ValidateInput(InputType inputType, string currentText, int charIndex, char addedChar)
    {
        switch (inputType)
        {
            case InputType.IntegerNumber:
                return char.IsDigit(addedChar) ? addedChar : '\0';
            case InputType.DecimalNumber:
                if (char.IsDigit(addedChar)) return addedChar;
                if (addedChar == '.' || addedChar == ',')
                {
                    if (currentText.IndexOf('.') >= 0 || currentText.IndexOf(',') >= 0) return '\0';
                    return addedChar;
                }
                return '\0';
            case InputType.Alphanumeric:
                return char.IsLetterOrDigit(addedChar) ? addedChar : '\0';
            case InputType.Pin:
                return char.IsDigit(addedChar) ? addedChar : '\0';
            case InputType.Name:
                return char.IsLetter(addedChar) || addedChar == '\'' || char.IsWhiteSpace(addedChar) ? addedChar : '\0';
            case InputType.EmailAddress:
                return char.IsLetterOrDigit(addedChar) || addedChar == '@' || addedChar == '.' || addedChar == '_' || addedChar == '-' ? addedChar : '\0';
            default:
                return addedChar;
        }
    }

    /// <summary>
    /// Applies color transition, caret, and selection styling to the input field.
    /// </summary>
    public static void StyleInputField(TMP_InputField inputField, Color accentColor)
    {
        UIStylingHelper.ApplyStandardSelectableColors(inputField);

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
        UIStylingHelper.AddOutlineAndShadow(inputContainer, accentColor);
    }
}
}
