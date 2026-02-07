using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class UIButton : MonoBehaviour
{
    private Button button;
    private Image backgroundImage;
    private TextMeshProUGUI buttonText;
    private UIButtonAnimator animator;

    public void CreateButton(string text, UnityAction onClick, Color buttonColor, Color textColor, Vector2 size)
    {
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        if (rect == null)
            rect = gameObject.AddComponent<RectTransform>();
        rect.sizeDelta = size;

        backgroundImage = gameObject.GetComponent<Image>();
        if (backgroundImage == null)
            backgroundImage = gameObject.AddComponent<Image>();
        backgroundImage.color = buttonColor;

        button = gameObject.GetComponent<Button>();
        if (button == null)
            button = gameObject.AddComponent<Button>();
        button.targetGraphic = backgroundImage;
        button.onClick.AddListener(onClick);

        SetupButtonColors(buttonColor);

        GameObject textGO = new GameObject("ButtonText");
        textGO.transform.SetParent(transform, false);

        buttonText = textGO.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.color = textColor;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontSize = 60;
        buttonText.enableAutoSizing = true;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Add optional animator
        animator = gameObject.GetComponent<UIButtonAnimator>();
        if (animator == null)
            animator = gameObject.AddComponent<UIButtonAnimator>();
        animator.Initialize(buttonColor);
    }
    void SetupButtonColors(Color buttonColor)
    {
        ColorBlock colors = button.colors;
        
        // Calculate color variations
        Color highlightColor = new Color(
            Mathf.Min(buttonColor.r * 1.2f, 1f),
            Mathf.Min(buttonColor.g * 1.2f, 1f),
            Mathf.Min(buttonColor.b * 1.2f, 1f),
            buttonColor.a
        );
        
        Color pressedColor = new Color(
            buttonColor.r * 0.8f,
            buttonColor.g * 0.8f,
            buttonColor.b * 0.8f,
            buttonColor.a
        );
        
        colors.normalColor = buttonColor;
        colors.highlightedColor = highlightColor;
        colors.pressedColor = pressedColor;
        colors.selectedColor = highlightColor;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.15f;
        
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;
    }
    
    void CreateButtonText(string text, Color textColor)
    {
        GameObject textObj = new GameObject("ButtonText");
        textObj.transform.SetParent(transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.localPosition = Vector3.zero;
        textRect.localScale = Vector3.one;
        
        buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 42;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.color = textColor;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.enableWordWrapping = false;
        
        // Add text shadow for depth
        Shadow textShadow = textObj.AddComponent<Shadow>();
        textShadow.effectColor = new Color(0, 0, 0, 0.5f);
        textShadow.effectDistance = new Vector2(2, -2);
    }
    
    void AddVisualEffects(Color buttonColor)
    {
        // Add outline
        Outline outline = gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.3f);
        outline.effectDistance = new Vector2(0, 2);
        
        // Add shadow for depth
        Shadow shadow = gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.6f);
        shadow.effectDistance = new Vector2(4, -4);
        
        // Add subtle inner glow
        Shadow innerGlow = gameObject.AddComponent<Shadow>();
        Color glowColor = new Color(
            Mathf.Min(buttonColor.r * 1.5f, 1f),
            Mathf.Min(buttonColor.g * 1.5f, 1f),
            Mathf.Min(buttonColor.b * 1.5f, 1f),
            0.3f
        );
        innerGlow.effectColor = glowColor;
        innerGlow.effectDistance = new Vector2(0, 0);
    }
    
    public void SetInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }
    
    public void SetText(string text)
    {
        if (buttonText != null)
        {
            buttonText.text = text;
        }
    }
}
