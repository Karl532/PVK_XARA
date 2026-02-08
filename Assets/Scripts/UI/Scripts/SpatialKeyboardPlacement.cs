using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

public class SpatialKeyboardPlacement : MonoBehaviour
{
    [Header("Position (camera-relative)")]
    [Tooltip("Distance in front of the camera (forward axis).")]
    [SerializeField] float _distanceForward = 1.2f;

    [Tooltip("Height offset from camera (up axis).")]
    [SerializeField] float _heightOffset = 0f;

    [Tooltip("Horizontal offset (right axis).")]
    [SerializeField] float _horizontalOffset = 0f;

    [Header("Scale")]
    [Tooltip("Scale of the keyboard so it is easily visible in VR.")]
    [SerializeField] float _keyboardScale = 2.5f;

    void Start()
    {
        ConfigureKeyboard();
    }

    /// <summary>
    /// Applies placement and scale to the global keyboard. Safe to call again (e.g. from editor button).
    /// </summary>
    public void ConfigureKeyboard()
    {
        if (GlobalNonNativeKeyboard.instance == null)
        {
            Debug.LogWarning("SpatialKeyboardPlacement: GlobalNonNativeKeyboard.instance not found. Is the XRI Global Keyboard Manager in the scene?", this);
            return;
        }

        GlobalNonNativeKeyboard manager = GlobalNonNativeKeyboard.instance;

        manager.keyboardOffset = new Vector3(_horizontalOffset, _heightOffset, _distanceForward);

        if (manager.keyboard != null)
        {
            manager.keyboard.transform.localScale = Vector3.one * _keyboardScale;
        }
    }
}
