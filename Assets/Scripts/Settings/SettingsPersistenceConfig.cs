using UnityEngine;

[CreateAssetMenu(fileName = "SettingsPersistenceConfig", menuName = "Scriptable Objects/Settings Persistence Config")]
public class SettingsPersistenceConfig : ScriptableObject
{
    [Header("Autosave")]
    [Tooltip("If true, settings will be saved automatically at a fixed interval.")]
    public bool autoSaveEnabled = true;

    [Tooltip("Autosave interval in seconds.")]
    [Min(1f)]
    public float autoSaveIntervalSeconds = 10f;
}

