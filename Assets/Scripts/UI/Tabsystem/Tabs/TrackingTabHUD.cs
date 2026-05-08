using TMPro;
using UnityEngine;

/// <summary>
/// Updates the QR corner status label in the Tracking tab every frame.
/// Must be in its own file so Unity can resolve AddComponent&lt;TrackingTabHUD&gt;.
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
            _statusLabel.text = "<color=#888>QrWorkspaceSnapper not in scene</color>";
            return;
        }

        int total = _snapper.TotalCornerCount;
        int seen  = _snapper.SeenCornerCount;

        // ● green  = currently tracked live
        // ● yellow = scanned and remembered, now out of view
        // ○ grey   = never seen
        string dots = "";
        for (int i = 0; i < total; i++)
        {
            if (_snapper.IsCornerTracked(i))
                dots += "<color=#00ff88>●</color>";
            else if (_snapper.IsCornerEverSeen(i))
                dots += "<color=#ffcc00>●</color>";
            else
                dots += "<color=#555>○</color>";
            if (i < total - 1) dots += "  ";
        }

        string stateText = _snapper.CurrentState switch
        {
            QrWorkspaceSnapper.SnapState.ReadyToSnap      => "<color=#00ff88>Ready – press [A]</color>",
            QrWorkspaceSnapper.SnapState.Snapped          => "<color=#00ff88>Snapped ✓</color>",
            QrWorkspaceSnapper.SnapState.PartialDetection => $"<color=#ffaa00>Scan remaining codes ({seen}/{total})</color>",
            QrWorkspaceSnapper.SnapState.Error            => "<color=#ff4444>Error – check logs</color>",
            _                                             => "<color=#888>No markers detected</color>"
        };

        _statusLabel.text = $"Corners: {dots}   {stateText}";
    }
}
