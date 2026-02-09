using UnityEngine;

/// <summary>
/// First concrete fiducial backend: intended for ArUco marker detection using an external CV library (e.g. OpenCV).
///
/// This class is responsible for:
/// - Acquiring an image corresponding to the XR camera view.
/// - Calling into the ArUco implementation (plugin / asset) to detect markers.
/// - Converting detected poses into Unity world (or XR Origin) space.
/// - Publishing them to <see cref="FiducialTrackingManager"/> via UpdateMarkerPose.
///
/// NOTE: The actual calls into OpenCV/ArUco are left as TODOs, because they depend on
/// the specific integration you choose (OpenCV for Unity, native plugin, etc.).
/// The structure is ready so you can drop those calls in later.
/// </summary>
public class ArucoBackend : IFiducialBackend
{
    private TrackingBridge _bridge;
    private FiducialTrackingManager _trackingManager;
    private TrackingBridge.TrackingBridgeConfig _config;

    /// <summary>
    /// Cached convenience reference to the camera used for detection.
    /// </summary>
    private Camera DetectionCamera => _bridge != null ? _bridge.XrCamera : null;

    public void Initialize(TrackingBridge bridge, FiducialTrackingManager trackingManager)
    {
        _bridge = bridge;
        _trackingManager = trackingManager;
        _config = bridge != null ? bridge.Config : default;

        // TODO: Initialize your ArUco / OpenCV backend here.

        Debug.Log("[ArucoBackend] Initialized (CV backend not yet wired).");
    }

    public void Configure(TrackingBridge.TrackingBridgeConfig config)
    {
        _config = config;


        Debug.Log($"[ArucoBackend] Configured: backend={config.backendType}, family={config.markerFamily}, size={config.markerSizeMeters}m");
    }

    public void ProcessFrame()
    {
        if (_bridge == null || _trackingManager == null || DetectionCamera == null)
            return;

    }

    public void Shutdown()
    {
        // TODO: Release any native / CV resources here.
        Debug.Log("[ArucoBackend] Shutdown.");
        _bridge = null;
        _trackingManager = null;
    }
}

