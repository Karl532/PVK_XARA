using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

namespace KeyBinding
{
    /// <summary>
    /// Listens for XR controller input (OVRInput) and invokes the bound action when the button is pressed.
    /// Lives on KeyBindRegistry's root; not tied to UI visibility.
    /// </summary>
    public class KeyBindInput : MonoBehaviour
    {
        public enum TriggerMode
        {
            OnPress,
            OnRelease,
            OnHold
        }

        [SerializeField] private OVRInput.Button button = OVRInput.Button.One;
        [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.Touch;
        [SerializeField] private TriggerMode triggerMode = TriggerMode.OnPress;

        [SerializeField] private UnityEvent onButtonTriggered = new UnityEvent();

        private bool _wasPressed;

        public OVRInput.Button Button { get => button; set => button = value; }
        public OVRInput.Controller Controller { get => controller; set => controller = value; }
        public TriggerMode Mode { get => triggerMode; set => triggerMode = value; }
        public UnityEvent OnButtonTriggered => onButtonTriggered;

        void Update()
        {
            if (!isActiveAndEnabled) return;

            bool triggered;
            switch (triggerMode)
            {
                case TriggerMode.OnPress:
                    triggered = GetButtonDown();
                    break;
                case TriggerMode.OnRelease:
                    triggered = GetButtonUp();
                    break;
                case TriggerMode.OnHold:
                    triggered = OVRInput.Get(button, controller);
                    break;
                default:
                    triggered = false;
                    break;
            }

            if (triggered && !IsKeyboardOrInputFocused())
                onButtonTriggered?.Invoke();
        }

        bool GetButtonDown()
        {
            bool pressed = OVRInput.Get(button, controller);
            bool down = pressed && !_wasPressed;
            _wasPressed = pressed;
            return down;
        }

        bool GetButtonUp()
        {
            bool pressed = OVRInput.Get(button, controller);
            bool up = !pressed && _wasPressed;
            _wasPressed = pressed;
            return up;
        }

        public void SetBinding(OVRInput.Button newButton, UnityAction action, TriggerMode mode = TriggerMode.OnPress)
        {
            button = newButton;
            triggerMode = mode;
            _wasPressed = true;
            onButtonTriggered.RemoveAllListeners();
            if (action != null)
                onButtonTriggered.AddListener(action);
        }

        /// <summary>
        /// Returns true when the spatial keyboard is open or an input field is focused.
        /// Keybinds are suppressed in that case so typing works without triggering actions.
        /// </summary>
        static bool IsKeyboardOrInputFocused()
        {
            if (GlobalNonNativeKeyboard.instance == null) return false;
            var keyboard = GlobalNonNativeKeyboard.instance.keyboard;
            return keyboard != null && keyboard.isOpen;
        }
    }
}
