using System;
using UnityEngine;
using KeyBinding;

/// <summary>
/// Controls the block placement mode: creates a semi-transparent block, handles 3D movement via thumbsticks.
/// B places the block and exits. Suppresses normal keybinds while active.
/// </summary>
public class BlockPlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera xrCamera;
    [SerializeField] private float spawnDistance = 2f;

    [Header("Block Appearance")]
    [SerializeField] private Color blockColor = new Color(0.3f, 0.6f, 1f, 0.2f);
    [SerializeField] private Color glowColor = new Color(0.2f, 0.5f, 0.9f, 1f);

    private GameObject _block;
    private GameObject _instructionCanvas;
    private bool _isActive;
    private Transform _cameraTransform;

    public bool IsActive => _isActive;

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

        // Reuse existing block if it was already placed once; otherwise create it.
        if (_block == null)
            _block = BlockPlacementBlockUtility.CreateBlock(xrCamera, spawnDistance, blockColor, glowColor);
        else
            BlockPlacementBlockUtility.SetBlockVisibility(_block, true);

        _instructionCanvas = BlockPlacementInstructionUIFactory.CreateInstructionUI(xrCamera);
        Debug.Log("[BlockPlacement] Entered placement mode. Move: thumbsticks | Place & Exit: B");
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

        // Hide the block visuals and interaction, but keep it in the scene as the
        // reference point for loading models. Users can re-enter placement mode
        // to adjust it.
        BlockPlacementBlockUtility.SetBlockVisibility(_block, false);
        Debug.Log("[BlockPlacement] Exited placement mode, block stays as hidden reference.");
    }

    void Update()
    {
        if (!_isActive || _block == null) return;

        // Keep block size in sync with settings
        var settings = SettingsManager.Instance?.settings;
        if (settings != null)
            _block.transform.localScale = settings.stoneBlockDimensions;

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

        _block.transform.position += move;
    }
}
