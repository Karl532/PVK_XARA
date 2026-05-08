using System;
using UnityEngine;

/// <summary>
/// Snaps the workspace to the rectangle defined by 4 physical QR codes placed at the corners.
///
/// ═══════════════════════════════════════════════════════════════════
/// INTERFACE FOR THE TRACKING SYSTEM DEVELOPER
/// ═══════════════════════════════════════════════════════════════════
///
/// Your tracking backend must call the following each frame a marker is detected:
///   FiducialTrackingManager.Instance.UpdateMarkerPose(id, worldPosition, rotation)
///
/// Positions must be in Unity world space (or in TrackingRoot local space if
/// FiducialTrackingManager.TrackingRoot is assigned).
///
/// QR code payload format:
///   Each QR code must encode its integer ID as plain text, e.g. "10", "11", "12", "13".
///   The MetaQrCodeBackend parses this automatically.
///
/// Physical corner layout (viewed from above):
///   [NearLeft=10] ────── [NearRight=11]
///         │                     │
///   [FarLeft=13]  ────── [FarRight=12]
///
/// IDs are configurable in Settings.qrCornerMarkerIds (default: 10, 11, 12, 13).
/// The order in the array maps to: [0]=NearLeft, [1]=NearRight, [2]=FarRight, [3]=FarLeft.
///
/// Snap is triggered by the user pressing A while all 4 markers are visible, or
/// automatically each frame when autoSnap = true.
/// ═══════════════════════════════════════════════════════════════════
/// </summary>
public class QrWorkspaceSnapper : MonoBehaviour
{
    /// <summary>Current detection / snap state for UI feedback.</summary>
    public enum SnapState
    {
        Idle,             // No markers detected
        PartialDetection, // Some (but not all 4) markers seen
        ReadyToSnap,      // All 4 markers tracked – waiting for user input
        Snapped,          // Workspace was successfully snapped
        Error             // Geometry was degenerate (markers too close, etc.)
    }

    /// <summary>
    /// Fired after a successful snap.
    /// Args: snapped world position, rotation, and workspace size (W, H, D).
    /// </summary>
    public static event Action<Vector3, Quaternion, Vector3> OnWorkspaceSnapped;

    // ──────────────────────────────────────────────────────────────
    //  Inspector fields
    // ──────────────────────────────────────────────────────────────

    [Header("Corner Marker IDs")]
    [Tooltip("IDs of the 4 corner QR codes. Order: NearLeft, NearRight, FarRight, FarLeft.\n"
           + "Override here or set globally in Settings.qrCornerMarkerIds.")]
    [SerializeField] private int[] cornerMarkerIds = new int[] { 10, 11, 12, 13 };

    [Header("Snap Behavior")]
    [Tooltip("Automatically snap every frame while all 4 markers are visible.")]
    [SerializeField] private bool autoSnap = false;

    [Tooltip("Lerp speed when autoSnap is on (0 = instant).")]
    [SerializeField] private float autoSnapLerpSpeed = 6f;

    [Header("References")]
    [SerializeField] private WorkspacePlacementController placementController;

    // ──────────────────────────────────────────────────────────────
    //  Runtime state
    // ──────────────────────────────────────────────────────────────

    private SnapState _state = SnapState.Idle;
    private int _detectedCount; // currently live this frame
    private int _seenCount;     // ever seen (historical)
    private Vector3 _lastSnapPosition;
    private Quaternion _lastSnapRotation;
    private Vector3 _lastSnapSize;

    // ──────────────────────────────────────────────────────────────
    //  Public read-only properties (used by HUD)
    // ──────────────────────────────────────────────────────────────

    public SnapState CurrentState => _state;

    /// <summary>Number of corner markers currently live in the tracker (0–4).</summary>
    public int DetectedCornerCount => _detectedCount;

    /// <summary>Number of corner markers ever seen, even if now out of view (0–4).</summary>
    public int SeenCornerCount => _seenCount;

    /// <summary>Total number of corners expected (always 4).</summary>
    public int TotalCornerCount => cornerMarkerIds?.Length ?? 0;

