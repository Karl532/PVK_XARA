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

    [Header("Block Options")]
    public bool autoScaleBlock = false;
    public bool blockPlacementEnabled = true;

    [Header("UI")]
    public bool uiFollowCamera = false;
    public bool uiLightMode = false;

    [Header("File Viewer")]
    [Tooltip("Folder path to browse for files. Set in Inspector or hardcode.")]
    public string folderViewerPath = "";
}
