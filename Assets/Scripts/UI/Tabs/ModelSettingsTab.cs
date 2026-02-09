using UnityEngine;
using UnityEngine.UI;
using UI.Elements.UIInputField;

public class ModelSettingsTab : MonoBehaviour
{
    public static GameObject Create(Transform parent, UIStyle style)
    {
        Color accentColor = style.accentColor;
        Color textColor = style.textColor;
        GameObject content = new GameObject("ModelSettingsContent");
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

        // --- Model size section ---
        GameObject modelSizeHeader = UILayoutFactory.CreateLayoutSection(content.transform, "ModelSizeHeader", 90);
        UILayoutFactory.CreateHeader(modelSizeHeader, "Model size", 90, accentColor, textColor, 15f, 2800f, 42f);

        GameObject sizeRow1 = UILayoutFactory.CreateHorizontalRow(content.transform, 220, 30, "ModelSize1");
        GameObject sizeXInput = UILayoutFactory.CreateInputSection(sizeRow1.transform, "Size X", 220, 1300f);
        UIInputField sizeXField = sizeXInput.AddComponent<UIInputField>();
        sizeXField.CreateInputField("Size X", "Enter X", accentColor, InputType.DecimalNumber,
            (val) => { if (settings != null && float.TryParse(val, out float f)) settings.modelSize.x = f; });

        GameObject sizeYInput = UILayoutFactory.CreateInputSection(sizeRow1.transform, "Size Y", 220, 1300f);
        UIInputField sizeYField = sizeYInput.AddComponent<UIInputField>();
        sizeYField.CreateInputField("Size Y", "Enter Y", accentColor, InputType.DecimalNumber,
            (val) => { if (settings != null && float.TryParse(val, out float f)) settings.modelSize.y = f; });

        GameObject sizeRow2 = UILayoutFactory.CreateHorizontalRow(content.transform, 220, 30, "ModelSize2");
        GameObject sizeZInput = UILayoutFactory.CreateInputSection(sizeRow2.transform, "Size Z", 220, 1300f);
        UIInputField sizeZField = sizeZInput.AddComponent<UIInputField>();
        sizeZField.CreateInputField("Size Z", "Enter Z", accentColor, InputType.DecimalNumber,
            (val) => { if (settings != null && float.TryParse(val, out float f)) settings.modelSize.z = f; });

        // --- Model offset section ---
        GameObject offsetHeader = UILayoutFactory.CreateLayoutSection(content.transform, "ModelOffsetHeader", 90);
        UILayoutFactory.CreateHeader(offsetHeader, "Model offset", 90, accentColor, textColor, 15f, 2800f, 42f);

        GameObject offsetRow1 = UILayoutFactory.CreateHorizontalRow(content.transform, 220, 30, "ModelOffset1");
        GameObject offsetXInput = UILayoutFactory.CreateInputSection(offsetRow1.transform, "Offset X", 220, 1300f);
        UIInputField offsetXField = offsetXInput.AddComponent<UIInputField>();
        offsetXField.CreateInputField("Offset X", "Enter X", accentColor, InputType.DecimalNumber,
            (val) => { if (settings != null && float.TryParse(val, out float f)) settings.modelOffset.x = f; });

        GameObject offsetYInput = UILayoutFactory.CreateInputSection(offsetRow1.transform, "Offset Y", 220, 1300f);
        UIInputField offsetYField = offsetYInput.AddComponent<UIInputField>();
        offsetYField.CreateInputField("Offset Y", "Enter Y", accentColor, InputType.DecimalNumber,
            (val) => { if (settings != null && float.TryParse(val, out float f)) settings.modelOffset.y = f; });

        GameObject offsetRow2 = UILayoutFactory.CreateHorizontalRow(content.transform, 220, 30, "ModelOffset2");
        GameObject offsetZInput = UILayoutFactory.CreateInputSection(offsetRow2.transform, "Offset Z", 220, 1300f);
        UIInputField offsetZField = offsetZInput.AddComponent<UIInputField>();
        offsetZField.CreateInputField("Offset Z", "Enter Z", accentColor, InputType.DecimalNumber,
            (val) => { if (settings != null && float.TryParse(val, out float f)) settings.modelOffset.z = f; });

        // Set initial values from settings
        if (settings != null)
        {
            sizeXField.SetText(settings.modelSize.x.ToString());
            sizeYField.SetText(settings.modelSize.y.ToString());
            sizeZField.SetText(settings.modelSize.z.ToString());
            offsetXField.SetText(settings.modelOffset.x.ToString());
            offsetYField.SetText(settings.modelOffset.y.ToString());
            offsetZField.SetText(settings.modelOffset.z.ToString());
        }

        return content;
    }
}
