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

        bool resizing = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger) > 0.5f;

        // ── Mode line ──────────────────────────────────────────────
        string modeLine = resizing
            ? "<color=#ffcc00>RESIZE  R.stick W/D  |  L.stick H</color>"
            : "Move R.stick XZ  |  L.stick Y  |  L.stick↔ rotate";

        // ── Size line ──────────────────────────────────────────────
        string sizeLine = $"<color=#aaaaaa>W</color> {dims.x:F2}m  "
                        + $"<color=#aaaaaa>H</color> {dims.y:F2}m  "
                        + $"<color=#aaaaaa>D</color> {dims.z:F2}m";

        // Hold Grip hint (only when not already resizing)
        string gripHint = resizing ? "" : "\n<size=80%><color=#888>Hold R.Grip + stick to resize</color></size>";

        // ── QR snap status ─────────────────────────────────────────
        string qrLine = "";
        if (_qrSnapper != null)
        {
            int total = _qrSnapper.TotalCornerCount;
            int detected = _qrSnapper.DetectedCornerCount;

            // Build corner dots: green filled = tracked, dark hollow = missing
            string dots = "";
            for (int i = 0; i < total; i++)
            {
                bool ok = _qrSnapper.IsCornerTracked(i);
                dots += ok ? "<color=#00ff88>●</color>" : "<color=#444>○</color>";
                if (i < total - 1) dots += " ";
            }

            string stateLabel = _qrSnapper.CurrentState switch
            {
                QrWorkspaceSnapper.SnapState.ReadyToSnap      => "<color=#00ff88> READY</color>",
                QrWorkspaceSnapper.SnapState.Snapped          => "<color=#00ff88> SNAPPED</color>",
                QrWorkspaceSnapper.SnapState.PartialDetection => $"<color=#ffaa00> {detected}/{total}</color>",
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

        return modeLine + "\n" + sizeLine + gripHint + qrLine + confirmLine;
    }
}
