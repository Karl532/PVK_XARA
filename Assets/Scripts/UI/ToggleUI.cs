using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleUI : MonoBehaviour
{
    public CanvasGroup settingsCanvas;

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Three) ||
            Keyboard.current.yKey.wasPressedThisFrame)
        {
            bool isOpen = settingsCanvas.alpha > 0;

            settingsCanvas.alpha = isOpen ? 0 : 1;
            settingsCanvas.interactable = !isOpen;
            settingsCanvas.blocksRaycasts = !isOpen;
        }
    }
}
