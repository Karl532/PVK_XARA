using UnityEngine;
using UnityEngine.UI;

public class DepthDeviceDropdownUI : MonoBehaviour
{
    public SettingsManager settingsManager;
    public Dropdown dropdown;

    void Start()
    {
        dropdown.value = (int)settingsManager.settings.depthDevice;

        dropdown.onValueChanged.AddListener(OnDropdownChanged);
    }

    void OnDropdownChanged(int value)
    {
        settingsManager.settings.depthDevice = (DepthProviderFactory.DeviceType)value;
    }
}