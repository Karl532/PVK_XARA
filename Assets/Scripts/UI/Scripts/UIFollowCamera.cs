using UnityEngine;

/// <summary>
/// Makes the UI follow the camera, always positioned at the center of view
/// and facing the camera like a billboard, regardless of manual rotation.
/// </summary>
public class UIFollowCamera : MonoBehaviour
{
    public Transform xrCamera;
    public Vector3 offset = new Vector3(0, 0, 2); // 2 meters in front

    void LateUpdate()
    {
        if (xrCamera == null) return;
        if (SettingsManager.Instance?.settings?.uiFollowCamera != true) return;

        // Position the UI directly in front of the camera center at the specified distance.
        // Use only the forward direction from the camera, applying the offset distance.
        Vector3 targetPosition = xrCamera.position + xrCamera.forward * offset.z;
        
        // Apply any vertical/horizontal offset if needed
        if (offset.y != 0)
            targetPosition += xrCamera.up * offset.y;
        if (offset.x != 0)
            targetPosition += xrCamera.right * offset.x;
        
        transform.position = targetPosition;

        // Make the UI face the camera directly (billboard style).
        // We want the UI's forward to point toward the camera, which means
        // we look from UI position toward camera position.
        Vector3 directionToCamera = xrCamera.position - transform.position;
        
        // Keep the UI upright by projecting the direction onto the horizontal plane
        // for the forward direction, but keeping Y up.
        directionToCamera.y = 0;
        
        if (directionToCamera.sqrMagnitude > 0.0001f)
        {
            directionToCamera.Normalize();
            transform.rotation = Quaternion.LookRotation(directionToCamera, Vector3.up);
        }
    }
}
