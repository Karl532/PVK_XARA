using UnityEngine;

namespace KeyBinding.Handlers
{
    /// <summary>
    /// Enters workspace placement mode when the keybind is pressed. Invoked by KeyBindActions.
    /// Workspace placement mode uses passthrough; user moves workspace with thumbsticks, places and exits with B.
    /// </summary>
    public class ToggleBlockPlacementHandler : MonoBehaviour
    {
        public void Toggle()
        {
            var controller = Object.FindFirstObjectByType<WorkspacePlacementController>();
            if (controller != null)
            {
                if (controller.IsActive)
                    controller.ExitPlacementMode();
                else
                    controller.EnterPlacementMode();
            }
            else
            {
                Debug.LogWarning("[KeyBind] WorkspacePlacementController not found. Add it to the scene (e.g. on UIManager).");
            }
        }
    }
}
