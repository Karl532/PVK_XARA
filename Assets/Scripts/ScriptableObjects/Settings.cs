using UnityEngine;

[CreateAssetMenu(fileName = "Settings", menuName = "Scriptable Objects/Settings")]
public class Settings : ScriptableObject
{
    [Header("Depth Device")]
    public DepthProviderFactory.DeviceType depthDevice;

    [Header("Target Sculpture")]
    public GameObject targetSculpturePrefab;

    [Header("Stone Block Dimensions")]
    public Vector3 stoneBlockDimensions = new Vector3(1f, 1f, 1f);

    [Header("Model")]
    public Vector3 modelSize = new Vector3(1f, 1f, 1f);
    public Vector3 modelOffset = Vector3.zero;

    [Header("Block Options")]
    public bool autoScaleBlock = false;
    public bool blockPlacementEnabled = true;

    [Header("Block Placement")]
    [Range(0f, 3f)]
    [Tooltip("Movement sensitivity when placing block (0 = slow, 3 = fast).")]
    public float blockPlacementMovementSensitivity = 1f;

    [Header("UI")]
    public bool uiFollowCamera = false;
    public bool uiLightMode = false;

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
}
