using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Binds a keyboard key to an action. Invokes a UnityEvent when the key is pressed, released, or held.
/// Uses Unity's Input System. Only active when a keyboard is available (e.g. null on Quest with no keyboard).
/// </summary>
public class KeyBinding : MonoBehaviour
{
    public enum TriggerMode
    {
        OnPress,
        OnRelease,
        OnHold
    }

    [SerializeField] private Key key = Key.Y;
    [SerializeField] private TriggerMode triggerMode = TriggerMode.OnPress;
    [SerializeField] private bool requireCtrl;
    [SerializeField] private bool requireAlt;
    [SerializeField] private bool requireShift;

    [SerializeField] private UnityEvent onKeyTriggered = new UnityEvent();

    /// <summary>
    /// The key to bind. Default is Y.
    /// </summary>
    public Key Key
    {
        get => key;
        set => key = value;
    }

    /// <summary>
    /// When to trigger: OnPress (down), OnRelease (up), or OnHold (every frame while held).
    /// </summary>
    public TriggerMode Mode
    {
        get => triggerMode;
        set => triggerMode = value;
    }

    /// <summary>
    /// Event invoked when the key condition is met.
    /// </summary>
    public UnityEvent OnKeyTriggered => onKeyTriggered;

    void Update()
    {
        if (!isActiveAndEnabled) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (!ModifiersSatisfied(keyboard)) return;

        bool triggered = triggerMode switch
        {
            TriggerMode.OnPress => keyboard[key].wasPressedThisFrame,
            TriggerMode.OnRelease => keyboard[key].wasReleasedThisFrame,
            TriggerMode.OnHold => keyboard[key].isPressed,
            _ => false
        };

        if (triggered)
            onKeyTriggered?.Invoke();
    }

    bool ModifiersSatisfied(Keyboard keyboard)
    {
        if (requireCtrl && !keyboard.ctrlKey.isPressed) return false;
        if (requireAlt && !keyboard.altKey.isPressed) return false;
        if (requireShift && !keyboard.shiftKey.isPressed) return false;
        return true;
    }

    /// <summary>
    /// Bind a key and action at runtime.
    /// </summary>
    public void SetBinding(Key newKey, UnityAction action, TriggerMode mode = TriggerMode.OnPress)
    {
        key = newKey;
        triggerMode = mode;
        onKeyTriggered.RemoveAllListeners();
        if (action != null)
            onKeyTriggered.AddListener(action);
    }
}
