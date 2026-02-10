using UnityEngine;

public class CalibrationOriginController : MonoBehaviour
{
    public static CalibrationOriginController Instance { get; private set; }

    /// <summary>
    /// Globally accessible transform that represents the calibrated origin.
    /// Returns null if no CalibrationOriginController exists yet.
    /// </summary>
    public static Transform OriginTransform => Instance != null ? Instance.calibrationOrigin : null;

    [Tooltip("Transform that acts as the root for all calibrated content.")]
    [SerializeField] private Transform calibrationOrigin;

    private Settings _settings;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _settings = SettingsManager.Instance != null ? SettingsManager.Instance.settings : null;

        if (_settings == null)
        {
            Debug.LogError("[CalibrationOrigin] Settings not found.");
        }

        if (calibrationOrigin == null)
        {
            calibrationOrigin = transform;
        }
    }

    private void Update()
    {
        if (_settings == null || calibrationOrigin == null)
            return;

        int markerId = _settings.calibrationMarkerId;

        if (!FiducialTrackingManager.Instance.TryGetMarkerPose(markerId, out var marker))
            return; // marker not currently tracked

        // Marker world pose
        Quaternion markerRot = marker.rotation;
        Vector3 markerPos = marker.position;

        // Apply stored offset in marker space
        Quaternion originRot = markerRot * _settings.originOffsetRotation;
        Vector3 originPos = markerPos + markerRot * _settings.originOffsetPosition;

        calibrationOrigin.SetPositionAndRotation(originPos, originRot);
    }

    /// <summary>
    /// Call this once when the user says "set origin now" while the marker and content are aligned.
    /// </summary>
    public void CalibrateNow()
    {
        if (_settings == null || calibrationOrigin == null)
            return;

        int markerId = _settings.calibrationMarkerId;

        if (!FiducialTrackingManager.Instance.TryGetMarkerPose(markerId, out var marker))
        {
            Debug.LogWarning("[CalibrationOrigin] Cannot calibrate: marker not tracked.");
            return;
        }

        // Current world poses
        Quaternion markerRot = marker.rotation;
        Vector3 markerPos = marker.position;

        Quaternion originRotWorld = calibrationOrigin.rotation;
        Vector3 originPosWorld = calibrationOrigin.position;

        // Compute origin pose in marker space:
        // originOffsetRotation = marker^-1 * origin
        // originOffsetPosition = marker^-1 * (originPos - markerPos)
        _settings.originOffsetRotation = Quaternion.Inverse(markerRot) * originRotWorld;
        _settings.originOffsetPosition = Quaternion.Inverse(markerRot) * (originPosWorld - markerPos);

        Debug.Log("[CalibrationOrigin] Calibration saved.");
    }
}