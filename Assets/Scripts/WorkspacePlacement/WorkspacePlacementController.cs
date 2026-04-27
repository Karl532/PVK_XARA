using System;
using UnityEngine;
using KeyBinding;

/// <summary>
/// Controls the workspace bounds placement mode.
///
/// Controls (during placement mode):
///   Right stick           – Move workspace XZ
///   Left stick Y          – Move workspace up/down
///   Left stick X          – Rotate around Y
///   Hold Right Grip       – RESIZE mode:
///       Right stick X     – Change Width  (X dimension)
///       Right stick Y     – Change Depth  (Z dimension)
///       Left stick Y      – Change Height (Y dimension)
///   A button              – QR-snap workspace to 4 detected corner markers
///   B button              – Confirm placement and exit
/// </summary>
public class WorkspacePlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera xrCamera;
    [SerializeField] private float spawnDistance = 2f;

    [Header("Workspace Appearance")]
    [SerializeField] private Color workspaceColor = new Color(0.3f, 0.6f, 1f, 0.2f);
    [SerializeField] private Color glowColor      = new Color(0.2f, 0.5f, 0.9f, 1f);

    [Header("Placement Trigger")]
    [SerializeField] private bool enterPlacementOnGrab = true;
    [SerializeField] private bool allowGrabWhenInactive = true;

    [Header("Resize")]
    [Tooltip("How fast each dimension changes when resizing (metres per second at sensitivity 1).")]
    [SerializeField] private float resizeSpeed = 0.5f;
    [Tooltip("Minimum allowed dimension on any axis (metres).")]
    [SerializeField] private float minDimension = 0.1f;
    [Tooltip("Maximum allowed dimension on any axis (metres).")]
    [SerializeField] private float maxDimension = 10f;

    private GameObject _workspace;
    private GameObject _instructionCanvas;
    private bool _isActive;
    private Transform _cameraTransform;
    private WorkspaceMovementState _movementState;
    private QrWorkspaceSnapper _qrSnapper;

    public bool IsActive => _isActive;
    public GameObject Workspace => _workspace;

    void Start()
    {
        if (xrCamera == null)
            xrCamera = Camera.main;
        _cameraTransform = xrCamera != null ? xrCamera.transform : transform;
        _qrSnapper = GetComponent<QrWorkspaceSnapper>();
    }

    public void EnterPlacementMode()
    {
        if (_isActive) return;

        _isActive = true;
        KeyBindRegistry.SuppressAll = true;

        // Reuse existing workspace if already placed; otherwise create it.
        if (_workspace == null)
            _workspace = WorkspaceBoundsUtility.CreateWorkspace(xrCamera, spawnDistance, workspaceColor, glowColor);
        else
            WorkspaceBoundsUtility.SetWorkspaceVisibility(_workspace, true);

        _movementState = _workspace.GetComponent<WorkspaceMovementState>();

        _instructionCanvas = WorkspacePlacementInstructionUIFactory.CreateInstructionUI(
            xrCamera, this, _qrSnapper);

        Debug.Log("[WorkspacePlacement] Entered placement mode.");
    }

    public void ExitPlacementMode()
    {
        if (!_isActive) return;

        _isActive = false;
        KeyBindRegistry.SuppressAll = false;

        if (_instructionCanvas != null)
        {
            Destroy(_instructionCanvas);
            _instructionCanvas = null;
        }

        bool allowInteraction = allowGrabWhenInactive && enterPlacementOnGrab;
        WorkspaceBoundsUtility.SetWorkspaceState(_workspace, visible: false, interactable: allowInteraction);
        Debug.Log("[WorkspacePlacement] Exited placement mode.");

        SaveWorkspaceToSettings();
    }

    void Update()
    {
        if (!_isActive)
        {
            if (enterPlacementOnGrab && _workspace != null && IsWorkspaceGrabbed())
                EnterPlacementMode();
            return;
        }

        if (_workspace == null) return;
        if (_movementState == null)
            _movementState = _workspace.GetComponent<WorkspaceMovementState>();

        // B: confirm and exit
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            ExitPlacementMode();
            return;
        }

        // A: QR snap (delegated to QrWorkspaceSnapper; it also listens itself,
        // but we call it here too so it works even without the component on this GO)
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            if (_qrSnapper != null)
                _qrSnapper.TrySnapToQrCorners();
            // After a snap the size may have changed; sync immediately
        }

        var settings = SettingsManager.Instance?.settings;
        float sensitivity = settings?.blockPlacementMovementSensitivity ?? 1f;

        bool resizing = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger) > 0.5f;

        Vector2 rightStick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        Vector2 leftStick  = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        if (resizing)
        {
            HandleResize(rightStick, leftStick, settings);
        }
        else
        {
            HandleMovement(rightStick, leftStick, sensitivity);
            HandleRotation(leftStick, settings);
        }

        // Always keep visual scale in sync with settings
        if (settings != null)
            _workspace.transform.localScale = settings.stoneBlockDimensions;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Movement & Rotation
    // ──────────────────────────────────────────────────────────────────

    private void HandleMovement(Vector2 rightStick, Vector2 leftStick, float sensitivity)
    {
        Vector3 right = _cameraTransform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 forward = _cameraTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 move = (right * rightStick.x + forward * rightStick.y) * sensitivity * Time.deltaTime;
        move.y = leftStick.y * sensitivity * Time.deltaTime;

        if (move.sqrMagnitude > 0f)
        {
            _workspace.transform.position += move;
            _movementState?.MarkMoved();
        }
    }

    private void HandleRotation(Vector2 leftStick, Settings settings)
    {
        float rotSensitivity = settings?.blockPlacementRotationSensitivity ?? 1f;
        float rotateInput    = leftStick.x;

        if (Mathf.Abs(rotateInput) > 0.05f)
        {
            float yaw = rotateInput * 90f * rotSensitivity * Time.deltaTime;
            _workspace.transform.Rotate(0f, yaw, 0f, Space.World);
            _movementState?.MarkMoved();
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Resize (hold Right Grip)
    // ──────────────────────────────────────────────────────────────────

    private void HandleResize(Vector2 rightStick, Vector2 leftStick, Settings settings)
    {
        if (settings == null) return;

        float speed = resizeSpeed * Time.deltaTime;

        Vector3 dims = settings.stoneBlockDimensions;

        // Right stick X → Width (X), Right stick Y → Depth (Z), Left stick Y → Height (Y)
        dims.x += rightStick.x * speed;
        dims.z += rightStick.y * speed;
        dims.y += leftStick.y  * speed;

        dims.x = Mathf.Clamp(dims.x, minDimension, maxDimension);
        dims.y = Mathf.Clamp(dims.y, minDimension, maxDimension);
        dims.z = Mathf.Clamp(dims.z, minDimension, maxDimension);

        bool changed = dims != settings.stoneBlockDimensions;
        settings.stoneBlockDimensions = dims;

        if (changed)
            _movementState?.MarkMoved();
    }

    // ──────────────────────────────────────────────────────────────────
    //  Persistence
    // ──────────────────────────────────────────────────────────────────

    private void SaveWorkspaceToSettings()
    {
        var settings = SettingsManager.Instance?.settings;
        if (settings == null || _workspace == null) return;

        var origin = CalibrationOriginController.OriginTransform;
        if (origin != null)
        {
            settings.workspacePosition       = origin.InverseTransformPoint(_workspace.transform.position);
            settings.workspaceRotationEuler  = (Quaternion.Inverse(origin.rotation) * _workspace.transform.rotation).eulerAngles;
        }
        else
        {
            settings.workspacePosition       = _workspace.transform.position;
            settings.workspaceRotationEuler  = _workspace.transform.rotation.eulerAngles;
        }

        Debug.Log($"[WorkspacePlacement] Saved pos={settings.workspacePosition} rot={settings.workspaceRotationEuler} dims={settings.stoneBlockDimensions}");
    }

    // ──────────────────────────────────────────────────────────────────
    //  Grab detection (reflection – compatible with XRI 2 and 3)
    // ──────────────────────────────────────────────────────────────────

    private bool IsWorkspaceGrabbed()
    {
        var grabType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
        if (grabType == null)
            grabType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
        if (grabType == null) return false;

        var grab = _workspace.GetComponent(grabType);
        if (grab == null) return false;

        var baseType = grabType.BaseType;
        while (baseType != null && baseType.FullName != null && !baseType.FullName.Contains("XRBaseInteractable"))
            baseType = baseType.BaseType;

        var prop = (baseType ?? grabType).GetProperty("isSelected");
        if (prop == null || !prop.CanRead) return false;
        return prop.GetValue(grab) is bool selected && selected;
    }
}
