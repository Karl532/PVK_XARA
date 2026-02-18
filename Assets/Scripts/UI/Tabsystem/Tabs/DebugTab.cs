using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI.Elements.UICheckbox;
using Assets.Scripts.Depth.Quest3.OXDepth;
using Assets.Scripts.Depth.Quest3.OXDepth.Diagnostics;

public class DebugTab : MonoBehaviour
{
    public static GameObject Create(Transform parent, UIStyle style)
    {
        Color accentColor = style.accentColor;
        Color textColor = style.textColor;

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
        CreateSpacer(content.transform, 10f);

        // Row: depth overlay + SDF grid
        Transform toggleRow = CreateToggleRow(content.transform, "DebugToggleRow");
        CreateDepthOverlayToggle(toggleRow, accentColor, textColor);
        CreateRowSpacer(toggleRow);
        CreateSdfFullOverlayToggle(toggleRow, accentColor, textColor);

        // Bottom: SDF sculpt guide
        CreateSdfSculptGuideToggle(content.transform, accentColor, textColor);
        CreateSdfSculptGuidePointsToggle(content.transform, accentColor, textColor);
        CreateSdfSculptGuideMeshToggle(content.transform, accentColor, textColor);
        CreateSdfSculptGuideMeshStepSlider(content.transform, accentColor, textColor);
        CreateSdfSculptGuideMeshSmoothingSlider(content.transform, accentColor, textColor);
        CreateSdfSculptGuideCacheToggle(content.transform, accentColor, textColor);
        CreateSdfSculptGuideCarveToggle(content.transform, accentColor, textColor);
        CreateSdfSculptGuideBetweenToggle(content.transform, accentColor, textColor);
        CreateSdfSculptGuidePointBudgetSlider(content.transform, accentColor, textColor);

        CreateSpacer(content.transform, 24f);
        CreateDotSizeSlider(content.transform, accentColor, textColor);
        CreateSpacer(content.transform, 40f);
        CreateSkipPixelsSlider(content.transform, accentColor, textColor);

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
            var settings = SettingsManager.Instance != null ? SettingsManager.Instance.settings : null;
            if (RuntimeModelLoader.Instance != null)
            {
                RuntimeModelLoader.Instance.LoadTestModel(settings);
            }
            else
            {
                Debug.LogError("[DebugTab] RuntimeModelLoader.Instance is null. Add RuntimeModelLoader to the scene.");
            }
        });
    }

    private static void CreateDepthOverlayToggle(Transform parent, Color accentColor, Color textColor)
    {
        GameObject toggleGO = new GameObject("DepthOverlayToggle");
        toggleGO.transform.SetParent(parent, false);

        var le = toggleGO.AddComponent<LayoutElement>();
        le.minWidth = 420f;
        le.preferredWidth = 420f;
        le.minHeight = 70f;
        le.preferredHeight = 70f;

        var checkbox = toggleGO.AddComponent<UICheckbox>();
        checkbox.CreateCheckbox(
            "Toggle depth overlay",
            accentColor,
            IsDepthOverlayEnabled(),
            (isOn) => SetDepthOverlayEnabled(isOn),
            textColor
        );
    }

    private static void CreateSdfFullOverlayToggle(Transform parent, Color accentColor, Color textColor)
    {
        GameObject toggleGO = new GameObject("SdfFullOverlayToggle");
        toggleGO.transform.SetParent(parent, false);

        var le = toggleGO.AddComponent<LayoutElement>();
        le.minWidth = 420f;
        le.preferredWidth = 420f;
        le.minHeight = 70f;
        le.preferredHeight = 70f;

        var checkbox = toggleGO.AddComponent<UICheckbox>();
        checkbox.CreateCheckbox(
            "SDF debug overlay (grid)",
            accentColor,
            IsSdfFullOverlayEnabled(),
            (isOn) => SetSdfFullOverlayEnabled(isOn),
            textColor
        );
    }

    private static void CreateSdfSculptGuideToggle(Transform parent, Color accentColor, Color textColor)
    {
        GameObject toggleGO = new GameObject("SdfSculptGuideToggle");
        toggleGO.transform.SetParent(parent, false);

        var le = toggleGO.AddComponent<LayoutElement>();
        le.minWidth = 900f;
        le.preferredWidth = 900f;
        le.minHeight = 70f;
        le.preferredHeight = 70f;

        var checkbox = toggleGO.AddComponent<UICheckbox>();
        checkbox.CreateCheckbox(
            "SDF sculpt guide",
            accentColor,
            IsSdfSculptGuideEnabled(),
            (isOn) => SetSdfSculptGuideEnabled(isOn),
            textColor
        );
    }

    private static void CreateSdfSculptGuidePointsToggle(Transform parent, Color accentColor, Color textColor)
    {
        GameObject toggleGO = new GameObject("SdfSculptGuidePointsToggle");
        toggleGO.transform.SetParent(parent, false);

        var le = toggleGO.AddComponent<LayoutElement>();
        le.minWidth = 420f;
        le.preferredWidth = 420f;
        le.minHeight = 70f;
        le.preferredHeight = 70f;

        var checkbox = toggleGO.AddComponent<UICheckbox>();
        checkbox.CreateCheckbox(
            "SDF guide points",
            accentColor,
            IsSdfSculptGuidePointsEnabled(),
            (isOn) => SetSdfSculptGuidePointsEnabled(isOn),
            textColor
        );
    }

    private static void CreateSdfSculptGuidePointBudgetSlider(Transform parent, Color accentColor, Color textColor)
    {
        int initial = 50000;
        var config = FindSdfVisualizationConfig();
        if (config != null)
            initial = Mathf.Clamp(config.sculptGuidePointRenderMaxCount, 10000, 500000);

        UILayoutFactory.CreateSliderElement(
            parent,
            "SdfGuidePointBudget",
            "SDF guide max points",
            10000f,
            500000f,
            initial,
            (val) =>
            {
                var cfg = FindSdfVisualizationConfig();
                if (cfg != null)
                    cfg.sculptGuidePointRenderMaxCount = Mathf.Clamp(Mathf.RoundToInt(val), 10000, 500000);
            },
            accentColor,
            textColor,
            120f,
            -1f,
            1f
        );
    }

    private static void CreateSdfSculptGuideMeshToggle(Transform parent, Color accentColor, Color textColor)
    {
        GameObject toggleGO = new GameObject("SdfSculptGuideMeshToggle");
        toggleGO.transform.SetParent(parent, false);

        var le = toggleGO.AddComponent<LayoutElement>();
        le.minWidth = 420f;
        le.preferredWidth = 420f;
        le.minHeight = 70f;
        le.preferredHeight = 70f;

        var checkbox = toggleGO.AddComponent<UICheckbox>();
        checkbox.CreateCheckbox(
            "SDF guide mesh",
            accentColor,
            IsSdfSculptGuideMeshEnabled(),
            (isOn) => SetSdfSculptGuideMeshEnabled(isOn),
            textColor
        );
    }

    private static void CreateSdfSculptGuideMeshStepSlider(Transform parent, Color accentColor, Color textColor)
    {
        int initial = 4;
        var config = FindSdfVisualizationConfig();
        if (config != null)
            initial = Mathf.Clamp(config.sculptGuideMeshStep, 1, 16);

        UILayoutFactory.CreateSliderElement(
            parent,
            "SdfGuideMeshStep",
            "SDF guide mesh step",
            1f,
            16f,
            initial,
            (val) =>
            {
                var cfg = FindSdfVisualizationConfig();
                if (cfg != null)
                    cfg.sculptGuideMeshStep = Mathf.Clamp(Mathf.RoundToInt(val), 1, 16);
            },
            accentColor,
            textColor,
            120f,
            -1f,
            1f
        );
    }

    private static void CreateSdfSculptGuideMeshSmoothingSlider(Transform parent, Color accentColor, Color textColor)
    {
        float initial = 0.8f;
        var config = FindSdfVisualizationConfig();
        if (config != null)
            initial = Mathf.Clamp01(config.sculptGuideMeshSmoothingAlpha);

        UILayoutFactory.CreateSliderElement(
            parent,
            "SdfGuideMeshSmoothing",
            "SDF guide mesh smoothing",
            0f,
            1f,
            initial,
            (val) =>
            {
                var cfg = FindSdfVisualizationConfig();
                if (cfg != null)
                    cfg.sculptGuideMeshSmoothingAlpha = Mathf.Clamp01(val);
            },
            accentColor,
            textColor,
            120f,
            -1f,
            0.01f
        );
    }

    private static void CreateSdfSculptGuideCacheToggle(Transform parent, Color accentColor, Color textColor)
    {
        GameObject toggleGO = new GameObject("SdfSculptGuideCacheToggle");
        toggleGO.transform.SetParent(parent, false);

        var le = toggleGO.AddComponent<LayoutElement>();
        le.minWidth = 420f;
        le.preferredWidth = 420f;
        le.minHeight = 70f;
        le.preferredHeight = 70f;

        var checkbox = toggleGO.AddComponent<UICheckbox>();
        checkbox.CreateCheckbox(
            "SDF guide cache",
            accentColor,
            IsSdfSculptGuideCacheEnabled(),
            (isOn) => SetSdfSculptGuideCacheEnabled(isOn),
            textColor
        );
    }

    private static void CreateSdfSculptGuideCarveToggle(Transform parent, Color accentColor, Color textColor)
    {
        GameObject toggleGO = new GameObject("SdfSculptGuideCarveToggle");
        toggleGO.transform.SetParent(parent, false);

        var le = toggleGO.AddComponent<LayoutElement>();
        le.minWidth = 420f;
        le.preferredWidth = 420f;
        le.minHeight = 70f;
        le.preferredHeight = 70f;

        var checkbox = toggleGO.AddComponent<UICheckbox>();
        checkbox.CreateCheckbox(
            "SDF guide carving",
            accentColor,
            IsSdfSculptGuideCarveEnabled(),
            (isOn) => SetSdfSculptGuideCarveEnabled(isOn),
            textColor
        );
    }

    private static void CreateSdfSculptGuideBetweenToggle(Transform parent, Color accentColor, Color textColor)
    {
        GameObject toggleGO = new GameObject("SdfSculptGuideBetweenToggle");
        toggleGO.transform.SetParent(parent, false);

        var le = toggleGO.AddComponent<LayoutElement>();
        le.minWidth = 420f;
        le.preferredWidth = 420f;
        le.minHeight = 70f;
        le.preferredHeight = 70f;

        var checkbox = toggleGO.AddComponent<UICheckbox>();
        checkbox.CreateCheckbox(
            "SDF guide between",
            accentColor,
            IsSdfSculptGuideBetweenEnabled(),
            (isOn) => SetSdfSculptGuideBetweenEnabled(isOn),
            textColor
        );
    }


    private static void CreateDotSizeSlider(Transform parent, Color accentColor, Color textColor)
    {
        float initial = 2f;
        var renderer = FindDepthRenderer();
        if (renderer != null)
            initial = renderer.dotSizePx;

        UILayoutFactory.CreateSliderElement(
            parent,
            "DepthDotSize",
            "Depth dot size (px)",
            1f,
            10f,
            initial,
            (val) =>
            {
                var r = FindOrCreateDepthRenderer();
                if (r != null)
                    r.dotSizePx = val;
            },
            accentColor,
            textColor,
            120f,
            -1f,
            0.5f
        );
    }

    private static void CreateSkipPixelsSlider(Transform parent, Color accentColor, Color textColor)
    {
        int initial = 2;
        var api = FindDepthApi();
        if (api != null)
            initial = Mathf.Max(1, api.skipPixels);

        UILayoutFactory.CreateSliderElement(
            parent,
            "DepthSkipPixels",
            "Depth skip pixels",
            1f,
            16f,
            initial,
            (val) =>
            {
                var a = FindDepthApi();
                if (a != null)
                    a.skipPixels = Mathf.Max(1, Mathf.RoundToInt(val));
            },
            accentColor,
            textColor,
            120f,
            -1f,
            1f
        );
    }

    private static void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(parent, false);
        LayoutElement le = spacer.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
        le.flexibleHeight = 0;
    }

    private static Transform CreateToggleRow(Transform parent, string name)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);

        var rect = row.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.sizeDelta = new Vector2(900f, 80f);

        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 24f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var le = row.AddComponent<LayoutElement>();
        le.minHeight = 80f;
        le.preferredHeight = 80f;

        return row.transform;
    }

    private static void CreateRowSpacer(Transform parent)
    {
        GameObject spacer = new GameObject("RowSpacer");
        spacer.transform.SetParent(parent, false);
        LayoutElement le = spacer.AddComponent<LayoutElement>();
        le.minWidth = 20f;
        le.preferredWidth = 20f;
        le.flexibleWidth = 1f;
    }

    private static bool IsDepthOverlayEnabled()
    {
        var renderers = Object.FindObjectsOfType<OXDepthPointCloudRenderer>(true);
        if (renderers == null || renderers.Length == 0)
            return false;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].enabled)
                return true;
        }
        return false;
    }

    private static void SetDepthOverlayEnabled(bool enabled)
    {
        Debug.Log($"[DebugTab] SetDepthOverlayEnabled -> {enabled}");
        var api = FindDepthApi();
        if (api != null)
        {
            api.SetPointCloudRendererEnabled(enabled);
            return;
        }

        var renderers = Object.FindObjectsOfType<OXDepthPointCloudRenderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].SetRenderingEnabled(enabled);
            }
            return;
        }

        if (!enabled)
            return;

        var renderer = FindOrCreateDepthRenderer();
        if (renderer == null)
        {
            Debug.LogWarning("[DebugTab] Could not find OXDepthPointCloudAPI to enable overlay.");
            return;
        }

        renderer.SetRenderingEnabled(true);
    }

    private static OXDepthPointCloudRenderer FindDepthRenderer()
    {
        return Object.FindFirstObjectByType<OXDepthPointCloudRenderer>();
    }

    private static OXDepthPointCloudRenderer FindOrCreateDepthRenderer()
    {
        var renderer = FindDepthRenderer();
        if (renderer != null)
            return renderer;

        var api = FindDepthApi();
        if (api != null)
            return api.gameObject.AddComponent<OXDepthPointCloudRenderer>();

        return null;
    }

    private static OXDepthPointCloudAPI FindDepthApi()
    {
        var apis = Object.FindObjectsOfType<OXDepthPointCloudAPI>(true);
        if (apis != null && apis.Length > 0)
            return apis[0];
        return null;
    }

    private static bool IsSdfFullOverlayEnabled()
    {
        var settings = FindSettings();
        return settings != null && settings.sdfRenderFullSdfGrid;
    }

    private static bool IsSdfSculptGuideEnabled()
    {
        var settings = FindSettings();
        return settings != null && settings.sdfRenderSculptGuide;
    }

    private static bool IsSdfSculptGuidePointsEnabled()
    {
        var config = FindSdfVisualizationConfig();
        return config != null && config.sculptGuideRenderPoints;
    }

    private static bool IsSdfSculptGuideMeshEnabled()
    {
        var config = FindSdfVisualizationConfig();
        return config != null && config.sculptGuideMeshEnabled;
    }

    private static bool IsSdfSculptGuideCacheEnabled()
    {
        var config = FindSdfVisualizationConfig();
        return config != null && config.sculptGuideEnableCache;
    }

    private static bool IsSdfSculptGuideCarveEnabled()
    {
        var config = FindSdfVisualizationConfig();
        return config != null && config.sculptGuideCacheCarveEnabled;
    }

    private static bool IsSdfSculptGuideBetweenEnabled()
    {
        var config = FindSdfVisualizationConfig();
        return config != null && config.sculptGuideBetweenEnabled;
    }

    private static void SetSdfFullOverlayEnabled(bool enabled)
    {
        Debug.Log($"[DebugTab] SetSdfFullOverlayEnabled -> {enabled}");
        var settings = FindSettings();
        if (settings == null)
        {
            Debug.LogWarning("[DebugTab] Settings asset not found. Add a Settings asset to use SDF overlay.");
            return;
        }

        settings.sdfRenderFullSdfGrid = enabled;
    }

    private static void SetSdfSculptGuideEnabled(bool enabled)
    {
        Debug.Log($"[DebugTab] SetSdfSculptGuideEnabled -> {enabled}");
        var settings = FindSettings();
        if (settings == null)
        {
            Debug.LogWarning("[DebugTab] Settings asset not found. Add a Settings asset to use SDF sculpt guide.");
            return;
        }

        settings.sdfRenderSculptGuide = enabled;
    }

    private static void SetSdfSculptGuidePointsEnabled(bool enabled)
    {
        Debug.Log($"[DebugTab] SetSdfSculptGuidePointsEnabled -> {enabled}");
        var config = FindSdfVisualizationConfig();
        if (config == null)
        {
            Debug.LogWarning("[DebugTab] SdfVisualizationConfig asset not found. Add it to use sculpt guide points.");
            return;
        }

        config.sculptGuideRenderPoints = enabled;
        if (enabled)
        {
            config.sculptGuideMeshEnabled = false;
            SetToggleState("SdfSculptGuideMeshToggle", false);
        }
    }

    private static void SetSdfSculptGuideMeshEnabled(bool enabled)
    {
        Debug.Log($"[DebugTab] SetSdfSculptGuideMeshEnabled -> {enabled}");
        var config = FindSdfVisualizationConfig();
        if (config == null)
        {
            Debug.LogWarning("[DebugTab] SdfVisualizationConfig asset not found. Add it to use sculpt guide mesh.");
            return;
        }

        config.sculptGuideMeshEnabled = enabled;
        if (enabled)
        {
            config.sculptGuideRenderPoints = false;
            SetToggleState("SdfSculptGuidePointsToggle", false);
        }
    }

    private static void SetToggleState(string toggleName, bool isOn)
    {
        var toggles = Object.FindObjectsOfType<Toggle>(true);
        if (toggles == null)
            return;

        for (int i = 0; i < toggles.Length; i++)
        {
            var t = toggles[i];
            if (t != null && t.gameObject.name == toggleName)
            {
                t.isOn = isOn;
                return;
            }
        }
    }

    private static void SetSdfSculptGuideCacheEnabled(bool enabled)
    {
        Debug.Log($"[DebugTab] SetSdfSculptGuideCacheEnabled -> {enabled}");
        var config = FindSdfVisualizationConfig();
        if (config == null)
        {
            Debug.LogWarning("[DebugTab] SdfVisualizationConfig asset not found. Add it to use sculpt guide cache.");
            return;
        }

        config.sculptGuideEnableCache = enabled;
    }

    private static void SetSdfSculptGuideCarveEnabled(bool enabled)
    {
        Debug.Log($"[DebugTab] SetSdfSculptGuideCarveEnabled -> {enabled}");
        var config = FindSdfVisualizationConfig();
        if (config == null)
        {
            Debug.LogWarning("[DebugTab] SdfVisualizationConfig asset not found. Add it to use sculpt guide carving.");
            return;
        }

        config.sculptGuideCacheCarveEnabled = enabled;
    }

    private static void SetSdfSculptGuideBetweenEnabled(bool enabled)
    {
        Debug.Log($"[DebugTab] SetSdfSculptGuideBetweenEnabled -> {enabled}");
        var config = FindSdfVisualizationConfig();
        if (config == null)
        {
            Debug.LogWarning("[DebugTab] SdfVisualizationConfig asset not found. Add it to use sculpt guide between.");
            return;
        }

        config.sculptGuideBetweenEnabled = enabled;
    }

    private static Settings FindSettings()
    {
        if (SettingsManager.Instance != null && SettingsManager.Instance.settings != null)
            return SettingsManager.Instance.settings;
        return Settings.FindAnySettingsAsset();
    }

    private static SdfVisualizationConfig FindSdfVisualizationConfig()
    {
        var assets = Resources.FindObjectsOfTypeAll<SdfVisualizationConfig>();
        if (assets != null && assets.Length > 0)
            return assets[0];
        return null;
    }
}
