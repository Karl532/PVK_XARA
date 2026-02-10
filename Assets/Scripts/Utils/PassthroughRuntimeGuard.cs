using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Disables MRUK passthrough when not running on a real Quest device (e.g. in Editor / XR Simulator / Windows).
/// Attach this to the same GameObject that has PassthroughCameraAccess.
/// </summary>
public class PassthroughRuntimeGuard : MonoBehaviour
{
    void Awake()
    {
#if UNITY_EDITOR || !UNITY_ANDROID
        var passthrough = GetComponent<PassthroughCameraAccess>();
        if (passthrough != null)
        {
            passthrough.enabled = false;
        }
#endif
    }
}

