using UnityEngine;
using UnityEngine.Events;

namespace KeyBinding
{
    /// <summary>
    /// Maps XR controller buttons to actions. Listens for input (OVRInput on Quest) and invokes the bound action.
    /// Lives on KeyBindingHolder; not tied to UI visibility.
    /// </summary>
    public class KeyBindingHandler : MonoBehaviour
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

            if (triggered)
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
            _wasPressed = false;
            onButtonTriggered.RemoveAllListeners();
            if (action != null)
                onButtonTriggered.AddListener(action);
        }
    }
}
