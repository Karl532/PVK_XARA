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
        layout.spacing = 35;
        layout.padding = new RectOffset(50, 50, 50, 50);
        layout.childAlignment = TextAnchor.UpperLeft;

        GameObject headerSection = UILayoutFactory.CreateLayoutSection(content.transform, "DebugHeader", 90);
        UILayoutFactory.CreateHeader(headerSection, "Debug", 90, accentColor, textColor, 15f, 2800f, 42f);

        CreateLoadTestModelButton(content.transform, accentColor, textColor, style.cornerRadius);
        // Top: depth overlay (OXDepth)
        CreateDepthOverlayToggle(content.transform, accentColor, textColor);
        // Middle: SDF full grid debug
        CreateSdfFullOverlayToggle(content.transform, accentColor, textColor);
        // Bottom: SDF sculpt guide
        CreateSdfSculptGuideToggle(content.transform, accentColor, textColor);
        CreateSpacer(content.transform, 20f);
        CreateDotSizeSlider(content.transform, accentColor, textColor);
        CreateSpacer(content.transform, 20f);
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

        var checkbox = toggleGO.AddComponent<UICheckbox>();
        checkbox.CreateCheckbox(
            "SDF sculpt guide",
            accentColor,
            IsSdfSculptGuideEnabled(),
            (isOn) => SetSdfSculptGuideEnabled(isOn),
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
        return Object.FindFirstObjectByType<OXDepthPointCloudAPI>();
    }

    private static bool IsSdfFullOverlayEnabled()
    {
        var api = FindSdfOverlayApi();
        return api != null && api.IsFullSdfDebugEnabled();
    }

    private static bool IsSdfSculptGuideEnabled()
    {
        var api = FindSdfOverlayApi();
        return api != null && api.IsSculptGuideEnabled();
    }

    private static void SetSdfFullOverlayEnabled(bool enabled)
    {
        Debug.Log($"[DebugTab] SetSdfFullOverlayEnabled -> {enabled}");
        var api = FindSdfOverlayApi();
        if (api == null)
        {
            Debug.LogWarning("[DebugTab] SdfOverlayApi not found. Add it to the scene to use SDF overlay.");
            return;
        }

        api.SetFullSdfDebug(enabled);
    }

    private static void SetSdfSculptGuideEnabled(bool enabled)
    {
        Debug.Log($"[DebugTab] SetSdfSculptGuideEnabled -> {enabled}");
        var api = FindSdfOverlayApi();
        if (api == null)
        {
            Debug.LogWarning("[DebugTab] SdfOverlayApi not found. Add it to the scene to use SDF sculpt guide.");
            return;
        }

        api.SetSculptGuideEnabled(enabled);
    }

    private static SdfOverlayApi FindSdfOverlayApi()
    {
        return Object.FindFirstObjectByType<SdfOverlayApi>();
    }
}
