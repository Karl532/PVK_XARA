using UnityEngine;
using UnityEngine.InputSystem;
public class ToggleUI : MonoBehaviour
{
    public GameObject settingsCanvas;

    void Update()
    {

        if (OVRInput.GetDown(OVRInput.Button.Three) || Keyboard.current.yKey.wasPressedThisFrame) // Y button
        {
            settingsCanvas.SetActive(!settingsCanvas.activeSelf);
        }
    }
}