using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Scripts.Depth.Diagnostics;
using UI.Utils;

public class DebugTab : MonoBehaviour
{
    private static Settings _settings;
    private static SdfVisualizationConfig _sdfConfig;
    private static Settings ActiveSettings => _settings ??= Settings.GetActive();
    private static SdfVisualizationConfig ActiveSdfConfig
    {
        get
        {
            var settings = ActiveSettings;
            if (settings != null && settings.sdfVisualizationConfig != null)
                return settings.sdfVisualizationConfig;
            return _sdfConfig ??= SdfVisualizationConfig.GetActive();
        }
    }

    public static GameObject Create(Transform parent, UIStyle style)
    {
        Color accentColor = style.accentColor;
        Color textColor = style.textColor;
        _settings = Settings.GetActive();
        _sdfConfig = ActiveSettings != null ? ActiveSettings.sdfVisualizationConfig : SdfVisualizationConfig.GetActive();

        GameObject content = new GameObject("DebugContent");
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

        GameObject headerSection = UILayoutFactory.CreateLayoutSection(content.transform, "DebugHeader", 90);
        UILayoutFactory.CreateHeader(headerSection, "Debug", 90, accentColor, textColor, 15f, 2800f, 42f);

        CreateLoadTestModelButton(content.transform, accentColor, textColor, style.cornerRadius);
        UILayoutFactory.CreateSpacer(content.transform, 10f);

        CreateTopRow(content.transform, accentColor, textColor);
        CreateSdfSculptGuideSection(content.transform, accentColor, textColor);

        return content;
    }

    private static void CreateLoadTestModelButton(Transform parent, Color accentColor, Color textColor, float cornerRadius)
    {
        GameObject buttonGO = new GameObject("LoadTestModelButton");
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(0, 0.5f);
        rect.pivot = new Vector2(0, 0.5f);
        rect.sizeDelta = new Vector2(900f, 140f);
        rect.localScale = Vector3.one;

        LayoutElement le = buttonGO.AddComponent<LayoutElement>();
        le.minWidth = 900f;
        le.preferredWidth = 900f;
        le.minHeight = 140f;
        le.preferredHeight = 140f;

        Image bg = buttonGO.AddComponent<Image>();
        bg.color = accentColor;

        RoundedImage rounded = buttonGO.AddComponent<RoundedImage>();
        rounded.SetRadius(cornerRadius);

        Button button = buttonGO.AddComponent<Button>();

        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(buttonGO.transform, false);

        RectTransform labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = "Load test model";
        label.alignment = TextAlignmentOptions.Center;
        label.color = textColor;
        label.fontSize = 42f;
        label.enableWordWrapping = false;

        button.onClick.AddListener(() =>
        {
            if (RuntimeModelLoader.Instance != null)
            {
                RuntimeModelLoader.Instance.LoadTestModel(ActiveSettings);
            }
            else
            {
                Debug.LogError("[DebugTab] RuntimeModelLoader.Instance is null. Add RuntimeModelLoader to the scene.");
            }
        });
    }

    private static void CreateTopRow(Transform parent, Color accentColor, Color textColor)
    {
        Transform toggleRow = UILayoutFactory.CreateFixedRow(parent, "DebugToggleRow", 900f, 80f).transform;
        UILayoutFactory.CreateCheckboxToggle(
            toggleRow,
            "DepthOverlayToggle",
            "Toggle depth overlay",
            accentColor,
            textColor,
            () => DepthOverlayController.IsEnabled,
            (enabled) =>
            {
                if (enabled)
                    DepthOverlayController.Enable();
                else
                    DepthOverlayController.Disable();
            },
            420f
        );
        UILayoutFactory.CreateRowSpacer(toggleRow);
        UILayoutFactory.CreateCheckboxToggle(
            toggleRow,
            "SdfFullOverlayToggle",
            "SDF debug overlay (grid)",
            accentColor,
            textColor,
            () => ActiveSettings != null && ActiveSettings.sdfRenderFullSdfGrid,
            (enabled) =>
            {
                Debug.Log($"[DebugTab] SetSdfFullOverlayEnabled -> {enabled}");
                if (ActiveSettings == null)
                {
                    Debug.LogWarning("[DebugTab] Settings asset not found. Add a Settings asset to use SDF overlay.");
                    return;
                }
                ActiveSettings.sdfRenderFullSdfGrid = enabled;
            },
            420f
        );
    }

