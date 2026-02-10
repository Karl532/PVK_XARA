using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
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

        CreateBlock();
        CreateInstructionUI();
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
        if (_block != null)
        {
            Destroy(_block);
            _block = null;
        }
        Debug.Log("[BlockPlacement] Exited placement mode.");
    }

    void CreateBlock()
    {
        Settings settings = SettingsManager.Instance?.settings;
        Vector3 dimensions = settings != null ? settings.stoneBlockDimensions : Vector3.one;

        _block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _block.name = "PlacementBlock";
        _block.transform.localScale = dimensions;

        // Position in front of camera
        Vector3 spawnPos = _cameraTransform.position + _cameraTransform.forward * spawnDistance;
        spawnPos.y = Mathf.Max(spawnPos.y, _cameraTransform.position.y - 0.5f); // Don't spawn too low
        _block.transform.position = spawnPos;

        // Parent under the calibration origin so placement is relative to the fiducial-tracked world origin.
        CalibrationOriginUtility.AttachToOrigin(_block.transform, worldPositionStays: true);

        // Semi-transparent blue material
        Renderer renderer = _block.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = CreateTransparentBlockMaterial();
            renderer.material = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // No collider during placement (optional - remove if you want physics)
        var col = _block.GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    Material CreateTransparentBlockMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard");
        Material mat = new Material(shader);

        if (mat.shader.name.Contains("Universal"))
        {
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);   // Alpha
            mat.SetFloat("_AlphaClip", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", glowColor * 2f);
        }
        else
        {
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", glowColor * 2f);
        }

        mat.SetColor("_BaseColor", blockColor);
        mat.SetColor("_Color", blockColor);
        return mat;
    }

    void CreateInstructionUI()
    {
        _instructionCanvas = new GameObject("BlockPlacementInstructions");
        var canvas = _instructionCanvas.AddComponent<Canvas>();
        // ScreenSpaceCamera works in VR; ScreenSpaceOverlay does not render in the headset
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        Camera cam = xrCamera != null ? xrCamera : Camera.main;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;
        canvas.sortingOrder = 100;

        var scaler = _instructionCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _instructionCanvas.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = _instructionCanvas.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        // Left-aligned top panel
        GameObject panel = new GameObject("InstructionPanel");
        panel.transform.SetParent(_instructionCanvas.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(40, -40);
        panelRect.sizeDelta = new Vector2(480, 120);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.12f, 0.9f);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 24);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        GameObject textObj = new GameObject("InstructionText");
        textObj.transform.SetParent(panel.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Move: Right stick (XZ) + Left stick (Y)\nPlace & Exit: B";
        text.fontSize = 28;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.extraPadding = true;
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
