using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class UICheckbox : MonoBehaviour
{
    private Toggle toggle;
    private Image checkboxBackground;
    private TextMeshProUGUI checkmarkText;
    private Color accentColorCached;

    private UnityAction<bool> onValueChanged; // 👈 stored callback

    // "Constructor-style" init
    public void CreateCheckbox(
        string label,
        Color accentColor,
        bool defaultValue,
        UnityAction<bool> callback,
        float fontSize = 42f,
        float checkboxSize = 60f
    )
    {
        accentColorCached = accentColor;
        onValueChanged = callback;

        RectTransform containerRect = GetComponent<RectTransform>();
        if (containerRect == null)
        {
            containerRect = gameObject.AddComponent<RectTransform>();
        }

        GameObject checkboxBox = CreateCheckboxBox(accentColor, checkboxSize);
        CreateCheckboxLabel(label, fontSize, checkboxSize);
        SetupToggle(defaultValue);
    }

    GameObject CreateCheckboxBox(Color accentColor, float checkboxSize)
    {
        GameObject boxObj = new GameObject("CheckboxBox");
        boxObj.transform.SetParent(transform, false);

        RectTransform boxRect = boxObj.AddComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0, 0.5f);
        boxRect.anchorMax = new Vector2(0, 0.5f);
        boxRect.pivot = new Vector2(0, 0.5f);
        boxRect.anchoredPosition = new Vector2(20, 0);
        boxRect.sizeDelta = new Vector2(checkboxSize, checkboxSize);

        checkboxBackground = boxObj.AddComponent<Image>();
        checkboxBackground.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

        RoundedImage rounded = boxObj.AddComponent<RoundedImage>();
        rounded.SetRadius(8f);

        Outline outline = boxObj.AddComponent<Outline>();
        outline.effectColor = new Color(accentColorCached.r, accentColorCached.g, accentColorCached.b, 0.4f);
        outline.effectDistance = new Vector2(2, 2);

        GameObject checkmarkObj = new GameObject("Checkmark");
        checkmarkObj.transform.SetParent(boxObj.transform, false);

        RectTransform checkRect = checkmarkObj.AddComponent<RectTransform>();
        checkRect.anchorMin = Vector2.zero;
        checkRect.anchorMax = Vector2.one;

        checkmarkText = checkmarkObj.AddComponent<TextMeshProUGUI>();
        checkmarkText.text = "\u2713";
        checkmarkText.fontSize = checkboxSize * 0.65f;
        checkmarkText.color = accentColorCached;
        checkmarkText.alignment = TextAlignmentOptions.Center;
        checkmarkText.fontStyle = FontStyles.Bold;

        return boxObj;
    }

    void CreateCheckboxLabel(string labelText, float fontSize, float checkboxSize)
    {
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(transform, false);

        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.offsetMin = new Vector2(checkboxSize + 40, 0);
        labelRect.offsetMax = new Vector2(-20, 0);

        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
    }

    void SetupToggle(bool defaultValue)
    {
        toggle = gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = checkboxBackground;
        toggle.isOn = defaultValue;

        toggle.onValueChanged.AddListener(OnToggleChanged);

        UpdateVisuals(defaultValue);
    }

    void OnToggleChanged(bool isOn)
    {
        UpdateVisuals(isOn);
        onValueChanged?.Invoke(isOn); // 👈 always fires
    }

    void UpdateVisuals(bool isOn)
    {
        checkmarkText.enabled = isOn;

        checkboxBackground.color = isOn
            ? new Color(accentColorCached.r * 0.3f,
                        accentColorCached.g * 0.3f,
                        accentColorCached.b * 0.3f, 0.95f)
            : new Color(0.12f, 0.12f, 0.18f, 0.95f);
    }
}
