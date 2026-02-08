using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using UI.Utils;

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
        private Color? _boxBgOff;
        private Color? _boxBgOn;
        private UnityAction<bool> _onValueChanged;

        /// <summary>
        /// Initializes the checkbox with label, appearance, default value, and callback.
        /// </summary>
        public void CreateCheckbox(
            string label,
            Color accentColor,
            bool defaultValue,
            UnityAction<bool> callback,
            Color? labelColor = null,
            float fontSize = UICheckboxStyling.DefaultFontSize,
            float checkboxSize = UICheckboxStyling.DefaultCheckboxSize,
            Color? boxBackgroundOff = null,
            Color? boxBackgroundOn = null)
        {
            _accentColorCached = accentColor;
            _boxBgOff = boxBackgroundOff;
            _boxBgOn = boxBackgroundOn;
            _onValueChanged = callback;

            RectTransform containerRect = UIComponentHelper.GetOrAddComponent<RectTransform>(gameObject);

            UICheckboxStyling.CreateCheckboxBox(transform, accentColor, checkboxSize, boxBackgroundOff, boxBackgroundOn, out _checkboxBackground, out _checkmarkText);
            UICheckboxStyling.CreateCheckboxLabel(transform, label, fontSize, checkboxSize, labelColor ?? Color.white);
            SetupToggle(defaultValue);
        }

        private void SetupToggle(bool defaultValue)
        {
            _toggle = gameObject.AddComponent<Toggle>();
            _toggle.targetGraphic = _checkboxBackground;
            _toggle.isOn = defaultValue;
            _toggle.onValueChanged.AddListener(OnToggleChanged);

            UICheckboxStyling.UpdateVisuals(defaultValue, _accentColorCached, _checkboxBackground, _checkmarkText, _boxBgOff, _boxBgOn);
        }

        private void OnToggleChanged(bool isOn)
        {
            UICheckboxStyling.UpdateVisuals(isOn, _accentColorCached, _checkboxBackground, _checkmarkText, _boxBgOff, _boxBgOn);
            _onValueChanged?.Invoke(isOn);
        }
    }
}
