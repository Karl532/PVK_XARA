using UI.Elements.UICheckbox;
using UnityEngine;
using UnityEngine.UI;

public class VisualizationTab : MonoBehaviour
{
    private static Settings _settings;
    private static Settings ActiveSettings => _settings ??= Settings.GetActive();

    public static GameObject Create(Transform parent, UIStyle style)
    {
        //the general settings
        Color accentColor = style.accentColor;
        Color textColor = style.textColor;
        _settings = Settings.GetActive();

        GameObject content = new GameObject("VisualizationContent");
        content.transform.SetParent(parent, false);

        RectTransform rect = content.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 24;
        layout.padding = new RectOffset(40, 40, 40, 40);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        //header
        GameObject headerSection = UILayoutFactory.CreateLayoutSection(content.transform, "VisualizationHeader", 90);
        UILayoutFactory.CreateHeader(headerSection, "Visualization", 90, accentColor, textColor, 15f, 2800f, 42f);

        //Wireframe opacity
        //< slider 0 - 100 >
        GameObject opacitySpacer = new GameObject("WireframeOpacitySpacer");
        opacitySpacer.transform.SetParent(content.transform, false);
        LayoutElement opacitySpacerLE = opacitySpacer.AddComponent<LayoutElement>();
        opacitySpacerLE.preferredHeight = 40;
        opacitySpacerLE.minHeight = 40;

        float initialOpacity = (_settings != null && _settings.wireframeOpacity > 0f) ? _settings.wireframeOpacity : 30f;

        UILayoutFactory.CreateSliderElement(
            content.transform,
            "WireframeOpacity",
            "Wireframe Opacity",
            0.0f,
            100f,
            initialOpacity,
            (val) =>
            {
                if (_settings != null)
                {
                    _settings.wireframeOpacity = val;
                }
            },
            accentColor,
            textColor,
            120f,
            -1f,
            10f);

        GameObject opacitySpacer2 = new GameObject("WireframeOpacitySpacer");
        opacitySpacer2.transform.SetParent(content.transform, false);
        LayoutElement opacitySpacerLE2 = opacitySpacer2.AddComponent<LayoutElement>();
        opacitySpacerLE2.preferredHeight = 40;
        opacitySpacerLE2.minHeight = 40;

        //Wireframe color
        //< color picker >


        //Wireframe Thickness
        //< slider 0.01 - 2 >
        GameObject thicknessSpacer = new GameObject("WireframeOpacitySpacer");
        thicknessSpacer.transform.SetParent(content.transform, false);
        LayoutElement thicknessSpacerLE = thicknessSpacer.AddComponent<LayoutElement>();
        thicknessSpacerLE.preferredHeight = 40;
        thicknessSpacerLE.minHeight = 40;

        float initialThickness = (_settings != null && _settings.wireframeThickness > 0f) ? _settings.wireframeThickness : 1f;

        UILayoutFactory.CreateSliderElement(
            content.transform,
            "WireframeThickness",
            "Wireframe Thickness",
            0.01f,
            2f,
            initialThickness,
            (val) =>
            {
                if (_settings != null)
                {
                    _settings.wireframeThickness = val;
                }
            },
            accentColor,
            textColor,
            120f,
            -1f,
            0.5f);


        return content;
    }


}