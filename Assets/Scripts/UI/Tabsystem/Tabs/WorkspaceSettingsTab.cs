using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WorkspaceSettingsTab : MonoBehaviour
{
    public static GameObject Create(Transform parent, UIStyle style)
    {
        Color accentColor = style.accentColor;
        Color textColor = style.textColor;
        GameObject content = new GameObject("WorkspaceSettingsContent");
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
        layout.childAlignment = TextAnchor.UpperLeft;

        Settings settings = SettingsManager.Instance != null ? SettingsManager.Instance.settings : null;

        // --- Workspace bounds section ---
        GameObject dimensionsHeader = UILayoutFactory.CreateLayoutSection(content.transform, "WorkspaceBoundsHeader", 90);
        UILayoutFactory.CreateHeader(dimensionsHeader, "Workspace bounds", 90, accentColor, textColor, 15f, 2800f, 42f);

        // Uniform workspace size slider
        float initialSize = settings != null ? settings.stoneBlockDimensions.x : 1f;
        if (settings != null)
        {
            // Use the average in case the values differ.
            initialSize = (settings.stoneBlockDimensions.x + settings.stoneBlockDimensions.y + settings.stoneBlockDimensions.z) / 3f;
        }

        UILayoutFactory.CreateSliderElement(
            content.transform,
            "WorkspaceSize",
            "Workspace size",
            0.1f, 5f,
            initialSize,
            (val) =>
            {
                if (settings != null)
                    settings.stoneBlockDimensions = new Vector3(val, val, val);
            },
            accentColor,
            textColor,
            120f,
            -1f,
            0.1f);

        GameObject slider1Spacer = new GameObject("SliderTopSpacer");
        slider1Spacer.transform.SetParent(content.transform, false);
        LayoutElement slider1SpacerLE = slider1Spacer.AddComponent<LayoutElement>();
        slider1SpacerLE.preferredHeight = 70;
        slider1SpacerLE.minHeight = 70;

        List<string> units = new List<string> { "Meters", "Centimeters", "Inches" };
        UILayoutFactory.CreateDropdownElement(content.transform, "Units", "Unit", units, accentColor, 220, 1300f);

        // Padding above workspace placement section
        GameObject placementSpacer = new GameObject("PlacementSectionSpacer");
        placementSpacer.transform.SetParent(content.transform, false);
        LayoutElement placementSpacerLE = placementSpacer.AddComponent<LayoutElement>();
        placementSpacerLE.preferredHeight = 45;
        placementSpacerLE.minHeight = 45;

        // --- Workspace placement section ---
        GameObject placementHeader = UILayoutFactory.CreateLayoutSection(content.transform, "WorkspacePlacementHeader", 90);
        UILayoutFactory.CreateHeader(placementHeader, "Workspace placement", 90, accentColor, textColor, 15f, 2800f, 42f);

        // Extra top padding above keybind (Workspace tab only)
        GameObject keybindSpacer = new GameObject("KeybindTopSpacer");
        keybindSpacer.transform.SetParent(content.transform, false);
        LayoutElement keybindSpacerLE = keybindSpacer.AddComponent<LayoutElement>();
        keybindSpacerLE.preferredHeight = 50;
        keybindSpacerLE.minHeight = 50;

        // Toggle workspace placement keybinding (calls KeyBindActions.ToggleBlockPlacement when pressed)
        if (style.keyBindActions != null)
        {
            UILayoutFactory.CreateKeyBindingElement(
                content.transform,
                "ToggleWorkspacePlacement",
                "Toggle workspace placement",
                OVRInput.Button.Two,
                style.keyBindActions.ToggleBlockPlacement,
                accentColor,
                style.textColor,
                100f,
                1700f);
        }

        // Extra top padding above slider (Workspace tab only)
        GameObject sliderSpacer = new GameObject("SliderTopSpacer");
        sliderSpacer.transform.SetParent(content.transform, false);
        LayoutElement sliderSpacerLE = sliderSpacer.AddComponent<LayoutElement>();
        sliderSpacerLE.preferredHeight = 50;
        sliderSpacerLE.minHeight = 50;

        // Workspace placement movement sensitivity (0-3 slider, markers 0, 0.5, 1...) - at bottom
        UILayoutFactory.CreateSliderElement(
            content.transform,
            "WorkspacePlacementSensitivity",
            "Workspace placement movement sensitivity",
            0f, 3f,
            settings != null ? settings.blockPlacementMovementSensitivity : 1f,
            (val) => { if (settings != null) settings.blockPlacementMovementSensitivity = val; },
            accentColor,
            style.textColor,
            120f,
            -1f,
            0.5f);

        GameObject slider2Spacer = new GameObject("SliderTopSpacer");
        slider2Spacer.transform.SetParent(content.transform, false);
        LayoutElement slider2SpacerLE = slider2Spacer.AddComponent<LayoutElement>();
        slider2SpacerLE.preferredHeight = 70;
        slider2SpacerLE.minHeight = 70;

        // Workspace placement rotation sensitivity (0-3 slider)
        UILayoutFactory.CreateSliderElement(
            content.transform,
            "WorkspacePlacementRotationSensitivity",
            "Workspace placement rotation sensitivity",
            0f, 3f,
            settings != null ? settings.blockPlacementRotationSensitivity : 1f,
            (val) => { if (settings != null) settings.blockPlacementRotationSensitivity = val; },
            accentColor,
            style.textColor,
            120f,
            -1f,
            0.5f);


        GameObject slider3Spacer = new GameObject("SliderTopSpacer");
        slider3Spacer.transform.SetParent(content.transform, false);
        LayoutElement slider3SpacerLE = slider3Spacer.AddComponent<LayoutElement>();
        slider3SpacerLE.preferredHeight = 70;
        slider3SpacerLE.minHeight = 70;


        return content;
    }
}
