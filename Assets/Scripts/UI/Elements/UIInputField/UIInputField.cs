using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;
using UnityEngine.XR.OpenXR.NativeTypes;

namespace UI.Elements.UIInputField
{
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
        /// </summary>
        public void CreateInputField(
            string label,
            string placeholder,
            Color accentColor,
            bool isPassword = false,
            float labelFontSize = UIInputFieldStyling.DefaultLabelFontSize,
            float inputFontSize = UIInputFieldStyling.DefaultInputFontSize)
        {
            RectTransform containerRect = GetComponent<RectTransform>();
            if (containerRect == null)
                gameObject.AddComponent<RectTransform>();

            UIInputFieldStyling.CreateLabel(transform, label, accentColor, labelFontSize);

            GameObject inputContainer = CreateInputContainer(accentColor, placeholder, isPassword, inputFontSize);

            UIInputFieldStyling.StyleInputField(_inputField, accentColor);
            UIInputFieldStyling.AddInteractionEffects(inputContainer, accentColor);

            XRKeyboard keyboard = UIInputFieldKeyboard.FindKeyboardInScene();
            if (keyboard == null)
                Debug.LogWarning("No XRKeyboard found in scene. Input field will work but Spatial Keyboard may not open.", this);

            UIInputFieldKeyboard.AttachKeyboard(inputContainer, _inputField, keyboard);
        }

        private GameObject CreateInputContainer(Color accentColor, string placeholder, bool isPassword, float inputFontSize)
        {
            GameObject inputContainer = new GameObject("InputContainer");
            inputContainer.transform.SetParent(transform, false);

            RectTransform inputRect = inputContainer.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0, 0);
            inputRect.anchorMax = new Vector2(1, 0.65f);
            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = Vector2.zero;
            inputRect.localScale = Vector3.one;

            _backgroundImage = inputContainer.AddComponent<Image>();
            _backgroundImage.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

            _inputField = inputContainer.AddComponent<TMP_InputField>();
            UIInputFieldStyling.CreateTextArea(inputContainer, placeholder, isPassword, inputFontSize, _inputField);

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
