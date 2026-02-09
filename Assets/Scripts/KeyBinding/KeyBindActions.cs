using UnityEngine;
using KeyBinding.Handlers;

namespace KeyBinding
{
    /// <summary>
    /// Add to a GameObject (e.g. same as UIManager). Implements the methods called when keybinds are pressed.
    /// Assign in UIManager's Key Bind Actions field.
    /// </summary>
    public class KeyBindActions : MonoBehaviour
    {
        public void ToggleBlockPlacement()
        {
            var handler = Object.FindFirstObjectByType<ToggleBlockPlacementHandler>();
            handler?.Toggle();
        }

        public void ToggleUI()
        {
            var handler = Object.FindFirstObjectByType<ToggleSettingsPanelHandler>();
            handler?.Toggle();
        }

        // Add more methods as needed, e.g.:
        // public void Save() { ... }
        // public void Undo() { ... }
    }
}
