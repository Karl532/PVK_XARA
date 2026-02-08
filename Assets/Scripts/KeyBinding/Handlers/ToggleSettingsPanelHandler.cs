using UnityEngine;

namespace KeyBinding.Handlers
{
    /// <summary>
    /// Toggles the settings panel visibility. Add to the UI GameObject; assign settingsPanel, xrCamera, offset.
    /// Invoked by KeyBindActions when the toggle keybind is pressed.
    /// </summary>
    public class ToggleSettingsPanelHandler : MonoBehaviour
    {
        [SerializeField] private CanvasGroup settingsPanel;
        [SerializeField] private Camera xrCamera;
        [SerializeField] private Vector3 offset = new Vector3(0, 0, 2);

        private UIFollowCamera _followCamera;

        void Awake()
        {
            if (settingsPanel == null)
                settingsPanel = GetComponent<CanvasGroup>();
        }

        public void Toggle()
        {
            if (settingsPanel == null) return;

            bool isOpen = settingsPanel.alpha > 0;

            if (isOpen)
            {
                settingsPanel.alpha = 0;
                settingsPanel.interactable = false;
                settingsPanel.blocksRaycasts = false;

                if (_followCamera != null)
                    _followCamera.enabled = false;
            }
            else
            {
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

                Settings settings = SettingsManager.Instance?.settings;
                bool shouldFollow = settings != null && settings.uiFollowCamera;

                if (shouldFollow && cam != null)
                {
                    if (_followCamera == null)
                        _followCamera = settingsPanel.gameObject.GetComponent<UIFollowCamera>();
                    if (_followCamera == null)
                        _followCamera = settingsPanel.gameObject.AddComponent<UIFollowCamera>();

                    _followCamera.xrCamera = cam.transform;
                    _followCamera.offset = offset;
                    _followCamera.enabled = true;
                }

                settingsPanel.alpha = 1;
                settingsPanel.interactable = true;
                settingsPanel.blocksRaycasts = true;
            }
        }
    }
}