    /// <summary>Whether a specific corner is currently live in the tracker.</summary>
    public bool IsCornerTracked(int index)
    {
        if (FiducialTrackingManager.Instance == null) return false;
        if (index < 0 || index >= cornerMarkerIds.Length) return false;
        return FiducialTrackingManager.Instance.TryGetMarkerPose(cornerMarkerIds[index], out _);
    }

    /// <summary>Whether a specific corner has ever been seen (even if now out of view).</summary>
    public bool IsCornerEverSeen(int index)
    {
        if (FiducialTrackingManager.Instance == null) return false;
        if (index < 0 || index >= cornerMarkerIds.Length) return false;
        return FiducialTrackingManager.Instance.TryGetLastKnownMarkerPose(cornerMarkerIds[index], out _);
    }

    /// <summary>Last successfully computed workspace size (W, H, D).</summary>
    public Vector3 ComputedSize => _lastSnapSize;

    // ──────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ──────────────────────────────────────────────────────────────

    void Awake()
    {
        if (placementController == null)
            placementController = GetComponent<WorkspacePlacementController>()
                ?? FindObjectOfType<WorkspacePlacementController>();
    }

    void Update()
    {
        SyncSettingsIds();
        RefreshDetectionState();

        if (autoSnap && _state == SnapState.ReadyToSnap)
            TrySnapToQrCorners(lerp: true);
    }

    // ──────────────────────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to snap the workspace to the 4 QR corners.
    /// Uses last-known poses so it works even if markers briefly leave view.
    /// Returns true on success.
    /// </summary>
    public bool TrySnapToQrCorners(bool lerp = false)
    {
        if (FiducialTrackingManager.Instance == null)
        {
            Debug.LogWarning("[QrWorkspaceSnapper] FiducialTrackingManager not found.");
            _state = SnapState.Error;
            return false;
        }

        Transform trackingRoot = FiducialTrackingManager.Instance.TrackingRoot;
        var positions = new Vector3[4];

        for (int i = 0; i < 4; i++)
        {
            // Allow last-known so snap works even when marker temporarily leaves FOV
            bool found = FiducialTrackingManager.Instance.TryGetLastKnownMarkerPose(
                cornerMarkerIds[i], out var pose);

            if (!found)
            {
                Debug.LogWarning(
                    $"[QrWorkspaceSnapper] Corner marker id={cornerMarkerIds[i]} has no pose yet. Place all 4 QR codes in view first.");
                _state = SnapState.Error;
                return false;
            }

            // MetaQrCodeBackend supplies t.transform.position which is already world space.
            // Only apply trackingRoot transform when the backend stores positions in local space
            // (i.e. when trackingRoot is not a world-space root such as the XR camera rig).
            Vector3 pos = pose.position;
            if (trackingRoot != null && trackingRoot.parent != null)
                pos = trackingRoot.TransformPoint(pos);
            positions[i] = pos;
        }

        Quaternion currentRotation = placementController?.Workspace != null
            ? placementController.Workspace.transform.rotation
            : Quaternion.identity;

        if (!ComputeWorkspaceFromCorners(positions, currentRotation, out Vector3 center, out Quaternion rotation, out Vector3 size))
        {
            Debug.LogWarning("[QrWorkspaceSnapper] Degenerate geometry – are markers too close together?");
            _state = SnapState.Error;
            return false;
        }

        ApplySnapToWorkspace(center, rotation, size, lerp);
        _state = SnapState.Snapped;
        return true;
    }

    // ──────────────────────────────────────────────────────────────
    //  Private helpers
    // ──────────────────────────────────────────────────────────────

    /// Keep cornerMarkerIds in sync with Settings so the Inspector value
    /// always acts as a project-wide override.
    private void SyncSettingsIds()
    {
        var settings = SettingsManager.Instance?.settings;
        if (settings?.qrCornerMarkerIds != null && settings.qrCornerMarkerIds.Length == 4)
            cornerMarkerIds = settings.qrCornerMarkerIds;
    }

