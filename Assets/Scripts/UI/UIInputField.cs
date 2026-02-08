using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class UIInputField : MonoBehaviour
{
    private TMP_InputField inputField;
    private Image backgroundImage;
    
    public void CreateInputField(string label, string placeholder, Color accentColor, bool isPassword = false, float labelFontSize = 42f, float inputFontSize = 38f)
    {
        // Create container
        RectTransform containerRect = gameObject.GetComponent<RectTransform>();
        if (containerRect == null)
        {
            containerRect = gameObject.AddComponent<RectTransform>();
        }

        // Create label
        CreateLabel(label, accentColor, labelFontSize);

        // Create input field container
        GameObject inputContainer = new GameObject("InputContainer");
        inputContainer.transform.SetParent(transform);

        RectTransform inputRect = inputContainer.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0, 0);
        inputRect.anchorMax = new Vector2(1, 0.65f);
        inputRect.offsetMin = new Vector2(0, 0);
        inputRect.offsetMax = new Vector2(0, 0);
        inputRect.localScale = Vector3.one;

        // Create input field background
        backgroundImage = inputContainer.AddComponent<Image>();
        backgroundImage.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

        // Add input field component
        inputField = inputContainer.AddComponent<TMP_InputField>();

        // Create text area
        CreateTextArea(inputContainer, placeholder, isPassword, inputFontSize);

        // Style the input field
        StyleInputField(accentColor, isPassword);

        // Add interaction effects
        AddInteractionEffects(inputContainer, accentColor);
    }
    
    void CreateLabel(string labelText, Color accentColor, float fontSize = 42f)
    {
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(transform);

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
    
    void CreateTextArea(GameObject parent, string placeholder, bool isPassword, float fontSize = 38f)
    {
        // Create text component
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent.transform);

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

        // Create placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(parent.transform);

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
        
        // Assign to input field
        inputField.textComponent = text;
        inputField.placeholder = placeholderText;
        
        if (isPassword)
        {
            inputField.contentType = TMP_InputField.ContentType.Password;
        }
    }
    
    void StyleInputField(Color accentColor, bool isPassword)
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

    void AddInteractionEffects(GameObject inputContainer, Color accentColor)
    {
        // Add padding to the input container so outline is visible on all sides
        RectTransform inputRect = inputContainer.GetComponent<RectTransform>();
        inputRect.offsetMin = new Vector2(5, 5);   // Left and bottom padding
        inputRect.offsetMax = new Vector2(-5, -5); // Right and top padding (negative values)

        // Add outline that glows on focus
        Outline outline = inputContainer.AddComponent<Outline>();
        outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.4f);
        outline.effectDistance = new Vector2(2, 2);

        // Add shadow for depth
        Shadow shadow = inputContainer.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(3, -3);
    }

    public string GetText()
    {
        return inputField != null ? inputField.text : "";
    }
    
    public void SetText(string text)
    {
        if (inputField != null)
        {
            inputField.text = text;
        }
    }

    public void OnValueChanged(UnityAction<string> callback)
    {
        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(callback);
        }
    }
}