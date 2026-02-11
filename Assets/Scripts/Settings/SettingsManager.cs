using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public Settings settings;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public DepthProviderFactory.DeviceType DepthDevice => settings.depthDevice;
    public GameObject TargetPrefab => settings.targetSculpturePrefab;
    public Vector3 StoneDimensions => settings.stoneBlockDimensions;
}