    private void RefreshDetectionState()
    {
        if (FiducialTrackingManager.Instance == null)
        {
            _detectedCount = 0;
            _seenCount     = 0;
            _state = SnapState.Idle;
            return;
        }

        int live = 0;
        int seen = 0;
        for (int i = 0; i < cornerMarkerIds.Length; i++)
        {
            if (FiducialTrackingManager.Instance.TryGetMarkerPose(cornerMarkerIds[i], out _))
                live++;
            if (FiducialTrackingManager.Instance.TryGetLastKnownMarkerPose(cornerMarkerIds[i], out _))
                seen++;
        }

        _detectedCount = live;
        _seenCount     = seen;

        if (_state == SnapState.Snapped || _state == SnapState.Error) return;

        // ReadyToSnap as soon as all 4 have been scanned — they don't all have to be
        // live at the same time. The user scans each code in turn, then presses A.
        _state = seen == 4 ? SnapState.ReadyToSnap
               : seen > 0  ? SnapState.PartialDetection
               :               SnapState.Idle;
    }

    /// <summary>
    /// Computes the workspace centre from the 4 QR corner positions.
    /// Rotation and size are preserved from the current workspace — snap only moves position.
    /// </summary>
    private bool ComputeWorkspaceFromCorners(
        Vector3[] c,
        Quaternion currentRotation,
        out Vector3 center,
        out Quaternion rotation,
        out Vector3 size)
    {
        center   = Vector3.zero;
        rotation = currentRotation; // rotation unchanged
        size     = Vector3.zero;

        // Centroid of the 4 QR codes in the horizontal plane
        center = (c[0] + c[1] + c[2] + c[3]) * 0.25f;

        // Keep all dimensions exactly as they are — scale is set separately
        var settings = SettingsManager.Instance?.settings;
        size = settings != null ? settings.stoneBlockDimensions : Vector3.one;

        if (size.x < 0.001f || size.y < 0.001f || size.z < 0.001f)
            return false;

        // Lift center so the workspace bottom sits at the QR code plane
        center += Vector3.up * (size.y * 0.5f);

        _lastSnapPosition = center;
        _lastSnapRotation = rotation;
        _lastSnapSize     = size;

        return true;
    }

    private void ApplySnapToWorkspace(Vector3 position, Quaternion rotation, Vector3 size, bool lerp)
    {
        if (placementController == null)
        {
            Debug.LogWarning("[QrWorkspaceSnapper] No WorkspacePlacementController referenced.");
            return;
        }

        // Create the workspace cube if it doesn't exist yet
        if (placementController.Workspace == null)
            placementController.EnterPlacementMode();

        GameObject workspace = placementController.Workspace;
        if (workspace == null)
        {
            Debug.LogWarning("[QrWorkspaceSnapper] Workspace still null after entering placement mode.");
            return;
        }

        // Snap only moves position — rotation and scale are unchanged
        if (lerp && autoSnapLerpSpeed > 0f)
        {
            float t = 1f - Mathf.Exp(-autoSnapLerpSpeed * Time.deltaTime);
            workspace.transform.position = Vector3.Lerp(workspace.transform.position, position, t);
        }
        else
        {
            workspace.transform.position = position;
        }

        var settings = SettingsManager.Instance?.settings;
        if (settings != null)
        {
            var origin = CalibrationOriginController.OriginTransform;
            if (origin != null)
            {
                settings.workspacePosition    = origin.InverseTransformPoint(position);
                settings.workspaceRotationEuler = (Quaternion.Inverse(origin.rotation) * rotation).eulerAngles;
            }
            else
            {
                settings.workspacePosition    = position;
                settings.workspaceRotationEuler = rotation.eulerAngles;
            }
        }

        // Refit model to the new workspace size if auto-fit is enabled
        if (settings != null && settings.modelFitToWorkspace)
            TryFitModelToWorkspace(workspace.transform, settings);

        Debug.Log($"[QrWorkspaceSnapper] Snapped → pos={position} size={size} rot={rotation.eulerAngles:F1}");
        OnWorkspaceSnapped?.Invoke(position, rotation, size);
    }

    private static void TryFitModelToWorkspace(Transform workspace, Settings settings)
    {
        var modelGO = GameObject.Find("RuntimeModel");
        if (modelGO == null) return;
        RuntimeModelPositionUtility.FitModelToWorkspace(modelGO.transform, workspace, settings);
        RuntimeModelPositionUtility.RepositionModelRelativeToWorkspace(modelGO.transform, workspace, settings);
    }

    private static Vector3 SafeNorm(Vector3 v)
    {
        float mag = v.magnitude;
        return mag > 0.0001f ? v / mag : Vector3.zero;
    }
}
