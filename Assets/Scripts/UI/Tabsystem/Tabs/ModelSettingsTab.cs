using UI.Elements.UIFolderViewer;
using UI.Elements.UIInputField;
using UI.Elements.UISlider;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class ModelSettingsTab : MonoBehaviour
{
    public static GameObject Create(Transform parent, UIStyle style)
    {
        Color accentColor = style.accentColor;
        Color textColor   = style.textColor;

        var content = new GameObject("ModelSettingsContent");
        content.transform.SetParent(parent, false);

        var rect = content.AddComponent<RectTransform>();
        rect.anchorMin    = Vector2.zero;
        rect.anchorMax    = Vector2.one;
        rect.offsetMin    = Vector2.zero;
        rect.offsetMax    = Vector2.zero;
        rect.localPosition = Vector3.zero;
        rect.localScale   = Vector3.one;

        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing           = 35;
        layout.padding           = new RectOffset(50, 50, 50, 50);
        layout.childAlignment    = TextAnchor.UpperLeft;

        Settings settings = SettingsManager.Instance != null ? SettingsManager.Instance.settings : null;

        // ── Model header ──────────────────────────────────────────────
        var modelHeader = UILayoutFactory.CreateLayoutSection(content.transform, "ModelHeader", 90);
        UILayoutFactory.CreateHeader(modelHeader, "Model", 90, accentColor, textColor, 15f, 2800f, 42f);

        // ── Model offset ──────────────────────────────────────────────
        var offsetHeader = UILayoutFactory.CreateLayoutSection(content.transform, "ModelOffsetHeader", 90);
        UILayoutFactory.CreateHeader(offsetHeader, "Model offset", 90, accentColor, textColor, 15f, 2800f, 42f);

        var offsetRow1   = UILayoutFactory.CreateHorizontalRow(content.transform, 220, 30, "ModelOffset1");
        var offsetXInput = UILayoutFactory.CreateInputSection(offsetRow1.transform, "Offset X", 220, 1300f);
        var offsetXField = offsetXInput.AddComponent<UIInputField>();
        offsetXField.CreateInputField("Offset X", "Enter X", accentColor, InputType.DecimalNumber,
            (val) => { if (settings != null && float.TryParse(val, out float f)) settings.modelOffset.x = f; });

        var offsetYInput = UILayoutFactory.CreateInputSection(offsetRow1.transform, "Offset Y", 220, 1300f);
        var offsetYField = offsetYInput.AddComponent<UIInputField>();
        offsetYField.CreateInputField("Offset Y", "Enter Y", accentColor, InputType.DecimalNumber,
            (val) => { if (settings != null && float.TryParse(val, out float f)) settings.modelOffset.y = f; });

        var offsetRow2   = UILayoutFactory.CreateHorizontalRow(content.transform, 220, 30, "ModelOffset2");
        var offsetZInput = UILayoutFactory.CreateInputSection(offsetRow2.transform, "Offset Z", 220, 1300f);
        var offsetZField = offsetZInput.AddComponent<UIInputField>();
        offsetZField.CreateInputField("Offset Z", "Enter Z", accentColor, InputType.DecimalNumber,
            (val) => { if (settings != null && float.TryParse(val, out float f)) settings.modelOffset.z = f; });

        // ── Scale header ──────────────────────────────────────────────
        UILayoutFactory.CreateSpacer(content.transform, 20);
        var scaleHeader = UILayoutFactory.CreateLayoutSection(content.transform, "ScaleHeader", 90);
        UILayoutFactory.CreateHeader(scaleHeader, "Model scale", 90, accentColor, textColor, 15f, 2800f, 42f);

        // ── Scale slider ──────────────────────────────────────────────
        float initialScale = (settings != null && settings.modelScale > 0f) ? settings.modelScale : 1f;

        var scaleSliderGO = UILayoutFactory.CreateSliderElement(
            content.transform,
            "ModelUniformScale",
            "Scale",
            0.01f,
            5f,
            initialScale,
            (val) => { if (settings != null) settings.modelScale = val; },
            accentColor,
            textColor,
            120f,
            -1f,
            0.5f);

        var scaleSlider = scaleSliderGO.GetComponent<UISlider>();

        // ── Auto-fit on load toggle ───────────────────────────────────
        UILayoutFactory.CreateSpacer(content.transform, 80);

        UILayoutFactory.CreateCheckboxToggle(
            content.transform,
            "AutoFitOnLoad",
            "Auto-fit when model loads",
            accentColor,
            textColor,
            getValue: () => settings != null && settings.modelFitToWorkspace,
            setValue: (val) => { if (settings != null) settings.modelFitToWorkspace = val; },
            width: 900f,
            height: 80f);

        // ── Fit to workspace button ───────────────────────────────────
        UILayoutFactory.CreateSpacer(content.transform, 10);

        UILayoutFactory.CreateButton(
            content.transform,
            "FitToWorkspaceBtn",
            "Fit model to workspace",
            accentColor,
            textColor,
            onClick: () =>
            {
                var s = SettingsManager.Instance?.settings;
                if (s == null) return;

                var modelGO = GameObject.Find("RuntimeModel");
                var wsGO    = GameObject.Find("Workspace");

                if (modelGO == null)
                {
                    Debug.LogWarning("[ModelSettingsTab] No model loaded (no 'RuntimeModel' found).");
                    return;
                }

                if (wsGO == null)
                {
                    Debug.LogWarning("[ModelSettingsTab] No workspace found. Place the workspace first.");
                    return;
                }

                RuntimeModelPositionUtility.FitModelToWorkspace(modelGO.transform, wsGO.transform, s);

                // Reflect the computed scale in the slider without triggering the callback
                scaleSlider?.SetValue(s.modelScale);

                Debug.Log($"[ModelSettingsTab] Fit to workspace → scale={s.modelScale:F3}");
            },
            height: 110f);

        // ── Populate initial field values ─────────────────────────────
        if (settings != null)
        {
            offsetXField.SetText(settings.modelOffset.x.ToString());
            offsetYField.SetText(settings.modelOffset.y.ToString());
            offsetZField.SetText(settings.modelOffset.z.ToString());
        }



        // load model section

        // ── Load Model header ──────────────────────────────────────────────
        UILayoutFactory.CreateSpacer(content.transform, 20);
        var loadHeader = UILayoutFactory.CreateLayoutSection(content.transform, "LoadHeader", 90);
        UILayoutFactory.CreateHeader(loadHeader, "Load model", 90, accentColor, textColor, 15f, 2800f, 42f);

        string folderPath = SettingsManager.Instance?.settings?.folderViewerPath ?? "";
        Debug.Log($"[FilesTab] Creating FilesTab. SettingsManager.Instance={(SettingsManager.Instance != null)}, " +
                  $"folderViewerPath='{folderPath}'");

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            Debug.LogWarning("[FilesTab] folderViewerPath is empty/null. UIFolderViewer will fall back to its own default.");
        }
        else if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"[FilesTab] folderViewerPath directory does NOT exist: '{folderPath}'");
        }
        else
        {
            var allFiles = Directory.GetFiles(folderPath);
            Debug.Log($"[FilesTab] Directory '{folderPath}' exists and contains {allFiles.Length} files before filtering.");
        }

        GameObject viewerGO = new GameObject("FolderViewer");
        viewerGO.transform.SetParent(content.transform, false);
        LayoutElement viewerLayout = viewerGO.AddComponent<LayoutElement>();
        viewerLayout.minHeight = 400;
        viewerLayout.flexibleHeight = 1;

        UIFolderViewer viewer = viewerGO.AddComponent<UIFolderViewer>();
        viewer.CreateFolderViewer(folderPath, OnLoadRequested, style.textColor, style.accentColor);




        return content;
    }

    static void OnLoadRequested(string path)
    {
        // User will implement load logic here
        Debug.Log($"[FilesTab] Load model: {path}");
    }
}
