using UnityEngine;

namespace KeyBinding.Handlers
{
    /// <summary>
    /// Toggles block placement on/off via settings. Invoked by KeyBindActions when the keybind is pressed.
    /// </summary>
    public class ToggleBlockPlacementHandler : MonoBehaviour
    {
        public void Toggle()
        {
            Settings settings = SettingsManager.Instance?.settings;
            if (settings != null)
            {
                settings.blockPlacementEnabled = !settings.blockPlacementEnabled;
                Debug.Log($"[KeyBind] Block placement: {(settings.blockPlacementEnabled ? "ON" : "OFF")}");
            }
        }
    }
}
