using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BlockSettingsTab : MonoBehaviour
{
    public static GameObject Create(Transform parent, Color accentColor)
    {
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

        Settings settings = SettingsManager.Instance != null ? SettingsManager.Instance.settings : null;

        // Row 1: Height and Width side by side
        GameObject row1 = UISection.CreateHorizontalRow(content.transform, 220, 30, "BlockDimensions1");

        GameObject blockHeightInput = UISection.CreateInputSection(row1.transform, "Block height", 220, 1300f);
        UIInputField blockHeightField = blockHeightInput.AddComponent<UIInputField>();
        blockHeightField.CreateInputField("Block height", "Enter height", accentColor);

        GameObject blockWidthInput = UISection.CreateInputSection(row1.transform, "Block width", 220, 1300f);
        UIInputField blockWidthField = blockWidthInput.AddComponent<UIInputField>();
        blockWidthField.CreateInputField("Block width", "Enter width", accentColor, false);

        // Row 2: Length input + dropdown side by side
        GameObject row2 = UISection.CreateHorizontalRow(content.transform, 220, 30, "BlockDimensions2");

        GameObject blockLengthInput = UISection.CreateInputSection(row2.transform, "Block length", 220, 1300f);
        UIInputField blockLengthField = blockLengthInput.AddComponent<UIInputField>();
        blockLengthField.CreateInputField("Block length", "Enter length", accentColor, false);

        List<string> units = new List<string> { "Meters", "Centimeters", "Inches" };
        UISection.CreateDropdownElement(row2.transform, "Units", "Unit", units, accentColor, 220, 1300f);

        // Row 3: Checkbox
        bool autoScaleDefault = settings != null ? settings.autoScaleBlock : false;
        GameObject autoScaleGO = UISection.CreateCheckboxElement(content.transform, "AutoScale", "Auto-scale block", accentColor, autoScaleDefault, 120);

        // Wire UI to settings
        if (settings != null)
        {
            blockHeightField.SetText(settings.stoneBlockDimensions.y.ToString());
            blockWidthField.SetText(settings.stoneBlockDimensions.x.ToString());
            blockLengthField.SetText(settings.stoneBlockDimensions.z.ToString());

            blockHeightField.OnValueChanged((val) =>
            {
                if (float.TryParse(val, out float f))
                    settings.stoneBlockDimensions.y = f;
            });

            blockWidthField.OnValueChanged((val) =>
            {
                if (float.TryParse(val, out float f))
                    settings.stoneBlockDimensions.x = f;
            });

            blockLengthField.OnValueChanged((val) =>
            {
                if (float.TryParse(val, out float f))
                    settings.stoneBlockDimensions.z = f;
            });

            UICheckbox autoScaleCheckbox = autoScaleGO.GetComponent<UICheckbox>();
            if (autoScaleCheckbox != null)
            {
                autoScaleCheckbox.OnValueChanged((val) =>
                {
                    settings.autoScaleBlock = val;
                });
            }
        }

        return content;
    }
}
