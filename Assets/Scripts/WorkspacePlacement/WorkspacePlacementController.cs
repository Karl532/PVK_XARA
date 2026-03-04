using System;
using UnityEngine;
using KeyBinding;

/// <summary>
/// Controls the workspace bounds placement mode: creates the workspace cube, handles 3D movement via thumbsticks.
/// B places the workspace and exits. Suppresses normal keybinds while active.
/// </summary>
public class WorkspacePlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera xrCamera;
    [SerializeField] private float spawnDistance = 2f;

    [Header("Workspace Appearance")]
    [SerializeField] private Color workspaceColor = new Color(0.3f, 0.6f, 1f, 0.2f);
    [SerializeField] private Color glowColor = new Color(0.2f, 0.5f, 0.9f, 1f);

    [Header("Placement Trigger")]
    [SerializeField] private bool enterPlacementOnGrab = true;
    [SerializeField] private bool allowGrabWhenInactive = true;

    private GameObject _workspace;
    private GameObject _instructionCanvas;
    private bool _isActive;
    private Transform _cameraTransform;
    private WorkspaceMovementState _movementState;

    public bool IsActive => _isActive;
    public GameObject Workspace => _workspace;

    void Start()
    {
        if (xrCamera == null)
            xrCamera = Camera.main;
        _cameraTransform = xrCamera != null ? xrCamera.transform : transform;
    }

    public void EnterPlacementMode()
    {
        if (_isActive) return;

        _isActive = true;
        KeyBindRegistry.SuppressAll = true;

        // Reuse existing workspace if it was already placed once; otherwise create it.
        if (_workspace == null)
            _workspace = WorkspaceBoundsUtility.CreateWorkspace(xrCamera, spawnDistance, workspaceColor, glowColor);
        else
            WorkspaceBoundsUtility.SetWorkspaceVisibility(_workspace, true);

        _movementState = _workspace.GetComponent<WorkspaceMovementState>();

        _instructionCanvas = WorkspacePlacementInstructionUIFactory.CreateInstructionUI(xrCamera);
        Debug.Log("[WorkspacePlacement] Entered placement mode. Move: thumbsticks | Place & Exit: B");
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

        // Hide the workspace visuals and interaction, but keep it in the scene as the
        // reference point for loading models. Users can re-enter placement mode
        // to adjust it.
        bool allowInteraction = allowGrabWhenInactive && enterPlacementOnGrab;
        WorkspaceBoundsUtility.SetWorkspaceState(_workspace, visible: false, interactable: allowInteraction);
        Debug.Log("[WorkspacePlacement] Exited placement mode, workspace stays as hidden reference.");

        var settings = SettingsManager.Instance?.settings;
        if (settings != null && _workspace != null)
        {
            var origin = CalibrationOriginController.OriginTransform;
            if (origin != null)
            {
                settings.workspacePosition = origin.InverseTransformPoint(_workspace.transform.position);
                settings.workspaceRotationEuler = (Quaternion.Inverse(origin.rotation) * _workspace.transform.rotation).eulerAngles;
            }
            else
            {
                settings.workspacePosition = _workspace.transform.position;
                settings.workspaceRotationEuler = _workspace.transform.rotation.eulerAngles;
            }
            Debug.Log($"[WorkspacePlacement] Saved workspace to settings pos={settings.workspacePosition} rot={settings.workspaceRotationEuler}");
        }
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

        // Keep workspace size in sync with settings
        var settings = SettingsManager.Instance?.settings;
        if (settings != null)
            _workspace.transform.localScale = settings.stoneBlockDimensions;

        // Place & Exit: B button
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            ExitPlacementMode();
            return;
        }

        // Movement: Right thumbstick = XZ, Left thumbstick Y = vertical
        Vector2 rightStick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        Vector2 leftStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        Vector3 right = _cameraTransform.right;
        right.y = 0;
        right.Normalize();
        Vector3 forward = _cameraTransform.forward;
        forward.y = 0;
        forward.Normalize();

        float sensitivity = (SettingsManager.Instance?.settings != null)
            ? SettingsManager.Instance.settings.blockPlacementMovementSensitivity
            : 1f;
        Vector3 move = (right * rightStick.x + forward * rightStick.y) * sensitivity * Time.deltaTime;
        move.y = leftStick.y * sensitivity * Time.deltaTime;

        if (move.sqrMagnitude > 0f)
        {
            _workspace.transform.position += move;
            if (_movementState != null)
                _movementState.MarkMoved();
        }

        // Rotation: Left thumbstick X = rotate around Y axis
        float rotSensitivity = (SettingsManager.Instance?.settings != null)
            ? SettingsManager.Instance.settings.blockPlacementRotationSensitivity
            : 1f;
        float rotateInput = leftStick.x;
        if (Mathf.Abs(rotateInput) > 0.05f)
        {
            float rotationSpeed = 90f; // degrees per second at sensitivity 1
            float yaw = rotateInput * rotationSpeed * rotSensitivity * Time.deltaTime;
            _workspace.transform.Rotate(0f, yaw, 0f, Space.World);
            if (_movementState != null)
                _movementState.MarkMoved();
        }
    }

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
        {
            baseType = baseType.BaseType;
        }

        var prop = (baseType ?? grabType).GetProperty("isSelected");
        if (prop == null || !prop.CanRead) return false;
        return prop.GetValue(grab) is bool selected && selected;
    }
}
