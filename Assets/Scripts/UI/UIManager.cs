using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using KeyBinding;
using KeyBinding.Handlers;

public class UIManager : MonoBehaviour
{
    [Header("Key Binds")]
    [SerializeField]
    [Tooltip("Implements keybind actions (ToggleBlockPlacement, etc.). Add to this GameObject and drag here.")]
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
        if (canvasObject != null)
        {
            var tabSystem = canvasObject.GetComponentInChildren<UITabSystem>();
            if (tabSystem != null)
                tabToRestore = tabSystem.GetActiveTabIndex();
            Destroy(canvasObject);
        }

        ApplyThemeFromSettings();
        BuildUI();

        if (tabToRestore >= 0 && canvasObject != null)
        {
            var tabSystem = canvasObject.GetComponentInChildren<UITabSystem>();
            if (tabSystem != null)
                tabSystem.SelectTab(tabToRestore);
        }
    }

    void BuildUI()
    {
        // Ensure keybind handlers and block placement controller exist
        if (GetComponent<ToggleSettingsPanelHandler>() == null)
            gameObject.AddComponent<ToggleSettingsPanelHandler>();
        if (GetComponent<ToggleBlockPlacementHandler>() == null)
            gameObject.AddComponent<ToggleBlockPlacementHandler>();
        if (GetComponent<BlockPlacementController>() == null)
            gameObject.AddComponent<BlockPlacementController>();

        // Canvas
        canvasObject = new GameObject("VR_UI_Canvas");
        canvasObject.transform.SetParent(transform);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;

        canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(3000, 3000);
        canvasRect.position = new Vector2(-0.4f, 1f);
        canvasRect.localScale = Vector3.one * 0.001f;

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
            new TabDefinition { label = "Block", createContent = BlockSettingsTab.Create },
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
}
