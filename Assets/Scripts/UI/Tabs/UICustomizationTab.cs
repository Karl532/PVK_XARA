using UnityEngine;
using UnityEngine.UI;
using UI.Elements.UICheckbox;

public class UICustomizationTab : MonoBehaviour
{
    private const float CheckboxRowHeight = 120f;
    private const float CheckboxSpacing = 50f;
    private const float CheckboxMinWidth = 450f;
    private const float CheckboxRowMinWidth = 1000f;

    public static GameObject Create(Transform parent, UIStyle style)
    {
        Color accentColor = style.accentColor;
        Color labelColor = style.textColor;
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

        // Horizontal row for checkboxes
        GameObject checkboxRow = UILayoutFactory.CreateHorizontalRow(content.transform, CheckboxRowHeight, CheckboxSpacing, "Checkboxes");
        LayoutElement rowLayout = checkboxRow.GetComponent<LayoutElement>();
        if (rowLayout != null)
        {
            rowLayout.minWidth = CheckboxRowMinWidth;
            rowLayout.flexibleWidth = 1;
        }

        HorizontalLayoutGroup rowHg = checkboxRow.GetComponent<HorizontalLayoutGroup>();
        if (rowHg != null)
        {
            rowHg.padding = new RectOffset(20, 0, 20, 0);
            rowHg.childForceExpandWidth = true;
        }

        Color? boxBgOff = null, boxBgOn = null;
        if (settings?.uiLightMode == true)
        {
            boxBgOff = new Color(0.88f, 0.88f, 0.91f, 0.95f);
            boxBgOn = new Color(accentColor.r * 0.5f, accentColor.g * 0.5f, accentColor.b * 0.5f, 0.95f);
        }

        // Follow camera checkbox
        GameObject followGO = new GameObject("FollowCamera");
        followGO.transform.SetParent(checkboxRow.transform, false);
        AddCheckboxLayout(followGO, CheckboxMinWidth);
        UICheckbox followCheckbox = followGO.AddComponent<UICheckbox>();
        followCheckbox.CreateCheckbox(
            "Follow camera",
            accentColor,
            settings?.uiFollowCamera ?? false,
            val => { if (settings != null) settings.uiFollowCamera = val; },
            labelColor,
            UICheckboxStyling.DefaultFontSize,
            UICheckboxStyling.DefaultCheckboxSize,
            boxBgOff,
            boxBgOn
        );

        // Light mode checkbox
        GameObject lightGO = new GameObject("LightMode");
        lightGO.transform.SetParent(checkboxRow.transform, false);
        AddCheckboxLayout(lightGO, CheckboxMinWidth);
        UICheckbox lightCheckbox = lightGO.AddComponent<UICheckbox>();
        lightCheckbox.CreateCheckbox(
            "Light mode",
            accentColor,
            settings?.uiLightMode ?? false,
            val =>
            {
                if (settings != null) settings.uiLightMode = val;
                UIManager manager = Object.FindFirstObjectByType<UIManager>();
                manager?.RebuildUI();
            },
            labelColor,
            UICheckboxStyling.DefaultFontSize,
            UICheckboxStyling.DefaultCheckboxSize,
            boxBgOff,
            boxBgOn
        );

        return content;
    }

    static void AddCheckboxLayout(GameObject go, float minWidth)
    {
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minWidth = minWidth;
        le.preferredWidth = minWidth;
        le.flexibleWidth = 1;
    }
}
