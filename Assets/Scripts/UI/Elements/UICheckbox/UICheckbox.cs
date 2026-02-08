using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace UI.Elements.UICheckbox
{
    /// <summary>
    /// A checkbox UI element with configurable label, accent color, and value callback.
    /// Styling is delegated to <see cref="UICheckboxStyling"/>.
    /// </summary>
    public class UICheckbox : MonoBehaviour
{
    private Toggle _toggle;
    private Image _checkboxBackground;
    private TextMeshProUGUI _checkmarkText;
    private Color _accentColorCached;
    private UnityAction<bool> _onValueChanged;

    /// <summary>
    /// Initializes the checkbox with label, appearance, default value, and callback.
    /// </summary>
    public void CreateCheckbox(
        string label,
        Color accentColor,
        bool defaultValue,
        UnityAction<bool> callback,
        float fontSize = UICheckboxStyling.DefaultFontSize,
        float checkboxSize = UICheckboxStyling.DefaultCheckboxSize)
    {
        _accentColorCached = accentColor;
        _onValueChanged = callback;

        RectTransform containerRect = GetComponent<RectTransform>();
        if (containerRect == null)
            containerRect = gameObject.AddComponent<RectTransform>();

        UICheckboxStyling.CreateCheckboxBox(transform, accentColor, checkboxSize, out _checkboxBackground, out _checkmarkText);
        UICheckboxStyling.CreateCheckboxLabel(transform, label, fontSize, checkboxSize);
        SetupToggle(defaultValue);
    }

    private void SetupToggle(bool defaultValue)
    {
        _toggle = gameObject.AddComponent<Toggle>();
        _toggle.targetGraphic = _checkboxBackground;
        _toggle.isOn = defaultValue;
        _toggle.onValueChanged.AddListener(OnToggleChanged);

        UICheckboxStyling.UpdateVisuals(defaultValue, _accentColorCached, _checkboxBackground, _checkmarkText);
    }

    private void OnToggleChanged(bool isOn)
    {
        UICheckboxStyling.UpdateVisuals(isOn, _accentColorCached, _checkboxBackground, _checkmarkText);
        _onValueChanged?.Invoke(isOn);
    }
}
}
