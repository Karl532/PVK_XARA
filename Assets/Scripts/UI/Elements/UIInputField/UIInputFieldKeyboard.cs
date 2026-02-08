using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

namespace UI.Elements.UIInputField
{
    /// <summary>
    /// Handles attaching the Spatial Keyboard to input fields.
    /// </summary>
    public class UIInputFieldKeyboard
    {
        /// <summary>
        /// Finds an XRKeyboard in the scene. Returns null if none found (display will use global keyboard).
        /// </summary>
        public static XRKeyboard FindKeyboardInScene()
        {
            return Object.FindFirstObjectByType<XRKeyboard>();
        }

        /// <summary>
        /// Attaches XRKeyboardDisplay to the input container and links it to the given keyboard.
        /// </summary>
        public static void AttachKeyboard(GameObject inputContainer, TMP_InputField inputField, XRKeyboard keyboard)
        {
            if (inputField == null)
            {
                Debug.LogError("UIInputFieldKeyboard: inputField is null.", inputContainer);
                return;
            }

            XRKeyboardDisplay display = inputContainer.AddComponent<XRKeyboardDisplay>();
            display.enabled = false;
            display.inputField = inputField;
            display.keyboard = keyboard;
            display.enabled = true;
        }
    }
}
