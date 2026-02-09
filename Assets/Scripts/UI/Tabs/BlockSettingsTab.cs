using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UI.Elements.UIInputField;

public class BlockSettingsTab : MonoBehaviour
{
    public static GameObject Create(Transform parent, UIStyle style)
    {
        Color accentColor = style.accentColor;
        Color textColor = style.textColor;
        GameObject content = new GameObject("BlockSettingsContent");
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

        // --- Block dimensions section ---
        GameObject dimensionsHeader = UILayoutFactory.CreateLayoutSection(content.transform, "BlockDimensionsHeader", 90);
        UILayoutFactory.CreateHeader(dimensionsHeader, "Block dimensions", 90, accentColor, textColor, 15f, 2800f, 42f);

        // Row 1: Height and Width side by side
        GameObject row1 = UILayoutFactory.CreateHorizontalRow(content.transform, 220, 30, "BlockDimensions1");

        GameObject blockHeightInput = UILayoutFactory.CreateInputSection(row1.transform, "Block height", 220, 1300f);
        UIInputField blockHeightField = blockHeightInput.AddComponent<UIInputField>();
        blockHeightField.CreateInputField(
            "Block height", "Enter height", accentColor,
            InputType.DecimalNumber,
            (val) => { if (float.TryParse(val, out float f)) settings.stoneBlockDimensions.y = f; });

        GameObject blockWidthInput = UILayoutFactory.CreateInputSection(row1.transform, "Block width", 220, 1300f);
        UIInputField blockWidthField = blockWidthInput.AddComponent<UIInputField>();
        blockWidthField.CreateInputField(
            "Block width", "Enter width", accentColor,
            InputType.DecimalNumber,
            (val) => { if (float.TryParse(val, out float f)) settings.stoneBlockDimensions.x = f; });

        // Row 2: Length input + dropdown side by side
        GameObject row2 = UILayoutFactory.CreateHorizontalRow(content.transform, 220, 30, "BlockDimensions2");

        GameObject blockLengthInput = UILayoutFactory.CreateInputSection(row2.transform, "Block length", 220, 1300f);
        UIInputField blockLengthField = blockLengthInput.AddComponent<UIInputField>();
        blockLengthField.CreateInputField(
            "Block length", "Enter length", accentColor,
            InputType.DecimalNumber,
            (val) => { if (float.TryParse(val, out float f)) settings.stoneBlockDimensions.z = f; });

        List<string> units = new List<string> { "Meters", "Centimeters", "Inches" };
        UILayoutFactory.CreateDropdownElement(row2.transform, "Units", "Unit", units, accentColor, 220, 1300f);

        // --- Block placement section ---
        GameObject placementHeader = UILayoutFactory.CreateLayoutSection(content.transform, "BlockPlacementHeader", 90);
        UILayoutFactory.CreateHeader(placementHeader, "Block placement", 90, accentColor, textColor, 15f, 2800f, 42f);

        // Extra top padding above keybind (Block tab only)
        GameObject keybindSpacer = new GameObject("KeybindTopSpacer");
        keybindSpacer.transform.SetParent(content.transform, false);
        LayoutElement keybindSpacerLE = keybindSpacer.AddComponent<LayoutElement>();
        keybindSpacerLE.preferredHeight = 50;
        keybindSpacerLE.minHeight = 50;

        // Toggle block placement keybinding (calls KeyBindActions.ToggleBlockPlacement when pressed)
        if (style.keyBindActions != null)
        {
            UILayoutFactory.CreateKeyBindingElement(
                content.transform,
                "ToggleBlockPlacement",
                "Toggle block placement",
                OVRInput.Button.Two,
                style.keyBindActions.ToggleBlockPlacement,
                accentColor,
                style.textColor,
                100f,
                1700f);
        }

        // Extra top padding above slider (Block tab only)
        GameObject sliderSpacer = new GameObject("SliderTopSpacer");
        sliderSpacer.transform.SetParent(content.transform, false);
        LayoutElement sliderSpacerLE = sliderSpacer.AddComponent<LayoutElement>();
        sliderSpacerLE.preferredHeight = 50;
        sliderSpacerLE.minHeight = 50;

        // Block placement movement sensitivity (0-3 slider, markers 0, 0.5, 1...) - at bottom
        UILayoutFactory.CreateSliderElement(
            content.transform,
            "BlockPlacementSensitivity",
            "Block placement movement sensitivity",
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
            blockHeightField.SetText(settings.stoneBlockDimensions.y.ToString());
            blockWidthField.SetText(settings.stoneBlockDimensions.x.ToString());
            blockLengthField.SetText(settings.stoneBlockDimensions.z.ToString());
        }

        return content;
    }
}
