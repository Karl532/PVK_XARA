using UnityEngine;
using Meta.XR.MRUtilityKit;
using Meta.XR;

/// <summary>
/// Disables MRUK PassthroughCameraAccess when not running on a Quest device (e.g. Editor, XR Simulator, PCVR).
/// Attach this to the same GameObject that has PassthroughCameraAccess.
/// </summary>
public class PassthroughRuntimeGuard : MonoBehaviour
{
    private void Awake()
    {
        var passthrough = GetComponent<PassthroughCameraAccess>();
        if (passthrough == null)
            return;

        // Only allow passthrough on Android Quest devices.
        bool isQuestDevice =
            Application.platform == RuntimePlatform.Android &&
            SystemInfo.deviceModel != null &&
            SystemInfo.deviceModel.Contains("Quest");

        if (!isQuestDevice)
        {
            passthrough.enabled = false;
        }
    }
}

