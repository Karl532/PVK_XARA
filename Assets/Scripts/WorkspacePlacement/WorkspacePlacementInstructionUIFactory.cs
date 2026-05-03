using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI.Utils;

/// <summary>
/// Builds the on-screen instruction HUD for workspace placement mode.
/// Returns the root canvas GameObject so the caller can destroy it when done.
/// Attaches a WorkspacePlacementHUD component that updates the text every frame.
/// </summary>
public static class WorkspacePlacementInstructionUIFactory
{
    /// <summary>
    /// Creates the instruction canvas parented to nothing (root-level).
    /// Passes <paramref name="controller"/> and <paramref name="qrSnapper"/> to the
    /// live HUD component so it can reflect runtime state.
    /// </summary>
    public static GameObject CreateInstructionUI(
        Camera xrCamera,
        WorkspacePlacementController controller = null,
        QrWorkspaceSnapper qrSnapper = null)
    {
        // ── Canvas ────────────────────────────────────────────────────
        var root = new GameObject("WorkspacePlacementInstructions");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = xrCamera != null ? xrCamera : Camera.main;
        canvas.planeDistance = 1f;
        canvas.sortingOrder  = 100;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        var canvasRect = root.GetComponent<RectTransform>();
        canvasRect.anchorMin  = Vector2.zero;
        canvasRect.anchorMax  = Vector2.one;
        canvasRect.offsetMin  = Vector2.zero;
        canvasRect.offsetMax  = Vector2.zero;

        // ── Panel ─────────────────────────────────────────────────────
        var panel = new GameObject("InstructionPanel");
        panel.transform.SetParent(root.transform, false);

        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin       = new Vector2(0, 1);
        panelRect.anchorMax       = new Vector2(0, 1);
        panelRect.pivot           = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(40, -40);
        panelRect.sizeDelta       = new Vector2(520, 200);

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.04f, 0.12f, 0.92f);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding             = new RectOffset(20, 20, 16, 20);
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = true;
        layout.childControlWidth      = true;
        layout.childControlHeight     = true;

        // ── Text ──────────────────────────────────────────────────────
        var textObj = new GameObject("InstructionText");
        textObj.transform.SetParent(panel.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize   = 26 * UIPrimitives.Sizing.FontScale;
        tmp.color      = Color.white;
        tmp.alignment  = TextAlignmentOptions.TopLeft;
        tmp.extraPadding = true;
        tmp.richText   = true;
        // Initial static text shown before first Update
        tmp.text = "Move: R.stick XZ  |  L.stick Y\nRotate: L.stick\nQR Snap: A  |  Confirm: B";

        // ── Live HUD ──────────────────────────────────────────────────
        var hud = root.AddComponent<WorkspacePlacementHUD>();
        hud.Initialize(tmp, controller, qrSnapper);

        return root;
    }
}
