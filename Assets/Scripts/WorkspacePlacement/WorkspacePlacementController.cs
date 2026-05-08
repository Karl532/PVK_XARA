using System;
using UnityEngine;
using KeyBinding;
using System.Runtime.CompilerServices;

/// <summary>
/// Controls the workspace bounds placement mode.
///
/// Controls (during placement mode):
///   Right stick  – Move workspace XZ
///   Left stick Y – Move workspace up/down
///   Left stick X – Rotate around Y
///   A button     – QR-snap workspace to 4 detected corner markers
///   B button     – Confirm placement and exit
/// </summary>
public class WorkspacePlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera xrCamera;
    [SerializeField] private float spawnDistance = 2f;

    [Header("Workspace Appearance")]
    [SerializeField] private Color workspaceColor = new Color(0.3f, 0.6f, 1f, 1f);
    [SerializeField] private Color glowColor = new Color(0.2f, 0.5f, 0.9f, 1f);

    [Header("Placement Trigger")]
    [SerializeField] private bool enterPlacementOnGrab = true;
    [SerializeField] private bool allowGrabWhenInactive = true;

    private GameObject _workspace;
    private GameObject _instructionCanvas;
    private bool _isActive;
    private Transform _cameraTransform;
    private WorkspaceMovementState _movementState;
    private QrWorkspaceSnapper _qrSnapper;
    private float _lastAppliedHeight = -1f;

    public bool IsActive => _isActive;
    public GameObject Workspace => _workspace;

    float cachedOpacity = -1f;


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

        _movementState     = _workspace.GetComponent<WorkspaceMovementState>();
        _lastAppliedHeight = _workspace.transform.localScale.y;

        _instructionCanvas = WorkspacePlacementInstructionUIFactory.CreateInstructionUI(
            xrCamera, this, _qrSnapper);

        //update thae style so that we don't get 1 frame of wrong styling
        UpdateWorkspaceStyle();

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
        TryFitModelToWorkspace();
    }

    private void TryFitModelToWorkspace()
    {
        var settings = SettingsManager.Instance?.settings;
        if (settings == null || !settings.modelFitToWorkspace || _workspace == null) return;

        var modelGO = GameObject.Find("RuntimeModel");
        if (modelGO == null) return;

        RuntimeModelPositionUtility.FitModelToWorkspace(modelGO.transform, _workspace.transform, settings);
        RuntimeModelPositionUtility.RepositionModelRelativeToWorkspace(modelGO.transform, _workspace.transform, settings);
        Debug.Log("[WorkspacePlacement] Auto-fitted model after workspace resize.");
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

        var settings = SettingsManager.Instance?.settings;
        float sensitivity = settings?.blockPlacementMovementSensitivity ?? 1f;

        Vector2 rightStick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        Vector2 leftStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        HandleMovement(rightStick, leftStick, sensitivity);
        HandleRotation(leftStick, settings);

        // Keep visual scale in sync with settings (set by QR snap or external tools).
        // When height changes, shift the workspace up by half the delta so the
        // bottom face stays fixed (scales from the bottom, not the centre).
        if (settings != null)
        {
            float newHeight = settings.stoneBlockDimensions.y;
            if (_lastAppliedHeight > 0f && !Mathf.Approximately(_lastAppliedHeight, newHeight))
                _workspace.transform.position += Vector3.up * ((newHeight - _lastAppliedHeight) * 0.5f);

            _lastAppliedHeight = newHeight;
            _workspace.transform.localScale = settings.stoneBlockDimensions;
        }
        
        //update workspace style (opacity)
        UpdateWorkspaceStyle();
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
        float rotateInput = leftStick.x;

        if (Mathf.Abs(rotateInput) > 0.05f)
        {
            float yaw = rotateInput * 90f * rotSensitivity * Time.deltaTime;
            _workspace.transform.Rotate(0f, yaw, 0f, Space.World);
            _movementState?.MarkMoved();
        }
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
            settings.workspacePosition = origin.InverseTransformPoint(_workspace.transform.position);
            settings.workspaceRotationEuler = (Quaternion.Inverse(origin.rotation) * _workspace.transform.rotation).eulerAngles;
        }
        else
        {
            settings.workspacePosition = _workspace.transform.position;
            settings.workspaceRotationEuler = _workspace.transform.rotation.eulerAngles;
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

    // ──────────────────────────────────────────────────────────────────
    //  Updating workspace style (opacity)
    // ──────────────────────────────────────────────────────────────────
    private void UpdateWorkspaceStyle()
    {
        var settings = SettingsManager.Instance?.settings;
        //this function exists to detect when workspace style settings have changed and then update the material
        if (settings == null) return;

        if (settings.workspaceOpacity != cachedOpacity)
        {
            cachedOpacity = settings.workspaceOpacity;


            float alpha = Mathf.Clamp01(cachedOpacity / 100f);

            var renderer = _workspace.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                Material mat = renderer.material;
                //the material has multiple fallbacks if it doesn't find the default so we have to handle these cases
                //"PVK/PlacementBlockDepthChams"
                if (mat.HasProperty("_Alpha")) { 
                    mat.SetFloat("_Alpha", alpha);
                }
                else { 
                    //"URP/Standard"
                    if (mat.HasProperty("_Color"))
                    {
                        Color c = mat.GetColor("_Color");
                        c.a = alpha;
                        mat.SetColor("_Color", c);
                    }

                    if (mat.HasProperty("_BaseColor"))
                    {
                        Color c = mat.GetColor("_BaseColor");
                        c.a = alpha;
                        mat.SetColor("_BaseColor", c);
                    }
                }
            }
        }

    }

}
