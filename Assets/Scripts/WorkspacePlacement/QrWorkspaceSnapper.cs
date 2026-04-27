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
    private int _detectedCount;
    private Vector3 _lastSnapPosition;
    private Quaternion _lastSnapRotation;
    private Vector3 _lastSnapSize;

    // ──────────────────────────────────────────────────────────────
    //  Public read-only properties (used by HUD)
    // ──────────────────────────────────────────────────────────────

    public SnapState CurrentState => _state;

    /// <summary>Number of corner markers currently tracked (0–4).</summary>
    public int DetectedCornerCount => _detectedCount;

    /// <summary>Total number of corners expected (always 4).</summary>
    public int TotalCornerCount => cornerMarkerIds?.Length ?? 0;

    /// <summary>Whether a specific corner index (0–3) is currently tracked.</summary>
    public bool IsCornerTracked(int index)
    {
        if (FiducialTrackingManager.Instance == null) return false;
        if (index < 0 || index >= cornerMarkerIds.Length) return false;
        return FiducialTrackingManager.Instance.TryGetMarkerPose(cornerMarkerIds[index], out _);
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
        {
            TrySnapToQrCorners(lerp: true);
            return;
        }

        // A button: snap on press when ready
        if (OVRInput.GetDown(OVRInput.Button.One) && _state != SnapState.Idle)
            TrySnapToQrCorners(lerp: false);
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

            Vector3 pos = pose.position;
            if (trackingRoot != null)
                pos = trackingRoot.TransformPoint(pos);
            positions[i] = pos;
        }

        if (!ComputeWorkspaceFromCorners(positions, out Vector3 center, out Quaternion rotation, out Vector3 size))
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
            _state = SnapState.Idle;
            return;
        }

        int count = 0;
        for (int i = 0; i < cornerMarkerIds.Length; i++)
        {
            if (FiducialTrackingManager.Instance.TryGetMarkerPose(cornerMarkerIds[i], out _))
                count++;
        }

        _detectedCount = count;
        if (_state == SnapState.Snapped || _state == SnapState.Error) return; // sticky until snap/error is cleared

        _state = count == 4 ? SnapState.ReadyToSnap
               : count > 0  ? SnapState.PartialDetection
               :               SnapState.Idle;
    }

    /// <summary>
    /// Fits a rectangle to the 4 corner world positions and returns workspace transform.
    ///
    /// Corner order: [0]=NearLeft, [1]=NearRight, [2]=FarRight, [3]=FarLeft.
    /// Height (Y) comes from Settings.stoneBlockDimensions.y.
    /// The workspace bottom is placed at the QR code plane level.
    /// </summary>
    private bool ComputeWorkspaceFromCorners(
        Vector3[] c,
        out Vector3 center,
        out Quaternion rotation,
        out Vector3 size)
    {
        center   = Vector3.zero;
        rotation = Quaternion.identity;
        size     = Vector3.zero;

        // Centroid (floor level)
        center = (c[0] + c[1] + c[2] + c[3]) * 0.25f;

        // Width edges: NearLeft→NearRight and FarLeft→FarRight
        Vector3 nearEdge  = c[1] - c[0];
        Vector3 farEdge   = c[2] - c[3];
        // Depth edges: NearLeft→FarLeft and NearRight→FarRight
        Vector3 leftEdge  = c[3] - c[0];
        Vector3 rightEdge = c[2] - c[1];

        float width = (nearEdge.magnitude + farEdge.magnitude)   * 0.5f;
        float depth = (leftEdge.magnitude + rightEdge.magnitude) * 0.5f;

        if (width < 0.01f || depth < 0.01f)
            return false; // Markers practically on top of each other

        // Average normalised edge directions for robustness against imperfect placement
        Vector3 right   = (SafeNorm(nearEdge)  + SafeNorm(farEdge)).normalized;
        Vector3 forward = (SafeNorm(leftEdge)  + SafeNorm(rightEdge)).normalized;

        // Plane normal via cross product
        Vector3 up = Vector3.Cross(right, forward).normalized;
        if (up.y < 0f) { up = -up; right = -right; }  // ensure normal points upward

        // Re-orthogonalise forward against the true up
        forward = Vector3.Cross(up, right).normalized;

        if (right == Vector3.zero || forward == Vector3.zero || up == Vector3.zero)
            return false;

        rotation = Quaternion.LookRotation(forward, up);

        // Height: keep the manually set value from Settings
        var settings = SettingsManager.Instance?.settings;
        float height = settings != null ? settings.stoneBlockDimensions.y : 0.5f;

        // Lift center so the workspace bottom sits exactly at the QR code plane
        center += up * (height * 0.5f);

        size = new Vector3(width, height, depth);

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

        if (lerp && autoSnapLerpSpeed > 0f)
        {
            float t = 1f - Mathf.Exp(-autoSnapLerpSpeed * Time.deltaTime);
            workspace.transform.SetPositionAndRotation(
                Vector3.Lerp(workspace.transform.position, position, t),
                Quaternion.Slerp(workspace.transform.rotation, rotation, t));
            workspace.transform.localScale = Vector3.Lerp(workspace.transform.localScale, size, t);
        }
        else
        {
            workspace.transform.SetPositionAndRotation(position, rotation);
            workspace.transform.localScale = size;
        }

        // Persist to Settings so the workspace survives reloads and other systems see the new size
        var settings = SettingsManager.Instance?.settings;
        if (settings != null)
        {
            settings.stoneBlockDimensions = size;

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

        Debug.Log($"[QrWorkspaceSnapper] Snapped → pos={position} size={size} rot={rotation.eulerAngles:F1}");
        OnWorkspaceSnapped?.Invoke(position, rotation, size);
    }

    private static Vector3 SafeNorm(Vector3 v)
    {
        float mag = v.magnitude;
        return mag > 0.0001f ? v / mag : Vector3.zero;
    }
}
