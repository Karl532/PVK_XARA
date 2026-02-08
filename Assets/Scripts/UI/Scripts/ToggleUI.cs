using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleUI : MonoBehaviour
{
    public CanvasGroup settingsPanel;
    public Camera xrCamera;
    public Vector3 offset = new Vector3(0, 0, 2);

    private UIFollowCamera followCamera;

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Three) ||
            Keyboard.current.yKey.wasPressedThisFrame)
        {
            bool isOpen = settingsPanel.alpha > 0;

            if (isOpen)
            {
                // Hide and disable follow
                settingsPanel.alpha = 0;
                settingsPanel.interactable = false;
                settingsPanel.blocksRaycasts = false;

                if (followCamera != null)
                    followCamera.enabled = false;
            }
            else
            {
                Camera cam = xrCamera != null ? xrCamera : Camera.main;

                // Always position in front of camera first
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

                // Check if follow camera mode is enabled
                Settings settings = SettingsManager.Instance != null ? SettingsManager.Instance.settings : null;
                bool shouldFollow = settings != null && settings.uiFollowCamera;

                if (shouldFollow && cam != null)
                {
                    if (followCamera == null)
                        followCamera = settingsPanel.gameObject.GetComponent<UIFollowCamera>();
                    if (followCamera == null)
                        followCamera = settingsPanel.gameObject.AddComponent<UIFollowCamera>();

                    followCamera.xrCamera = cam.transform;
                    followCamera.offset = offset;
                    followCamera.enabled = true;
                }

                settingsPanel.alpha = 1;
                settingsPanel.interactable = true;
                settingsPanel.blocksRaycasts = true;
            }
        }
    }
}
