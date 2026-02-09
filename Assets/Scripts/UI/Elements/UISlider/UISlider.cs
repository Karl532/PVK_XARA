using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using UI.Utils;

namespace UI.Elements.UISlider
{
    /// <summary>
    /// A horizontal slider UI element with configurable label, range, and value callback.
    /// </summary>
    public class UISlider : MonoBehaviour
    {
        private Slider _slider;
        private TextMeshProUGUI _valueText;
        private UnityAction<float> _onValueChanged;

        /// <summary>
        /// Initializes the slider with label, range, default value, and callback.
        /// </summary>
        public void Create(
            string label,
            float minValue,
            float maxValue,
            float defaultValue,
            UnityAction<float> onValueChanged,
            Color accentColor,
            Color? textColor = null,
            float height = 120f,
            bool showValue = true,
            float tickStep = 0f)
        {
            _onValueChanged = onValueChanged;
            Color labelColor = textColor ?? Color.white;

            RectTransform containerRect = UIComponentHelper.GetOrAddComponent<RectTransform>(gameObject);

            UISliderStyling.CreateLabel(transform, label, accentColor, labelColor);
            UISliderStyling.CreateSliderBar(transform, minValue, maxValue, defaultValue, accentColor, out _slider, out _valueText, showValue, tickStep);

            _slider.onValueChanged.AddListener(OnSliderChanged);
            if (showValue && _valueText != null)
                _valueText.text = FormatValue(_slider.value);
        }

        void OnSliderChanged(float value)
        {
            if (_valueText != null)
                _valueText.text = FormatValue(value);
            _onValueChanged?.Invoke(value);
        }

        static string FormatValue(float value)
        {
            return value < 10f ? value.ToString("F1") : Mathf.RoundToInt(value).ToString();
        }

        public void SetValue(float value)
        {
            if (_slider != null)
            {
                _slider.SetValueWithoutNotify(value);
                if (_valueText != null)
                    _valueText.text = FormatValue(value);
            }
        }
    }
}
