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
        UILayoutFactory.CreateHeader(originHeader, "World origin (single QR)", 90, accentColor, textColor, 15f, 2800f, 36f);

        UILayoutFactory.CreateButton(
            content.transform,
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
            height: 140f);

        // ── 4-corner QR workspace snap ────────────────────────────────
        UILayoutFactory.CreateSpacer(content.transform, 20f);
        var snapHeader = UILayoutFactory.CreateLayoutSection(content.transform, "QrSnapHeader", 90);
        UILayoutFactory.CreateHeader(snapHeader, "Workspace snap (4 QR corners)", 90, accentColor, textColor, 15f, 2800f, 36f);

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

        UILayoutFactory.CreateButton(
            content.transform,
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
            height: 140f);

        // Attach live HUD to keep the status label updated
        var hud = content.AddComponent<TrackingTabHUD>();
        hud.Initialize(statusTmp);

        return content;
    }
}

/// <summary>
/// Updates the QR corner status label in the Tracking tab every frame.
/// </summary>
public class TrackingTabHUD : MonoBehaviour
{
    private TextMeshProUGUI _statusLabel;
    private QrWorkspaceSnapper _snapper;

    public void Initialize(TextMeshProUGUI label)
    {
        _statusLabel = label;
    }

    void Update()
    {
        if (_statusLabel == null) return;

        if (_snapper == null)
            _snapper = Object.FindFirstObjectByType<QrWorkspaceSnapper>();

        if (_snapper == null)
        {
            _statusLabel.text = "<color=#888>QrWorkspaceSnapper not found in scene</color>";
            return;
        }

        int total    = _snapper.TotalCornerCount;
        int detected = _snapper.DetectedCornerCount;

        string dots = "";
        for (int i = 0; i < total; i++)
        {
            bool ok = _snapper.IsCornerTracked(i);
            dots += ok ? "<color=#00ff88>●</color>" : "<color=#555>○</color>";
            if (i < total - 1) dots += "  ";
        }

        string stateText = _snapper.CurrentState switch
        {
            QrWorkspaceSnapper.SnapState.ReadyToSnap      => "<color=#00ff88>Ready</color>",
            QrWorkspaceSnapper.SnapState.Snapped          => "<color=#00ff88>Snapped ✓</color>",
            QrWorkspaceSnapper.SnapState.PartialDetection => $"<color=#ffaa00>{detected}/{total} seen</color>",
            QrWorkspaceSnapper.SnapState.Error            => "<color=#ff4444>Error – check logs</color>",
            _                                             => "<color=#888>No markers detected</color>"
        };

        _statusLabel.text = $"Corners: {dots}   {stateText}";
    }
}
