using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;
using UI.Utils;

namespace UI.Elements.UIInputField
{
    /// <summary>
    /// Restricts what the user can type. Maps to TMP_InputField.ContentType so the keyboard/field only accepts valid characters.
    /// </summary>
    public enum InputType
    {
        Standard,
        IntegerNumber,
        DecimalNumber,
        Alphanumeric,
        Name,
        EmailAddress,
        Pin
    }

    /// <summary>
    /// A text input field with label, placeholder, styling, and Spatial Keyboard support.
    /// Styling is in <see cref="UIInputFieldStyling"/>, keyboard wiring in <see cref="UIInputFieldKeyboard"/>.
    /// </summary>
    public class UIInputField : MonoBehaviour
    {
        private TMP_InputField _inputField;
        private Image _backgroundImage;

        /// <summary>
        /// Creates a TMP_InputField with label, placeholder, styling, and Spatial Keyboard.
        /// InputType restricts which characters can be entered (enforced by TMP_InputField.ContentType).
        /// </summary>
        /// <param name="onValueChanged">Optional. Invoked whenever the text value changes.</param>
        public void CreateInputField(
            string label,
            string placeholder,
            Color accentColor,
            InputType inputType = InputType.Standard,
            UnityAction<string> onValueChanged = null,
            float labelFontSize = UIInputFieldStyling.DefaultLabelFontSize,
            float inputFontSize = UIInputFieldStyling.DefaultInputFontSize)
        {
            UIComponentHelper.GetOrAddComponent<RectTransform>(gameObject);

            UIInputFieldStyling.CreateLabel(transform, label, accentColor, labelFontSize);

            GameObject inputContainer = CreateInputContainer(accentColor, placeholder, inputType, inputFontSize);

            UIInputFieldStyling.StyleInputField(_inputField, accentColor);
            UIInputFieldStyling.AddInteractionEffects(inputContainer, accentColor);

            // Sanitize on every value change so bulk updates (e.g. from Spatial Keyboard) only show allowed characters
            if (inputType != InputType.Standard)
            {
                _inputField.onValueChanged.AddListener(SanitizeValue);
                void SanitizeValue(string value)
                {
                    string sanitized = UIInputFieldStyling.SanitizeForInputType(value, inputType);
                    if (sanitized != value)
                        _inputField.SetTextWithoutNotify(sanitized);
                }
            }

            if (onValueChanged != null)
                _inputField.onValueChanged.AddListener(onValueChanged);

            XRKeyboard keyboard = UIInputFieldKeyboard.FindKeyboardInScene();
            if (keyboard == null)
                Debug.LogWarning("No XRKeyboard found in scene. Input field will work but Spatial Keyboard may not open.", this);

            UIInputFieldKeyboard.AttachKeyboard(inputContainer, _inputField, keyboard);
        }

        private GameObject CreateInputContainer(Color accentColor, string placeholder, InputType inputType, float inputFontSize)
        {
            GameObject inputContainer = UIPrimitives.CreateUIElement("InputContainer", transform,
                new Vector2(0, 0), new Vector2(1, 0.65f));

            _backgroundImage = inputContainer.AddComponent<Image>();
            _backgroundImage.color = UIPrimitives.Colors.DarkBackground;

            _inputField = inputContainer.AddComponent<TMP_InputField>();
            UIInputFieldStyling.CreateTextArea(inputContainer, placeholder, inputType, inputFontSize, _inputField);

            return inputContainer;
        }

        public string GetText() => _inputField != null ? _inputField.text : "";

        public void SetText(string text)
        {
            if (_inputField != null)
                _inputField.text = text;
        }

        public void OnValueChanged(UnityAction<string> callback)
        {
            if (_inputField != null)
                _inputField.onValueChanged.AddListener(callback);
        }
    }
}
