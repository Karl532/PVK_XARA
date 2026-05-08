using TMPro;
using UnityEngine;

/// <summary>
/// Live HUD that updates the workspace placement instruction panel every frame.
/// Created by WorkspacePlacementInstructionUIFactory and attached to the instruction canvas.
/// </summary>
public class WorkspacePlacementHUD : MonoBehaviour
{
    private TextMeshProUGUI _text;
    private QrWorkspaceSnapper _qrSnapper;

    public void Initialize(
        TextMeshProUGUI text,
        WorkspacePlacementController controller,
        QrWorkspaceSnapper qrSnapper)
    {
        _text      = text;
        _qrSnapper = qrSnapper;
    }

    void Update()
    {
        if (_text == null) return;
        _text.text = BuildText();
    }

    private string BuildText()
    {
        var settings = SettingsManager.Instance?.settings;
        Vector3 dims = settings?.stoneBlockDimensions ?? Vector3.one;

        // ── Mode line ──────────────────────────────────────────────
        string modeLine = "Move R.stick XZ  |  L.stick Y  |  L.stick↔ rotate";

        // ── Size line ──────────────────────────────────────────────
        string sizeLine = $"<color=#aaaaaa>W</color> {dims.x:F2}m  "
                        + $"<color=#aaaaaa>H</color> {dims.y:F2}m  "
                        + $"<color=#aaaaaa>D</color> {dims.z:F2}m";

        // ── QR snap status ─────────────────────────────────────────
        string qrLine = "";
        if (_qrSnapper != null)
        {
            int total = _qrSnapper.TotalCornerCount;
            int seen  = _qrSnapper.SeenCornerCount;

            // ● green = live  ● yellow = seen but out of view  ○ grey = never seen
            string dots = "";
            for (int i = 0; i < total; i++)
            {
                if (_qrSnapper.IsCornerTracked(i))
                    dots += "<color=#00ff88>●</color>";
                else if (_qrSnapper.IsCornerEverSeen(i))
                    dots += "<color=#ffcc00>●</color>";
                else
                    dots += "<color=#444>○</color>";
                if (i < total - 1) dots += " ";
            }

            string stateLabel = _qrSnapper.CurrentState switch
            {
                QrWorkspaceSnapper.SnapState.ReadyToSnap      => "<color=#00ff88> READY</color>",
                QrWorkspaceSnapper.SnapState.Snapped          => "<color=#00ff88> SNAPPED</color>",
                QrWorkspaceSnapper.SnapState.PartialDetection => $"<color=#ffaa00> {seen}/{total} scanned</color>",
                QrWorkspaceSnapper.SnapState.Error            => "<color=#ff4444> ERR</color>",
                _                                             => ""
            };

            string snapPrompt = _qrSnapper.CurrentState == QrWorkspaceSnapper.SnapState.ReadyToSnap
                ? "  <color=#ffcc00>[A] Snap</color>"
                : "";

            qrLine = $"\nQR {dots}{stateLabel}{snapPrompt}";
        }

        // ── Confirm hint ───────────────────────────────────────────
        string confirmLine = "\n<color=#aaaaaa>[B] Confirm placement</color>";

        return modeLine + "\n" + sizeLine + qrLine + confirmLine;
    }
}
