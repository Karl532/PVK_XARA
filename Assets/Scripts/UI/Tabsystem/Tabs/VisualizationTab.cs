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

        GameObject wireframeColorSpacer = new GameObject("WireframeColorSpacer");
        wireframeColorSpacer.transform.SetParent(content.transform, false);
        LayoutElement wireframeColorSpacerLE = wireframeColorSpacer.AddComponent<LayoutElement>();
        wireframeColorSpacerLE.preferredHeight = 60;
        wireframeColorSpacerLE.minHeight = 60;


        float WireframeColorRowHeight = 120f;
        float WireframeColorSpacing = 30f;
        float WireframeColorWidth = 450f;
        float WireframeColorMinWidth = 1000f;

        GameObject checkboxRow = UILayoutFactory.CreateHorizontalRow(content.transform, WireframeColorRowHeight, WireframeColorSpacing, "Checkboxes");
        ToggleGroup colorToggleGroup = checkboxRow.AddComponent<ToggleGroup>(); //add ToggleGrroup to checkbox row to force only one active
        colorToggleGroup.allowSwitchOff = false; //force 1 to be active (not 0)
        Color currentColor = _settings != null ? _settings.wireframeColor : new Color(1f, 1f, 1f, 1f);

        LayoutElement rowLayout = checkboxRow.GetComponent<LayoutElement>();
        if (rowLayout != null)
        {
            rowLayout.minWidth = WireframeColorWidth;
            rowLayout.flexibleWidth = 1;
        }

        HorizontalLayoutGroup rowHg = checkboxRow.GetComponent<HorizontalLayoutGroup>();
        if (rowHg != null)
        {
            rowHg.padding = new RectOffset(0, 0, 0, 0);
            rowHg.childForceExpandWidth = true;
        }


        //white
        Color color1TrueColor = new Color(1f, 1f, 1f, 1f);
        Color color1ColorOff = color1TrueColor*0.85f;
        Color color1ColorOn = new Color(color1ColorOff.r * 0.5f, color1ColorOff.g * 0.5f, color1ColorOff.b * 0.5f, 1f);

        GameObject color1 = new GameObject("color1");
        color1.transform.SetParent(checkboxRow.transform, false);
        AddCheckboxLayout(color1, WireframeColorMinWidth);
        UICheckbox color1Checkbox = color1.AddComponent<UICheckbox>();
        color1Checkbox.CreateCheckbox(
            "",
            new Color(0,0,0),
            currentColor == color1TrueColor,
            val => { if (_settings != null && val) _settings.wireframeColor = color1TrueColor; },
            null,
            UICheckboxStyling.DefaultFontSize,
            UICheckboxStyling.DefaultCheckboxSize*3,
            color1ColorOff,
            color1ColorOn,
            colorToggleGroup
        );

        //black
        Color color2TrueColor = new Color(0f, 0f, 0f, 1f);
        Color color2ColorOff = color2TrueColor * 0.85f;
        Color color2ColorOn = new Color(color1ColorOff.r * 0.1f, color1ColorOff.g * 0.1f, color1ColorOff.b * 0.1f, 1f);

        GameObject color2 = new GameObject("color2");
        color2.transform.SetParent(checkboxRow.transform, false);
        AddCheckboxLayout(color2, WireframeColorMinWidth);
        UICheckbox color2Checkbox = color2.AddComponent<UICheckbox>();
        color2Checkbox.CreateCheckbox(
            "",
            new Color(0, 0, 0),
            currentColor == color2TrueColor,
            val => { if (_settings != null && val) _settings.wireframeColor = color2TrueColor; },
            null,
            UICheckboxStyling.DefaultFontSize,
            UICheckboxStyling.DefaultCheckboxSize * 3,
            color2ColorOff,
            color2ColorOn,
            colorToggleGroup
        );

        //red
        Color color3TrueColor = new Color(1f, 0f, 0f, 1f);
        Color color3ColorOff = color3TrueColor*0.85f;
        Color color3ColorOn = new Color(color3ColorOff.r * 0.5f, color3ColorOff.g * 0.5f, color3ColorOff.b * 0.5f, 1f);

        GameObject color3 = new GameObject("color3");
        color3.transform.SetParent(checkboxRow.transform, false);
        AddCheckboxLayout(color3, WireframeColorMinWidth);
        UICheckbox color3Checkbox = color3.AddComponent<UICheckbox>();
        color3Checkbox.CreateCheckbox(
            "",
            new Color(0, 0, 0),
            currentColor == color3TrueColor,
            val => { if (_settings != null && val) _settings.wireframeColor = color3TrueColor; },
            null,
            UICheckboxStyling.DefaultFontSize,
            UICheckboxStyling.DefaultCheckboxSize * 3,
            color3ColorOff,
            color3ColorOn,
            colorToggleGroup
        );

        //green
        Color color4TrueColor = new Color(0f, 1f, 0f, 1f);
        Color color4ColorOff = color4TrueColor * 0.85f;
        Color color4ColorOn = new Color(color4ColorOff.r * 0.5f, color4ColorOff.g * 0.5f, color4ColorOff.b * 0.5f, 1f);

        GameObject color4 = new GameObject("color4");
        color4.transform.SetParent(checkboxRow.transform, false);
        AddCheckboxLayout(color4, WireframeColorMinWidth);
        UICheckbox color4Checkbox = color4.AddComponent<UICheckbox>();
        color4Checkbox.CreateCheckbox(
            "",
            new Color(0, 0, 0),
            currentColor == color4TrueColor,
            val => { if (_settings != null && val) _settings.wireframeColor = color4TrueColor; },
            null,
            UICheckboxStyling.DefaultFontSize,
            UICheckboxStyling.DefaultCheckboxSize * 3,
            color4ColorOff,
            color4ColorOn,
            colorToggleGroup
        );

        //blue
        Color color5TrueColor = new Color(0f, 0f, 1f, 1f);
        Color color5ColorOff = color5TrueColor * 0.85f;
        Color color5ColorOn = new Color(color5ColorOff.r * 0.5f, color5ColorOff.g * 0.5f, color5ColorOff.b * 0.5f, 1f);

        GameObject color5 = new GameObject("color5");
        color5.transform.SetParent(checkboxRow.transform, false);
        AddCheckboxLayout(color5, WireframeColorMinWidth);
        UICheckbox color5Checkbox = color5.AddComponent<UICheckbox>();
        color5Checkbox.CreateCheckbox(
            "",
            new Color(0, 0, 0),
            currentColor == color5TrueColor,
            val => { if (_settings != null && val) _settings.wireframeColor = color5TrueColor; },
            null,
            UICheckboxStyling.DefaultFontSize,
            UICheckboxStyling.DefaultCheckboxSize * 3,
            color5ColorOff,
            color5ColorOn,
            colorToggleGroup
        );

        //yellow
        Color color6TrueColor = new Color(1f, 1f, 0f, 1f);
        Color color6ColorOff = color6TrueColor * 0.85f;
        Color color6ColorOn = new Color(color6ColorOff.r * 0.5f, color6ColorOff.g * 0.5f, color6ColorOff.b * 0.5f, 1f);

        GameObject color6 = new GameObject("color6");
        color6.transform.SetParent(checkboxRow.transform, false);
        AddCheckboxLayout(color6, WireframeColorMinWidth);
        UICheckbox color6Checkbox = color6.AddComponent<UICheckbox>();
        color6Checkbox.CreateCheckbox(
            "",
            new Color(0, 0, 0),
            currentColor == color6TrueColor,
            val => { if (_settings != null && val) _settings.wireframeColor = color6TrueColor; },
            null,
            UICheckboxStyling.DefaultFontSize,
            UICheckboxStyling.DefaultCheckboxSize * 3,
            color6ColorOff,
            color6ColorOn,
            colorToggleGroup
        );











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


    static void AddCheckboxLayout(GameObject go, float minWidth)
    {
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minWidth = minWidth;
        le.preferredWidth = minWidth;
        le.flexibleWidth = 1;
    }

}