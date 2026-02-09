using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;
using UI.Utils;

namespace UI.Elements.UIDropdown
{
    /// <summary>
    /// A dropdown UI element with label, options, and value change callback.
    /// Styling is delegated to <see cref="UIDropdownStyling"/>.
    /// </summary>
    public class UIDropdown : MonoBehaviour
    {
        private TMP_Dropdown _dropdown;

        /// <summary>
        /// Creates the dropdown with label, options, accent color, and optional value change callback.
        /// </summary>
        /// <param name="onValueChanged">Invoked when the selected option changes. Passes the new option index.</param>
        public void CreateDropdown(
            string label,
            List<string> options,
            Color accentColor,
            UnityAction<int> onValueChanged = null,
            float labelFontSize = UIDropdownStyling.DefaultLabelFontSize,
            float itemFontSize = UIDropdownStyling.DefaultItemFontSize)
        {
            UIComponentHelper.GetOrAddComponent<RectTransform>(gameObject);

            UIDropdownStyling.CreateLabel(transform, label, accentColor, labelFontSize);

            GameObject dropdownContainer = UIDropdownStyling.CreateDropdownElement(transform, options, accentColor, itemFontSize, out _dropdown);

            UIDropdownStyling.StyleDropdown(_dropdown);
            UIDropdownStyling.AddInteractionEffects(dropdownContainer, accentColor);

            if (onValueChanged != null)
                _dropdown.onValueChanged.AddListener(onValueChanged);
        }

        public int GetValue() => _dropdown != null ? _dropdown.value : 0;

        public void SetValue(int index)
        {
            if (_dropdown != null)
                _dropdown.value = index;
        }

        public string GetSelectedText()
        {
            if (_dropdown != null && _dropdown.options.Count > _dropdown.value)
                return _dropdown.options[_dropdown.value].text;
            return "";
        }

        public void OnValueChanged(UnityAction<int> callback)
        {
            if (_dropdown != null)
                _dropdown.onValueChanged.AddListener(callback);
        }
    }
}
