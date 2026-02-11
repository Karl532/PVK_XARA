using System;
using UnityEngine;
using UnityEngine.UI;
using KeyBinding;
using KeyBinding.Handlers;

public class UIManager : MonoBehaviour
{
    [Header("Key Binds")]
    [SerializeField]
    [Tooltip("Implements keybind actions (ToggleWorkspacePlacement, etc.). Add to this GameObject and drag here.")]
    private KeyBindActions keyBindActions;

    [Header("Visual Style")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    [SerializeField] private Color accentColor = new Color(0.3f, 0.5f, 0.9f, 1f);
    [SerializeField] private Color secondaryColor = new Color(0.15f, 0.15f, 0.2f, 0.9f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.18f, 0.18f, 0.23f, 0.85f);
    [SerializeField] private float cornerRadius = 25f;
    [SerializeField] private bool useShadows = true;
    [SerializeField] private Color shadowColor = new Color(0, 0, 0, 0.5f);

    private GameObject canvasObject;

    void Start()
    {
        ApplyThemeFromSettings();
        BuildUI();
    }

    void ApplyThemeFromSettings()
    {
        Settings settings = SettingsManager.Instance != null ? SettingsManager.Instance.settings : null;
        if (settings == null) return;

        if (settings.uiLightMode)
        {
            backgroundColor = new Color(0.96f, 0.96f, 0.98f, 0.98f);
            accentColor = new Color(0.15f, 0.35f, 0.75f, 1f);
            secondaryColor = new Color(0.88f, 0.88f, 0.91f, 0.95f);
            textColor = new Color(0.08f, 0.08f, 0.12f, 1f);
            inactiveColor = new Color(0.75f, 0.75f, 0.8f, 0.95f);
            shadowColor = new Color(0, 0, 0, 0.15f);
        }
        else
        {
            backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            accentColor = new Color(0.3f, 0.5f, 0.9f, 1f);
            secondaryColor = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            textColor = Color.white;
            inactiveColor = new Color(0.18f, 0.18f, 0.23f, 0.85f);
            shadowColor = new Color(0, 0, 0, 0.5f);
        }
    }

    public void RebuildUI()
    {
        int tabToRestore = -1;
        Vector3 positionToRestore = Vector3.zero;
        Quaternion rotationToRestore = Quaternion.identity;
        Vector3 scaleToRestore = Vector3.one;
        bool hasTransformToRestore = false;

        if (canvasObject != null)
        {
            var tabSystem = canvasObject.GetComponentInChildren<UITabSystem>();
            if (tabSystem != null)
                tabToRestore = tabSystem.GetActiveTabIndex();

            // Save the current transform state before destroying
            Transform canvasTransform = canvasObject.transform;
            positionToRestore = canvasTransform.position;
            rotationToRestore = canvasTransform.rotation;
            scaleToRestore = canvasTransform.localScale;
            hasTransformToRestore = true;

            Destroy(canvasObject);
        }

        ApplyThemeFromSettings();
        BuildUI();

        if (canvasObject != null)
        {
            // Restore the transform state
            if (hasTransformToRestore)
            {
                Transform canvasTransform = canvasObject.transform;
                canvasTransform.position = positionToRestore;
                canvasTransform.rotation = rotationToRestore;
                canvasTransform.localScale = scaleToRestore;
            }

            // Restore the active tab
            if (tabToRestore >= 0)
            {
                var tabSystem = canvasObject.GetComponentInChildren<UITabSystem>();
                if (tabSystem != null)
                    tabSystem.SelectTab(tabToRestore);
            }
        }
    }

    void BuildUI()
    {
        // Ensure keybind handlers and workspace bounds controller exist
        if (GetComponent<ToggleSettingsPanelHandler>() == null)
            gameObject.AddComponent<ToggleSettingsPanelHandler>();
        if (GetComponent<ToggleBlockPlacementHandler>() == null)
            gameObject.AddComponent<ToggleBlockPlacementHandler>();
        if (GetComponent<WorkspacePlacementController>() == null)
            gameObject.AddComponent<WorkspacePlacementController>();

        // Canvas
        canvasObject = new GameObject("VR_UI_Canvas");
        canvasObject.transform.SetParent(transform);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;

        // Root canvas group so we can easily show/hide and fade via ToggleSettingsPanelHandler.
        CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Enable XR / pointer interaction with the canvas in a safe, reflection-based way.
        TryAddTrackedDeviceGraphicRaycaster(canvasObject);

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(3000, 3000);
        canvasRect.position = new Vector2(-0.4f, 1f);
        canvasRect.localScale = Vector3.one * 0.001f;

        // Add a BoxCollider so XR rays / grabs can hit the UI root.
        // Values are in local space; world size is scaled by the canvas transform.
        BoxCollider rootCollider = canvasObject.AddComponent<BoxCollider>();
        rootCollider.center = Vector3.zero;
        rootCollider.size = new Vector3(canvasRect.sizeDelta.x, canvasRect.sizeDelta.y, 10f);

        // Allow grabbing / moving / rotating the entire UI in world space using XR controllers.
        TryAddXRGrabInteractable(canvasObject);

        // Keep the UI upright and within a comfortable distance of the player.
        TryAddUIKeepUpright(canvasObject);

        // Background panel
        CreateBackgroundPanel();

        // Content panel
        GameObject contentPanel = CreateContentPanel();

        // Header
        GameObject headerGO = UILayoutFactory.CreateLayoutSection(contentPanel.transform, "Header", 140);
        UILayoutFactory.CreateHeader(headerGO, "Settings", 140, accentColor, textColor, cornerRadius);

        // Keybind registry root (parented to UIManager so it stays active; bindings work even when UI is hidden)
        GameObject keyBindRoot = new GameObject("KeyBindRegistry");
        keyBindRoot.transform.SetParent(transform, false);
        KeyBindRegistry.SetRoot(keyBindRoot.transform);

        // Tab system
        UIStyle style = new UIStyle
        {
            accentColor = accentColor,
            inactiveColor = inactiveColor,
            textColor = textColor,
            cornerRadius = cornerRadius,
            useShadows = useShadows
        };

        var actions = keyBindActions != null ? keyBindActions : GetComponent<KeyBindActions>();
        if (actions == null) actions = gameObject.AddComponent<KeyBindActions>();

        style.keyBindActions = actions;

        UITabSystem.Build(contentPanel.transform, style,
            new TabDefinition { label = "Workspace", createContent = WorkspaceSettingsTab.Create },
            new TabDefinition { label = "Model", createContent = ModelSettingsTab.Create },
            new TabDefinition { label = "Tracking", createContent = TrackingTab.Create },
            new TabDefinition { label = "UI", createContent = UICustomizationTab.Create },
            new TabDefinition { label = "Load model", createContent = FilesTab.Create }
        );
    }

    void CreateBackgroundPanel()
    {
        GameObject bgPanel = new GameObject("BackgroundPanel");
        bgPanel.transform.SetParent(canvasObject.transform);

        RectTransform bgRect = bgPanel.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(3100, 3100);
        bgRect.localPosition = Vector3.zero;
        bgRect.localScale = Vector3.one;

        Image bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = backgroundColor;

        RoundedImage rounded = bgPanel.AddComponent<RoundedImage>();
        rounded.SetRadius(cornerRadius);

        if (useShadows)
        {
            Shadow shadow = bgPanel.AddComponent<Shadow>();
            shadow.effectColor = shadowColor;
            shadow.effectDistance = new Vector2(5, -5);
            shadow.useGraphicAlpha = true;
        }

        Outline glow = bgPanel.AddComponent<Outline>();
        glow.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.3f);
        glow.effectDistance = new Vector2(2, 2);
    }

