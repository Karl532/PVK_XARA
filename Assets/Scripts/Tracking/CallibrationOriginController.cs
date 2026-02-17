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

        if (calibrationOrigin == null)
        {
            calibrationOrigin = transform;
        }

        string originParent = calibrationOrigin.parent != null ? calibrationOrigin.parent.name : "null";
        Debug.Log($"[CalibrationOrigin] Origin='{calibrationOrigin.name}' parent='{originParent}'.");
    }

    private void Start()
    {
        TryResolveSettings();
    }

    private void Update()
    {
        if (_settings == null)
        {
            TryResolveSettings();
            if (_settings == null)
                return;
        }

        if (_settings == null || calibrationOrigin == null)
            return;

        int markerId = _settings.calibrationMarkerId;

        if (!FiducialTrackingManager.Instance.TryGetMarkerPose(markerId, out var marker))
            return; // marker not currently tracked

        // Marker pose (convert to world if tracking root is set)
        Transform trackingRoot = FiducialTrackingManager.Instance.TrackingRoot;
        Quaternion markerRot = marker.rotation;
        Vector3 markerPos = marker.position;
        if (trackingRoot != null)
        {
            markerPos = trackingRoot.TransformPoint(markerPos);
            markerRot = trackingRoot.rotation * markerRot;
        }

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
        if (_settings == null)
            TryResolveSettings();

        if (_settings == null || calibrationOrigin == null)
            return;

        int markerId = _settings.calibrationMarkerId;

        if (!FiducialTrackingManager.Instance.TryGetMarkerPose(markerId, out var marker))
        {
            Debug.LogWarning("[CalibrationOrigin] Cannot calibrate: marker not tracked.");
            return;
        }

        // Marker pose (convert to world if tracking root is set)
        Transform trackingRoot = FiducialTrackingManager.Instance.TrackingRoot;
        Quaternion markerRot = marker.rotation;
        Vector3 markerPos = marker.position;
        if (trackingRoot != null)
        {
            markerPos = trackingRoot.TransformPoint(markerPos);
            markerRot = trackingRoot.rotation * markerRot;
        }

        Quaternion originRotWorld = calibrationOrigin.rotation;
        Vector3 originPosWorld = calibrationOrigin.position;

        // Compute origin pose in marker space:
        // originOffsetRotation = marker^-1 * origin
        // originOffsetPosition = marker^-1 * (originPos - markerPos)
        _settings.originOffsetRotation = Quaternion.Inverse(markerRot) * originRotWorld;
        _settings.originOffsetPosition = Quaternion.Inverse(markerRot) * (originPosWorld - markerPos);

        Debug.Log("[CalibrationOrigin] Calibration saved.");
    }

    private void TryResolveSettings()
    {
        _settings = SettingsManager.Instance != null ? SettingsManager.Instance.settings : null;
        if (_settings == null)
        {
            Debug.LogWarning("[CalibrationOrigin] Settings not found yet. Will retry.");
        }
    }
}
