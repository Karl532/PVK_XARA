using UnityEngine;

[CreateAssetMenu(fileName = "Settings", menuName = "Scriptable Objects/Settings")]
public class Settings : ScriptableObject
{
    private static Settings _cached;
    private static bool _resolved;

    public static Settings GetActive()
    {
        if (!_resolved)
        {
            _cached = SettingsManager.Instance != null ? SettingsManager.Instance.settings : null;
            if (_cached == null)
                _cached = FindAnySettingsAsset();
            _resolved = true;
        }

        return _cached;
    }

    public static Settings FindAnySettingsAsset()
    {
        var assets = Resources.FindObjectsOfTypeAll<Settings>();
        if (assets != null && assets.Length > 0)
            return assets[0];
        return null;
    }

    [Header("SDF Visualization")]
    public SdfVisualizationConfig sdfVisualizationConfig;

    [Header("Depth Device")]
    public DepthProviderFactory.DeviceType depthDevice;

    [Header("Target Sculpture")]
    public GameObject targetSculpturePrefab;

    [Header("Stone Block Dimensions")]
    public Vector3 stoneBlockDimensions = new Vector3(1f, 1f, 1f);

    [Header("Workspace Transform")]
    [Tooltip("Workspace position in world space if no Workspace exists.")]
    public Vector3 workspacePosition = Vector3.zero;

    [Tooltip("Workspace rotation in world space (euler) if no Workspace exists.")]
    public Vector3 workspaceRotationEuler = Vector3.zero;

    [Header("Model")]
    [Tooltip("Uniform scale applied to the loaded model. 1 = original size.")]
    public float modelScale = 1f;
    public Vector3 modelOffset = Vector3.zero;

    [Header("Block Options")]
    public bool autoScaleBlock = false;
    public bool blockPlacementEnabled = true;

    [Header("Block Placement")]
    [Range(0f, 3f)]
    [Tooltip("Movement sensitivity when placing block (0 = slow, 3 = fast).")]
    public float blockPlacementMovementSensitivity = 1f;

    [Range(0f, 3f)]
    [Tooltip("Rotation sensitivity when placing block (0 = slow, 3 = fast).")]
    public float blockPlacementRotationSensitivity = 1f;

    [Header("UI")]
    public bool uiFollowCamera = false;
    public bool uiLightMode = false;

    [Header("Debug")]
    [Tooltip("Enable debug UI and tooling.")]
    public bool debugEnabled = false;

    [Header("SDF Visualization")]
    public bool sdfRenderFullSdfGrid = false;
    public bool sdfRenderSculptGuide = true;
    public bool sdfRenderWorkspaceBounds = true;


    [Tooltip("Optional prefab to load as a test model (can live in Resources).")]
    public GameObject testModelPrefab;

    [Tooltip("Resources path for test model prefab (e.g. \"SDF/TestModel\").")]
    public string testModelResourcePath = "SDF/TestModel";

    [Header("File Viewer")]
    [Tooltip("Folder path to browse for files. Set in Inspector or hardcode.")]
    public string folderViewerPath = "";

    [Header("Fiducial Calibration")]
    [Tooltip("Marker ID used to define the world origin.")]
    public int calibrationMarkerId = 0;

    [Tooltip("Position of the calibration origin in marker space.")]
    public Vector3 originOffsetPosition = Vector3.zero;

    [Tooltip("Rotation of the calibration origin in marker space.")]
    public Quaternion originOffsetRotation = Quaternion.identity;

    [Header("QR Workspace Corners")]
    [Tooltip("IDs of the 4 QR codes placed at workspace corners. Order: NearLeft, NearRight, FarRight, FarLeft (viewed from above). QR payload must be the integer ID as plain text, e.g. '10'.")]
    public int[] qrCornerMarkerIds = new int[] { 10, 11, 12, 13 };

    [Header("Model Fit")]
    [Tooltip("If true, automatically scale the model so its longest axis fills the workspace.")]
    public bool modelFitToWorkspace = false;
}
