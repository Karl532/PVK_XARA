using System.IO;
using UnityEngine;

/// <summary>
/// Saves and loads runtime-modifiable settings to persistent storage.
/// - Saves when the settings panel is closed (via ToggleSettingsPanelHandler).
/// - Optionally autosaves on a fixed interval, configured via inspector fields.
/// </summary>
public class SettingsPersistence : MonoBehaviour
{
    public static SettingsPersistence Instance { get; private set; }

    [Header("Autosave")]
    [Tooltip("If true, settings will be saved automatically at a fixed interval.")]
    [SerializeField] private bool autoSaveEnabled = true;

    [Tooltip("Autosave interval in seconds.")]
    [Min(1f)]
    [SerializeField] private float autoSaveIntervalSeconds = 10f;

    private Settings Settings => SettingsManager.Instance != null ? SettingsManager.Instance.settings : null;

    private const string FileName = "settings.json";

    private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();

        if (autoSaveEnabled)
        {
            InvokeRepeating(nameof(SaveSettings), autoSaveIntervalSeconds, autoSaveIntervalSeconds);
        }
    }

    [System.Serializable]
    private class RuntimeSettingsData
    {
        public Vector3 stoneBlockDimensions;
        public bool autoScaleBlock;
        public bool blockPlacementEnabled;
        public float blockPlacementMovementSensitivity;

        public Vector3 modelSize;
        public Vector3 modelOffset;

        public bool uiFollowCamera;
        public bool uiLightMode;

        public string folderViewerPath;

        public int calibrationMarkerId;
        public Vector3 originOffsetPosition;
        public Quaternion originOffsetRotation;
    }

    public void SaveSettings()
    {
        if (Settings == null)
            return;

        var data = new RuntimeSettingsData
        {
            stoneBlockDimensions = Settings.stoneBlockDimensions,
            autoScaleBlock = Settings.autoScaleBlock,
            blockPlacementEnabled = Settings.blockPlacementEnabled,
            blockPlacementMovementSensitivity = Settings.blockPlacementMovementSensitivity,

            modelSize = Settings.modelSize,
            modelOffset = Settings.modelOffset,

            uiFollowCamera = Settings.uiFollowCamera,
            uiLightMode = Settings.uiLightMode,

            folderViewerPath = Settings.folderViewerPath,

            calibrationMarkerId = Settings.calibrationMarkerId,
            originOffsetPosition = Settings.originOffsetPosition,
            originOffsetRotation = Settings.originOffsetRotation
        };

        var json = JsonUtility.ToJson(data);
        try
        {
            File.WriteAllText(FilePath, json);
        }
        catch (IOException e)
        {
            Debug.LogError($"[SettingsPersistence] Failed to write settings file: {e}");
        }
    }

    public void LoadSettings()
    {
        if (Settings == null)
            return;

        if (!File.Exists(FilePath))
            return;

        try
        {
            var json = File.ReadAllText(FilePath);
            var data = JsonUtility.FromJson<RuntimeSettingsData>(json);
            if (data == null)
                return;

            Settings.stoneBlockDimensions = data.stoneBlockDimensions;
            Settings.autoScaleBlock = data.autoScaleBlock;
            Settings.blockPlacementEnabled = data.blockPlacementEnabled;
            Settings.blockPlacementMovementSensitivity = data.blockPlacementMovementSensitivity;

            Settings.modelSize = data.modelSize;
            Settings.modelOffset = data.modelOffset;

            Settings.uiFollowCamera = data.uiFollowCamera;
            Settings.uiLightMode = data.uiLightMode;

            Settings.folderViewerPath = data.folderViewerPath;

            Settings.calibrationMarkerId = data.calibrationMarkerId;
            Settings.originOffsetPosition = data.originOffsetPosition;
            Settings.originOffsetRotation = data.originOffsetRotation;
        }
        catch (IOException e)
        {
            Debug.LogError($"[SettingsPersistence] Failed to read settings file: {e}");
        }
    }
}

