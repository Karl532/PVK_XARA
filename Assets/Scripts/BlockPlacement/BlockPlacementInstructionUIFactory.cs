using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds the on-screen instruction UI for block placement mode.
/// Separated from BlockPlacementController so UI layout logic lives in one place.
/// </summary>
public static class BlockPlacementInstructionUIFactory
{
    /// <summary>
    /// Creates the instruction canvas and panel, parented to nothing (root-level).
    /// Returns the root canvas GameObject so the caller can destroy it when done.
    /// </summary>
    public static GameObject CreateInstructionUI(Camera xrCamera)
    {
        GameObject instructionCanvas = new GameObject("BlockPlacementInstructions");
        var canvas = instructionCanvas.AddComponent<Canvas>();
        // ScreenSpaceCamera works in VR; ScreenSpaceOverlay does not render in the headset
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        Camera cam = xrCamera != null ? xrCamera : Camera.main;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;
        canvas.sortingOrder = 100;

        var scaler = instructionCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        instructionCanvas.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = instructionCanvas.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        // Left-aligned top panel
        GameObject panel = new GameObject("InstructionPanel");
        panel.transform.SetParent(instructionCanvas.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(40, -40);
        panelRect.sizeDelta = new Vector2(480, 120);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.12f, 0.9f);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 24);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        GameObject textObj = new GameObject("InstructionText");
        textObj.transform.SetParent(panel.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Move: Right stick (XZ) + Left stick (Y)\nPlace & Exit: B";
        text.fontSize = 28;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.extraPadding = true;

        return instructionCanvas;
    }
}

