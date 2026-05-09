using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrackingTab : MonoBehaviour
{
    public static GameObject Create(Transform parent, UIStyle style)
    {
        Color accentColor = style.accentColor;
        Color textColor   = style.textColor;

        var content = new GameObject("TrackingContent");
        content.transform.SetParent(parent, false);

        var rect = content.AddComponent<RectTransform>();
        rect.anchorMin    = Vector2.zero;
        rect.anchorMax    = Vector2.one;
        rect.offsetMin    = Vector2.zero;
        rect.offsetMax    = Vector2.zero;
        rect.localScale   = Vector3.one;

        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing        = 35;
        layout.padding        = new RectOffset(50, 50, 50, 50);
        layout.childAlignment = TextAnchor.UpperLeft;

        // ── Tracking header ───────────────────────────────────────────
        var headerSection = UILayoutFactory.CreateLayoutSection(content.transform, "TrackingHeader", 90);
        UILayoutFactory.CreateHeader(headerSection, "Tracking", 90, accentColor, textColor, 15f, 2800f, 42f);

        // ── Single-QR calibration ─────────────────────────────────────
        var originHeader = UILayoutFactory.CreateLayoutSection(content.transform, "OriginHeader", 90);
        UILayoutFactory.CreateHeader(originHeader, "World origin (single QR)", 90, accentColor * 0.65f, textColor, 15f, 2800f, 36f);


        //create wrapper to center and change width of button
        GameObject button1Wrapper = new GameObject("Button1Wrapper");
        button1Wrapper.transform.SetParent(content.transform, false);
        LayoutElement wrapper1Layout = button1Wrapper.AddComponent<LayoutElement>();
        wrapper1Layout.minHeight = 140f;
        wrapper1Layout.preferredHeight = 140f;

        UILayoutFactory.CreateButton(
            button1Wrapper.transform,
            "SetOriginBtn",
            "Set origin now",
            accentColor,
            textColor,
            onClick: () =>
            {
                var controller = Object.FindFirstObjectByType<CalibrationOriginController>();
                if (controller != null)
                    controller.CalibrateNow();
                else
                    Debug.LogWarning("[TrackingTab] No CalibrationOriginController found in scene.");
            },
            height: 140f, width: 900f);

        // ── 4-corner QR workspace snap ────────────────────────────────
        UILayoutFactory.CreateSpacer(content.transform, 20f);
        var snapHeader = UILayoutFactory.CreateLayoutSection(content.transform, "QrSnapHeader", 90);
        UILayoutFactory.CreateHeader(snapHeader, "Workspace snap (4 QR corners)", 90, accentColor * 0.65f, textColor, 15f, 2800f, 36f);

        // Live status label — updated by TrackingTabHUD below
        var statusGO = new GameObject("QrSnapStatus");
        statusGO.transform.SetParent(content.transform, false);
        var statusLE = statusGO.AddComponent<LayoutElement>();
        statusLE.minHeight       = 60f;
        statusLE.preferredHeight = 60f;
        var statusTmp = statusGO.AddComponent<TextMeshProUGUI>();
        statusTmp.fontSize        = 28f;
        statusTmp.color           = textColor;
        statusTmp.richText        = true;
        statusTmp.alignment       = TextAlignmentOptions.Left;
        statusTmp.enableWordWrapping = false;

        //create wrapper to center and change width of button
        GameObject button2Wrapper = new GameObject("Button2Wrapper");
        button2Wrapper.transform.SetParent(content.transform, false);
        LayoutElement wrapper2Layout = button2Wrapper.AddComponent<LayoutElement>();
        wrapper2Layout.minHeight = 140f;
        wrapper2Layout.preferredHeight = 140f;

        UILayoutFactory.CreateButton(
            button2Wrapper.transform,
            "QrSnapBtn",
            "Snap workspace to QR corners",
            accentColor,
            textColor,
            onClick: () =>
            {
                var snapper = Object.FindFirstObjectByType<QrWorkspaceSnapper>();
                if (snapper != null)
                    snapper.TrySnapToQrCorners();
                else
                    Debug.LogWarning("[TrackingTab] No QrWorkspaceSnapper found in scene.");
            },
            height: 140f, width: 900f);

        // Attach live HUD to keep the status label updated
        var hud = content.AddComponent<TrackingTabHUD>();
        hud.Initialize(statusTmp);

        return content;
    }
}
