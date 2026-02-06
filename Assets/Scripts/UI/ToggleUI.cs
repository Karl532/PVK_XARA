using UnityEngine;

public class SettingsOverlayToggle : MonoBehaviour
{
    public GameObject settingsCanvas;

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Three)) // Y button
        {
            settingsCanvas.SetActive(!settingsCanvas.activeSelf);
        }
    }
}