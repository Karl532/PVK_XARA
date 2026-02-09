using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

namespace UI.Elements.UIInputField
{
    /// <summary>
    /// Switches the XR Spatial Keyboard to a subset layout when this input field opens the keyboard
    /// (e.g. numeric layout for number fields). Used automatically when you use InputType.DecimalNumber etc.
    /// The keyboard comes from GlobalNonNativeKeyboard (XRI Global Keyboard Manager); no XR Keyboard in scene needed.
    ///
    /// OPTIONAL – numbers-only keyboard: The manager spawns the keyboard from its "Keyboard Prefab". To show a numeric layout:
    /// 1. In your scene, select the XRI Global Keyboard Manager. Note which prefab is in "Keyboard Prefab".
    /// 2. Open that prefab (e.g. XRI Keyboard from the XRI Samples).
    /// 3. On the XRKeyboard component, add a "Subset Layout" entry. Layout String:  123
    /// 4. Set Layout Root to the same layout root the main keys use. Create a numeric XRKeyboardConfig (Create → XR → Spatial Keyboard → Keyboard Config)
    ///    with only 0-9, ., backspace etc. Assign it to Toggle On Config; set Toggle Off Config to your default layout.
    /// If you skip this, input is still restricted to numbers; the keyboard just won't switch to a numeric layout.
    /// </summary>
    public class UIInputFieldKeyboardLayoutSwitcher : MonoBehaviour
    {
        /// <summary>
        /// Layout key to apply when the keyboard opens. Must match a Subset Mapping layout string on the XRKeyboard.
        /// E.g. "123" for numeric, "\\sym" for symbols. Empty/null = no switch.
        /// </summary>
        public static string GetLayoutKeyForInputType(InputType inputType)
        {
            switch (inputType)
            {
                case InputType.IntegerNumber:
                case InputType.DecimalNumber:
                case InputType.Pin:
                    return "123";
                default:
                    return null;
            }
        }

        string _layoutKey;
        UnityAction _onOpenedHandler;

        /// <summary>
        /// Initializes the switcher. Call after adding the component.
        /// </summary>
        public void Initialize(InputType inputType)
        {
            _layoutKey = GetLayoutKeyForInputType(inputType);
        }

        void Awake()
        {
            var display = GetComponent<XRKeyboardDisplay>();
            if (display == null) return;

            _onOpenedHandler = OnKeyboardOpened;
            display.onKeyboardOpened.AddListener(_onOpenedHandler);
        }

        void OnDestroy()
        {
            var display = GetComponent<XRKeyboardDisplay>();
            if (display != null && _onOpenedHandler != null)
                display.onKeyboardOpened.RemoveListener(_onOpenedHandler);
        }

        void OnKeyboardOpened()
        {
            if (string.IsNullOrEmpty(_layoutKey)) return;
            StartCoroutine(SwitchLayoutNextFrame());
        }

        IEnumerator SwitchLayoutNextFrame()
        {
            yield return null;
            if (GlobalNonNativeKeyboard.instance == null) yield break;
            var keyboard = GlobalNonNativeKeyboard.instance.keyboard;
            if (keyboard != null && keyboard.isOpen)
                keyboard.UpdateLayout(_layoutKey);
        }
    }
}
