using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public Settings settings;


    public DepthProviderFactory.DeviceType DepthDevice => settings.depthDevice;
    public GameObject TargetPrefab => settings.targetSculpturePrefab;
    public Vector3 StoneDimensions => settings.stoneBlockDimensions;

}