    private static void CreateSdfSculptGuideSection(Transform parent, Color accentColor, Color textColor)
    {
        UILayoutFactory.CreateCheckboxToggle(
            parent,
            "SdfSculptGuideToggle",
            "SDF sculpt guide",
            accentColor,
            textColor,
            () => ActiveSettings != null && ActiveSettings.sdfRenderSculptGuide,
            (enabled) =>
            {
                Debug.Log($"[DebugTab] SetSdfSculptGuideEnabled -> {enabled}");
                if (ActiveSettings == null)
                {
                    Debug.LogWarning("[DebugTab] Settings asset not found. Add a Settings asset to use SDF sculpt guide.");
                    return;
                }
                ActiveSettings.sdfRenderSculptGuide = enabled;
            },
            900f
        );
        UILayoutFactory.CreateCheckboxToggle(
            parent,
            "SdfSculptGuidePointsToggle",
            "SDF guide points",
            accentColor,
            textColor,
            () => ActiveSdfConfig != null && ActiveSdfConfig.sculptGuideRenderPoints,
            (enabled) =>
            {
                Debug.Log($"[DebugTab] SetSdfSculptGuidePointsEnabled -> {enabled}");
                if (ActiveSdfConfig == null)
                {
                    Debug.LogWarning("[DebugTab] SdfVisualizationConfig asset not found. Add it to use sculpt guide points.");
                    return;
                }
                ActiveSdfConfig.sculptGuideRenderPoints = enabled;
                if (enabled)
                {
                    ActiveSdfConfig.sculptGuideMeshEnabled = false;
                    UIComponentHelper.SetToggleState("SdfSculptGuideMeshToggle", false);
                }
            },
            420f
        );
        UILayoutFactory.CreateCheckboxToggle(
            parent,
            "SdfSculptGuideMeshToggle",
            "SDF guide mesh",
            accentColor,
            textColor,
            () => ActiveSdfConfig != null && ActiveSdfConfig.sculptGuideMeshEnabled,
            (enabled) =>
            {
                Debug.Log($"[DebugTab] SetSdfSculptGuideMeshEnabled -> {enabled}");
                if (ActiveSdfConfig == null)
                {
                    Debug.LogWarning("[DebugTab] SdfVisualizationConfig asset not found. Add it to use sculpt guide mesh.");
                    return;
                }
                ActiveSdfConfig.sculptGuideMeshEnabled = enabled;
                if (enabled)
                {
                    ActiveSdfConfig.sculptGuideRenderPoints = false;
                    UIComponentHelper.SetToggleState("SdfSculptGuidePointsToggle", false);
                }
            },
            420f
        );
        UILayoutFactory.CreateCheckboxToggle(
            parent,
            "SdfSculptGuideCacheToggle",
            "SDF guide cache",
            accentColor,
            textColor,
            () => ActiveSdfConfig != null && ActiveSdfConfig.sculptGuideEnableCache,
            (enabled) =>
            {
                Debug.Log($"[DebugTab] SetSdfSculptGuideCacheEnabled -> {enabled}");
                if (ActiveSdfConfig == null)
                {
                    Debug.LogWarning("[DebugTab] SdfVisualizationConfig asset not found. Add it to use sculpt guide cache.");
                    return;
                }
                ActiveSdfConfig.sculptGuideEnableCache = enabled;
            },
            420f
        );
        UILayoutFactory.CreateCheckboxToggle(
            parent,
            "SdfSculptGuideCarveToggle",
            "SDF guide carving",
            accentColor,
            textColor,
            () => ActiveSdfConfig != null && ActiveSdfConfig.sculptGuideCacheCarveEnabled,
            (enabled) =>
            {
                Debug.Log($"[DebugTab] SetSdfSculptGuideCarveEnabled -> {enabled}");
                if (ActiveSdfConfig == null)
                {
                    Debug.LogWarning("[DebugTab] SdfVisualizationConfig asset not found. Add it to use sculpt guide carving.");
                    return;
                }
                ActiveSdfConfig.sculptGuideCacheCarveEnabled = enabled;
            },
            420f
        );
        UILayoutFactory.CreateCheckboxToggle(
            parent,
            "SdfSculptGuideBetweenToggle",
            "SDF guide between",
            accentColor,
            textColor,
            () => ActiveSdfConfig != null && ActiveSdfConfig.sculptGuideBetweenEnabled,
            (enabled) =>
            {
                Debug.Log($"[DebugTab] SetSdfSculptGuideBetweenEnabled -> {enabled}");
                if (ActiveSdfConfig == null)
                {
                    Debug.LogWarning("[DebugTab] SdfVisualizationConfig asset not found. Add it to use sculpt guide between.");
                    return;
                }
                ActiveSdfConfig.sculptGuideBetweenEnabled = enabled;
            },
            420f
        );
        UILayoutFactory.CreateCheckboxToggle(
            parent,
            "SdfDepthErrorToggle",
            "SDF depth error overlay",
            accentColor,
            textColor,
            () => ActiveSdfConfig != null && ActiveSdfConfig.depthErrorEnabled,
            (enabled) =>
            {
                Debug.Log($"[DebugTab] SetDepthErrorEnabled -> {enabled}");
                if (ActiveSdfConfig == null)
                {
                    Debug.LogWarning("[DebugTab] SdfVisualizationConfig asset not found. Add it to use depth error overlay.");
                    return;
                }
                ActiveSdfConfig.depthErrorEnabled = enabled;
            },
            900f
        );
    }
}
