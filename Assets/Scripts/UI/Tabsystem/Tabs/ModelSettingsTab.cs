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

        // --- Model section ---
        GameObject modelHeader = UILayoutFactory.CreateLayoutSection(content.transform, "ModelHeader", 90);
        UILayoutFactory.CreateHeader(modelHeader, "Model", 90, accentColor, textColor, 15f, 2800f, 42f);

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

        // --- Uniform model scale slider ---
        GameObject scaleSpacer = new GameObject("ModelScaleSpacer");
        scaleSpacer.transform.SetParent(content.transform, false);
        LayoutElement scaleSpacerLE = scaleSpacer.AddComponent<LayoutElement>();
        scaleSpacerLE.preferredHeight = 40;
        scaleSpacerLE.minHeight = 40;

        float initialScale = (settings != null && settings.modelScale > 0f) ? settings.modelScale : 1f;

        UILayoutFactory.CreateSliderElement(
            content.transform,
            "ModelUniformScale",
            "Model scale",
            0.1f,
            5f,
            initialScale,
            (val) =>
            {
                if (settings != null)
                {
                    settings.modelScale = val;
                }
            },
            accentColor,
            textColor,
            120f,
            -1f,
            0.1f);

        // Set initial values from settings
        if (settings != null)
        {
            offsetXField.SetText(settings.modelOffset.x.ToString());
            offsetYField.SetText(settings.modelOffset.y.ToString());
            offsetZField.SetText(settings.modelOffset.z.ToString());
        }

        return content;
    }
}
