using UnityEngine;
using UnityEngine.UI;
using UI.Elements.UICheckbox;

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

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 35;
        layout.padding = new RectOffset(50, 50, 50, 50);

        Settings settings = SettingsManager.Instance?.settings;

        // Follow camera checkbox
        GameObject followGO = new GameObject("FollowCamera");
        followGO.transform.SetParent(content.transform, false);

        UICheckbox followCheckbox = followGO.AddComponent<UICheckbox>();
        followCheckbox.CreateCheckbox(
            "Follow camera",
            accentColor,
            settings?.uiFollowCamera ?? false,
            val =>
            {
                Debug.Log("Follow camera changed");
                settings.uiFollowCamera = val;
            }
        );

        // Light mode checkbox
        GameObject lightGO = new GameObject("LightMode");
        lightGO.transform.SetParent(content.transform, false);

        UICheckbox lightCheckbox = lightGO.AddComponent<UICheckbox>();
        lightCheckbox.CreateCheckbox(
            "Light mode",
            accentColor,
            settings?.uiLightMode ?? false,
            val =>
            {
                settings.uiLightMode = val;

                UIManager manager = Object.FindFirstObjectByType<UIManager>();
                manager?.RebuildUI();
            }
        );

        return content;
    }
}
