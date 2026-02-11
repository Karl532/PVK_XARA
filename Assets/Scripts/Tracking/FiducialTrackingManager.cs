using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central registry for fiducial marker poses (e.g. ArUco).
///
/// Responsibilities:
/// - Accept pose updates from one or more tracking backends.
/// - Maintain the latest pose and tracking state for each marker.
/// - Provide a simple query API for other systems (block placement, model alignment, etc.).
/// - Stay agnostic of the concrete backend so we can swap ArUco / other providers later.
///
/// This class does NOT talk directly to any specific SDK. Instead, tracking providers should
/// call <see cref="UpdateMarkerPose"/> every frame they have a valid detection and
/// <see cref="MarkMarkerLost"/> when a marker is considered lost.
/// </summary>
public class FiducialTrackingManager : MonoBehaviour
{
    #region Singleton

    public static FiducialTrackingManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #endregion

    /// <summary>
    /// Describes a single fiducial marker pose in world space.
    /// </summary>
    [Serializable]
    public struct MarkerPose
    {
        public int id;
        public Vector3 position;
        public Quaternion rotation;
        public float lastSeenTime;
        public bool isTracked;

        public MarkerPose(int id, Vector3 position, Quaternion rotation, float time, bool isTracked)
        {
            this.id = id;
            this.position = position;
            this.rotation = rotation;
            this.lastSeenTime = time;
            this.isTracked = isTracked;
        }
    }

    /// <summary>
    /// How long a marker may go without updates before we consider it lost.
    /// Backends can override this behaviour by explicitly calling MarkMarkerLost.
    /// </summary>
    [Header("Tracking")]
    [Tooltip("Seconds without updates before a marker is considered lost. Set to 0 to disable automatic timeout.")]
    [SerializeField] private float markerTimeout = 0.5f;

    /// <summary>
    /// Optional root transform that marker poses are expressed in.
    /// If null, poses are treated as Unity world-space.
    /// For XR projects, this should typically be the XR Origin / Rig.
    /// </summary>
    [Tooltip("Optional transform that defines the root coordinate space for marker poses (e.g. XR Origin). If null, world space is used.")]
    [SerializeField] private Transform trackingRoot;

    private readonly Dictionary<int, MarkerPose> _markers = new Dictionary<int, MarkerPose>();

    /// <summary>
    /// Returns the transform that marker poses are relative to.
    /// If null, poses are in world space.
    /// </summary>
    public Transform TrackingRoot => trackingRoot;

    /// <summary>
    /// Returns a shallow copy of all known marker poses.
    /// Intended mainly for debugging and UI; for per-frame logic, prefer TryGetMarkerPose.
    /// </summary>
    public IReadOnlyDictionary<int, MarkerPose> AllMarkers => _markers;

    /// <summary>
    /// Called by a tracking backend (e.g. ArUco) when it has a valid pose for a marker.
    ///
    /// <para>
    /// The pose must be expressed in the same coordinate space as <see cref="trackingRoot"/>.
    /// If <see cref="trackingRoot"/> is null, it should be in world space.
    /// </para>
    /// </summary>
    /// <param name="markerId">Unique ID of the marker (e.g. ArUco ID).</param>
    /// <param name="position">Marker position.</param>
    /// <param name="rotation">Marker rotation.</param>
    public void UpdateMarkerPose(int markerId, Vector3 position, Quaternion rotation)
    {
        float time = Time.time;

        if (_markers.TryGetValue(markerId, out var existing))
        {
            existing.position = position;
            existing.rotation = rotation;
            existing.lastSeenTime = time;
            existing.isTracked = true;
            _markers[markerId] = existing;
        }
        else
        {
            _markers[markerId] = new MarkerPose(markerId, position, rotation, time, true);
        }
    }

    /// <summary>
    /// Explicitly mark a marker as lost. Backends that have a precise notion of
    /// visibility should call this instead of relying solely on timeouts.
    /// </summary>
    public void MarkMarkerLost(int markerId)
    {
        if (_markers.TryGetValue(markerId, out var pose))
        {
            pose.isTracked = false;
            _markers[markerId] = pose;
        }
    }

    /// <summary>
    /// Attempts to retrieve the current pose for a marker.
    /// Returns true if the marker exists and is currently tracked.
    /// </summary>
    public bool TryGetMarkerPose(int markerId, out MarkerPose pose)
    {
        if (_markers.TryGetValue(markerId, out pose))
        {
            // If we are using a timeout, auto-mark as lost when stale.
            if (markerTimeout > 0f &&
                pose.isTracked &&
                (Time.time - pose.lastSeenTime) > markerTimeout)
            {
                pose.isTracked = false;
                _markers[markerId] = pose;
            }

            return pose.isTracked;
        }

        return false;
    }

    /// <summary>
    /// Returns the last known pose for a marker, even if it is no longer tracked.
    /// This is useful for calibration workflows that want to keep using the last pose
    /// after the marker disappears from view.
    /// </summary>
    public bool TryGetLastKnownMarkerPose(int markerId, out MarkerPose pose)
    {
        return _markers.TryGetValue(markerId, out pose);
    }

    /// <summary>
    /// Clears all marker data. Typically only useful for debugging or when resetting calibration.
    /// </summary>
    public void ClearAllMarkers()
    {
        _markers.Clear();
    }
}

