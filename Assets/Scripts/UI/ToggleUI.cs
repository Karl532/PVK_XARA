using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleUI : MonoBehaviour
{
    public CanvasGroup settingsPanel;
    public Camera xrCamera;
    public Vector3 offset = new Vector3(0, 0, 2);

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Three) ||
            Keyboard.current.yKey.wasPressedThisFrame)
        {
            bool isOpen = settingsPanel.alpha > 0;

            if (isOpen)
            {
                settingsPanel.alpha = 0;
                settingsPanel.interactable = false;
                settingsPanel.blocksRaycasts = false;
            }
            else
            {
                // Position once in front of the camera (not the rig)
                Camera cam = xrCamera != null ? xrCamera : Camera.main;
                if (cam != null)
                {
                    Transform camTransform = cam.transform;
                    Transform uiTransform = settingsPanel.transform;
                    uiTransform.position = camTransform.position
                        + camTransform.forward * offset.z
                        + camTransform.up * offset.y
                        + camTransform.right * offset.x;
                    uiTransform.rotation = Quaternion.LookRotation(
                        uiTransform.position - camTransform.position);
                }

                settingsPanel.alpha = 1;
                settingsPanel.interactable = true;
                settingsPanel.blocksRaycasts = true;
            }
        }
    }
}
