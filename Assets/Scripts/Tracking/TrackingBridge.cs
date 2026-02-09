using System;
using UnityEngine;

/// <summary>
/// Bridge between Unity (XR Origin, cameras, settings) and a concrete fiducial tracking backend.
///
/// Responsibilities:
/// - Own a single active backend (e.g. ArUco) implementing <see cref="IFiducialBackend"/>.
/// - Provide detection configuration (marker family, size, ID filters).
/// - Call into the backend every frame and let it publish poses into <see cref="FiducialTrackingManager"/>.
///
/// This component should be attached to a GameObject in the scene (typically near the XR Origin).
/// </summary>
public class TrackingBridge : MonoBehaviour
{
    /// <summary>
    /// Supported backend types. Only ArUco is planned initially, but this allows
    /// us to add others later without changing consumers.
    /// </summary>
    public enum BackendType
    {
        None = 0,
        ArUco = 1,
        // AprilTag = 2, // Future example
    }

    /// <summary>
    /// Configuration data controlling what and how we detect.
    /// This is kept small on purpose so it can later be moved into a ScriptableObject
    /// or synchronized with your existing Settings asset.
    /// </summary>
    [Serializable]
    public struct TrackingBridgeConfig
    {
        [Tooltip("Which CV backend to use (ArUco, etc.).")]
        public BackendType backendType;

        [Tooltip("Marker family / dictionary name understood by the backend (e.g. DICT_4X4_50).")]
        public string markerFamily;

        [Tooltip("Physical size of a single marker in meters.")]
        public float markerSizeMeters;

        [Tooltip("Optional list of marker IDs to detect. Empty = detect all IDs supported by the family.")]
        public int[] markerIdWhitelist;
    }

    [Header("Unity References")]
    [Tooltip("Camera whose view is used for fiducial detection (e.g. XR camera).")]
    [SerializeField] private Camera xrCamera;

    [Tooltip("Central tracking manager that stores marker poses. If null, will use FiducialTrackingManager.Instance.")]
    [SerializeField] private FiducialTrackingManager trackingManager;

    [Header("Detection Configuration")]
    [SerializeField] private TrackingBridgeConfig config = new TrackingBridgeConfig
    {
        backendType = BackendType.Aruco,
        markerFamily = "DICT_4X4_50",
        markerSizeMeters = 0.05f, // 5 cm default
        markerIdWhitelist = null
    };

    private IFiducialBackend _backend;

    /// <summary>
    /// Current configuration in use by the bridge.
    /// </summary>
    public TrackingBridgeConfig Config => config;

    /// <summary>
    /// Camera used for detection. Backends may read this to obtain projection or image data.
    /// </summary>
    public Camera XrCamera => xrCamera;

    /// <summary>
    /// Tracking manager that receives marker poses.
    /// </summary>
    public FiducialTrackingManager TrackingManager => trackingManager;

    private void Awake()
    {
        if (xrCamera == null)
            xrCamera = Camera.main;

        if (trackingManager == null)
            trackingManager = FiducialTrackingManager.Instance;
    }

    private void OnEnable()
    {
        CreateAndInitializeBackend();
    }

    private void OnDisable()
    {
        ShutdownBackend();
    }

    private void Update()
    {
        if (_backend == null || trackingManager == null)
            return;

        _backend.ProcessFrame();
    }

    /// <summary>
    /// Allows runtime reconfiguration of the bridge and backend (e.g. from UI).
    /// </summary>
    public void ApplyConfig(TrackingBridgeConfig newConfig)
    {
        config = newConfig;

        if (_backend == null)
        {
            CreateAndInitializeBackend();
        }
        else
        {
            _backend.Configure(config);
        }
    }

    /// <summary>
    /// Convenience method for switching backend type at runtime.
    /// </summary>
    public void SetBackendType(BackendType type)
    {
        if (config.backendType == type)
            return;

        config.backendType = type;
        RecreateBackend();
    }

    private void CreateAndInitializeBackend()
    {
        ShutdownBackend();

        if (trackingManager == null)
            trackingManager = FiducialTrackingManager.Instance;

        if (config.backendType == BackendType.None)
        {
            Debug.LogWarning("[TrackingBridge] BackendType is None. No fiducial tracking will be performed.");
            return;
        }

        _backend = CreateBackend(config.backendType);

        if (_backend == null)
        {
            Debug.LogError($"[TrackingBridge] Failed to create backend for type {config.backendType}.");
            return;
        }

        _backend.Initialize(this, trackingManager);
        _backend.Configure(config);
    }

    private void RecreateBackend()
    {
        CreateAndInitializeBackend();
    }

    private void ShutdownBackend()
    {
        if (_backend != null)
        {
            _backend.Shutdown();
            _backend = null;
        }
    }

    /// <summary>
    /// Factory method that instantiates the correct backend implementation.
    /// The concrete ArUco backend will be implemented separately and returned here.
    /// </summary>
    private IFiducialBackend CreateBackend(BackendType type)
    {
        switch (type)
        {
            case BackendType.Aruco:
                return new ArucoBackend();

            case BackendType.None:
            default:
                return null;
        }
    }
}