    GameObject CreateContentPanel()
    {
        GameObject contentPanel = new GameObject("ContentPanel");
        contentPanel.transform.SetParent(canvasObject.transform);

        RectTransform rect = contentPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(3000, 3000);
        rect.localPosition = Vector3.zero;
        rect.localScale = Vector3.one;

        VerticalLayoutGroup layout = contentPanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 30;
        layout.padding = new RectOffset(80, 80, 100, 100);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return contentPanel;
    }

    private void TryAddTrackedDeviceGraphicRaycaster(GameObject target)
    {
        // XR Interaction Toolkit 2.x namespace
        var type = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (type == null) return;
        if (target.GetComponent(type) != null) return;
        target.AddComponent(type);
    }

    private void TryAddXRGrabInteractable(GameObject target)
    {
        // Try both pre-3.0 and 3.x namespaces for XRGrabInteractable.
        var type = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
        if (type == null)
        {
            type = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
        }

        if (type == null) return;
        if (target.GetComponent(type) != null) return;

        // Ensure we have a Rigidbody configured for kinematic, no-gravity motion
        // so the UI doesn't fall due to physics or flip uncontrollably.
        var rb = target.GetComponent<Rigidbody>();
        if (rb == null)
            rb = target.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;
        // Only allow yaw rotation so the panel can't be grabbed and turned upside-down.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        var grab = target.AddComponent(type);

        // Best-effort: set movementType to VelocityTracking if available, so it follows controllers naturally.
        try
        {
            var baseType = type.BaseType;
            while (baseType != null && baseType.FullName != null && !baseType.FullName.Contains("XRBaseInteractable"))
            {
                baseType = baseType.BaseType;
            }

            if (baseType != null)
            {
                var movementTypeProp = baseType.GetProperty("movementType");
                if (movementTypeProp != null && movementTypeProp.CanWrite)
                {
                    var movementEnumType = movementTypeProp.PropertyType;
                    var velocityValue = Enum.Parse(movementEnumType, "VelocityTracking", ignoreCase: true);
                    movementTypeProp.SetValue(grab, velocityValue);
                }
            }
        }
        catch
        {
            // Ignore – defaults are usually fine; this just tweaks feel when available.
        }
    }

    private void TryAddUIKeepUpright(GameObject target)
    {
        // Use reflection so this compiles even if UIKeepUpright lives in a different assembly/namespace.
        var type = Type.GetType("UIKeepUpright");
        if (type == null)
        {
            type = Type.GetType("UI.Utils.UIKeepUpright");
        }

        if (type == null) return;
        if (target.GetComponent(type) != null) return;

        target.AddComponent(type);
    }

}
