using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UI.Elements.UIInputField;

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

        // Row 1: Height and Width side by side
        GameObject row1 = UILayoutFactory.CreateHorizontalRow(content.transform, 220, 30, "WorkspaceBounds1");

        GameObject workspaceHeightInput = UILayoutFactory.CreateInputSection(row1.transform, "Workspace height", 220, 1300f);
        UIInputField workspaceHeightField = workspaceHeightInput.AddComponent<UIInputField>();
        workspaceHeightField.CreateInputField(
            "Workspace height", "Enter height", accentColor,
            InputType.DecimalNumber,
            (val) => { if (float.TryParse(val, out float f)) settings.stoneBlockDimensions.y = f; });

        GameObject workspaceWidthInput = UILayoutFactory.CreateInputSection(row1.transform, "Workspace width", 220, 1300f);
        UIInputField workspaceWidthField = workspaceWidthInput.AddComponent<UIInputField>();
        workspaceWidthField.CreateInputField(
            "Workspace width", "Enter width", accentColor,
            InputType.DecimalNumber,
            (val) => { if (float.TryParse(val, out float f)) settings.stoneBlockDimensions.x = f; });

        // Row 2: Length input + dropdown side by side
        GameObject row2 = UILayoutFactory.CreateHorizontalRow(content.transform, 220, 30, "WorkspaceBounds2");

        GameObject workspaceLengthInput = UILayoutFactory.CreateInputSection(row2.transform, "Workspace length", 220, 1300f);
        UIInputField workspaceLengthField = workspaceLengthInput.AddComponent<UIInputField>();
        workspaceLengthField.CreateInputField(
            "Workspace length", "Enter length", accentColor,
            InputType.DecimalNumber,
            (val) => { if (float.TryParse(val, out float f)) settings.stoneBlockDimensions.z = f; });

        List<string> units = new List<string> { "Meters", "Centimeters", "Inches" };
        UILayoutFactory.CreateDropdownElement(row2.transform, "Units", "Unit", units, accentColor, 220, 1300f);

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

        // Set initial values from settings
        if (settings != null)
        {
            workspaceHeightField.SetText(settings.stoneBlockDimensions.y.ToString());
            workspaceWidthField.SetText(settings.stoneBlockDimensions.x.ToString());
            workspaceLengthField.SetText(settings.stoneBlockDimensions.z.ToString());
        }

        return content;
    }
}
