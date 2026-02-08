using UnityEngine;
using UnityEngine.UI;

public class UICustomizationTab : MonoBehaviour
{
    public static GameObject Create(Transform parent, Color accentColor)
    {
        GameObject content = new GameObject("UICustomizationContent");
        content.transform.SetParent(parent, false);

        RectTransform rect = content.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localPosition = Vector3.zero;
        rect.localScale = Vector3.one;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 35;
        layout.padding = new RectOffset(50, 50, 50, 50);

        Settings settings = SettingsManager.Instance != null ? SettingsManager.Instance.settings : null;

        Debug.Log("Settings: " + settings);
        // Row 1: Follow camera checkbox
        bool followDefault = settings != null ? settings.uiFollowCamera : false;
        GameObject followGO = UISection.CreateCheckboxElement(
            content.transform, "FollowCamera", "Follow camera",
            accentColor, followDefault, 120);

        // Row 2: Light mode checkbox
        bool lightDefault = settings != null ? settings.uiLightMode : false;
        GameObject lightGO = UISection.CreateCheckboxElement(
            content.transform, "LightMode", "Light mode",
            accentColor, lightDefault, 120);

        // Wire UI to settings
        if (settings != null)
        {
            UICheckbox followCheckbox = followGO.GetComponent<UICheckbox>();
            if (followCheckbox != null)
            {
                followCheckbox.OnValueChanged((val) =>
                {
                    Debug.Log("Value changed on follow.");
                    settings.uiFollowCamera = val;
                });
            }

            UICheckbox lightCheckbox = lightGO.GetComponent<UICheckbox>();
            if (lightCheckbox != null)
            {
                lightCheckbox.OnValueChanged((val) =>
                {
                    settings.uiLightMode = val;

                    // Rebuild the UI with new theme
                    UIManager manager = Object.FindFirstObjectByType<UIManager>();
                    if (manager != null)
                    {
                        manager.RebuildUI();
                    }
                });
            }
        }

        return content;
    }
}
