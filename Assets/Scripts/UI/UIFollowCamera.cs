using UnityEngine;

public class UIFollowCamera : MonoBehaviour
{
    public Transform xrCamera;
    public Vector3 offset = new Vector3(0, 0, 2); // 2 meters in front

    void LateUpdate()
    {
        if (xrCamera == null) return;
        if (SettingsManager.Instance?.settings?.uiFollowCamera != true) return;

        transform.position = xrCamera.position + xrCamera.forward * offset.z +
                             xrCamera.up * offset.y + xrCamera.right * offset.x;

        transform.LookAt(xrCamera);
        transform.Rotate(0, 180f, 0);
    }
}
