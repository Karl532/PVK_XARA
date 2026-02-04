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

}
