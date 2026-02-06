using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleUI : MonoBehaviour
{
    public CanvasGroup settingsPanel;

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Three) ||
            Keyboard.current.yKey.wasPressedThisFrame)
        {
            bool isOpen = settingsPanel.alpha > 0;

            settingsPanel.alpha = isOpen ? 0 : 1;
            settingsPanel.interactable = !isOpen;
            settingsPanel.blocksRaycasts = !isOpen;
        }
    }
}