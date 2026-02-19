using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI.Elements.UICheckbox;

public class SculptTab : MonoBehaviour
{
    private static Settings _settings;
    private static SdfVisualizationConfig _sdfConfig;
    private static Settings ActiveSettings => _settings ??= Settings.GetActive();
    private static SdfVisualizationConfig ActiveSdfConfig => _sdfConfig ??= SdfVisualizationConfig.GetActive();

    public static GameObject Create(Transform parent, UIStyle style)
    {
        Color accentColor = style.accentColor;
        Color textColor = style.textColor;
        _settings = Settings.GetActive();
        _sdfConfig = SdfVisualizationConfig.GetActive();

        GameObject content = new GameObject("SculptContent");
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

        GameObject headerSection = UILayoutFactory.CreateLayoutSection(content.transform, "SculptHeader", 90);
        UILayoutFactory.CreateHeader(headerSection, "Sculpt", 90, accentColor, textColor, 15f, 2800f, 42f);

        CreateToggle(
            content.transform,
            "SdfHelperToggle",
            "Enable SDF helper",
            accentColor,
            textColor,
            () => ActiveSettings != null && ActiveSettings.sdfRenderSculptGuide,
            (isOn) =>
            {
                Debug.Log($"[SculptTab] SetSdfHelperEnabled -> {isOn}");
                if (ActiveSettings == null)
                {
                    Debug.LogWarning("[SculptTab] Settings asset not found. Add a Settings asset to use SDF helper.");
                    return;
                }

                ActiveSettings.sdfRenderSculptGuide = isOn;
            }
        );
        CreateToggle(
            content.transform,
            "SdfMatchOverlayToggle",
            "Enable SDF match overlay",
            accentColor,
            textColor,
            () => ActiveSdfConfig != null && ActiveSdfConfig.sdfMatchOverlayEnabled,
            (isOn) =>
            {
                ActiveSdfConfig.sdfMatchOverlayEnabled = isOn;
            }
        );
        CreateToggle(
            content.transform,
            "RayHelperToggle",
            "Enable ray helper",
            accentColor,
            textColor,
            () => ActiveSdfConfig != null && ActiveSdfConfig.depthErrorEnabled,
            (isOn) =>
            {
                ActiveSdfConfig.depthErrorEnabled = isOn;
            }
        );

        return content;
    }

    private static void CreateToggle(
        Transform parent,
        string name,
        string label,
        Color accentColor,
        Color textColor,
        System.Func<bool> isEnabled,
        System.Action<bool> setEnabled)
    {
        GameObject toggleGO = new GameObject(name);
        toggleGO.transform.SetParent(parent, false);

        var le = toggleGO.AddComponent<LayoutElement>();
        le.minWidth = 900f;
        le.preferredWidth = 900f;
        le.minHeight = 70f;
        le.preferredHeight = 70f;

        var checkbox = toggleGO.AddComponent<UICheckbox>();
        checkbox.CreateCheckbox(
            label,
            accentColor,
            isEnabled(),
            (isOn) => setEnabled(isOn),
            textColor
        );
    }

    
}
