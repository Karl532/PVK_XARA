using UnityEngine;

namespace KeyBinding.Handlers
{
    /// <summary>
    /// Enters block placement mode when the keybind is pressed. Invoked by KeyBindActions.
    /// Block placement mode uses passthrough; user moves block with thumbsticks, places and exits with B.
    /// </summary>
    public class ToggleBlockPlacementHandler : MonoBehaviour
    {
        public void Toggle()
        {
            var controller = Object.FindFirstObjectByType<BlockPlacementController>();
            if (controller != null)
            {
                if (controller.IsActive)
                    controller.ExitPlacementMode();
                else
                    controller.EnterPlacementMode();
            }
            else
            {
                Debug.LogWarning("[KeyBind] BlockPlacementController not found. Add it to the scene (e.g. on UIManager).");
            }
        }
    }
}
