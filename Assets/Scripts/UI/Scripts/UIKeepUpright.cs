using UnityEngine;

/// <summary>
/// Keeps a world-space UI panel upright (no tipping upside down), while still
/// allowing yaw rotation. Intended to be used together with XRGrabInteractable.
/// </summary>
public class UIKeepUpright : MonoBehaviour
{
    void LateUpdate()
    {
        // Project the current forward vector onto the horizontal plane so we
        // preserve yaw but remove pitch/roll.
        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 1e-4f)
        {
            forward = Vector3.forward;
        }
        else
        {
            forward.Normalize();
        }

        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }
}

