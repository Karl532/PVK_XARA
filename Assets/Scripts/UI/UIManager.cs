using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class UIManager : MonoBehaviour
{
    [Header("UI Configuration")]
    [SerializeField] private float distanceFromCamera = 2.5f;
    [SerializeField] private Vector3 uiRotation = new Vector3(0, 0, 0);
    
    [Header("Panel Settings")]
    [SerializeField] private float panelWidth = 2.0f;
    [SerializeField] private float panelHeight = 2.5f;
    [SerializeField] private float curveRadius = 3.0f;
    [SerializeField] private int curveSegments = 40;
    
    [Header("Visual Style")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    [SerializeField] private Color accentColor = new Color(0.3f, 0.5f, 0.9f, 1f);
    [SerializeField] private Color textColor = Color.white;
    
    private GameObject canvasObject;
    private Canvas canvas;
    private GameObject curvedPanelMesh;
    private GameObject contentPanel;
    
    void Start()
    {
        CreateCompleteUI();
    }
    
    void CreateCompleteUI()
    {
        CreateCanvas();
        
        
        CreateContentPanel();
        
        CreateUIContent();
        
    }
    
    void CreateCanvas()
    {
        canvasObject = new GameObject("VR_UI_Canvas");
        canvasObject.transform.SetParent(transform);
        
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;
        
        GraphicRaycaster raycaster = canvasObject.AddComponent<GraphicRaycaster>();
        
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(3000, 3000);
        canvasRect.position = new Vector2(-0.4f, 1f);
        canvasRect.localScale = Vector3.one * 0.001f;
    }


    void CreateContentPanel()
    {
        contentPanel = new GameObject("ContentPanel");
        contentPanel.transform.SetParent(canvasObject.transform);

        RectTransform rect = contentPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(3000, 3000);
        rect.localPosition = Vector3.zero;
        rect.localScale = Vector3.one;

        VerticalLayoutGroup layout = contentPanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20;
        layout.padding = new RectOffset(50, 50, 80, 80);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;  // Changed to false
        layout.childForceExpandHeight = false; // Changed to false
    }

    void CreateUIContent()
    {
        // Create header
        GameObject headerGO = CreateSection("Header", 120);
        UISection headerSection = headerGO.AddComponent<UISection>();
        headerSection.CreateHeader("User Settings", 120, accentColor, textColor);

        // Create tab container
        GameObject tabContainer = CreateTabContainer();
        UITabSystem tabSystem = tabContainer.AddComponent<UITabSystem>();

        // Create tabs
        CreateTabsWithContent(tabSystem);

        tabSystem.Initialize();
        
    }

    GameObject CreateSection(string name, float height)
    {
        GameObject section = new GameObject($"Section_{name}");
        section.transform.SetParent(contentPanel.transform, false);  // Add false

        RectTransform rect = section.AddComponent<RectTransform>();

        // Stretch horizontally, fixed height
        rect.anchorMin = new Vector2(0, 0.5f);      // Left edge
        rect.anchorMax = new Vector2(1, 0.5f);      // Right edge (stretch)
        rect.pivot = new Vector2(0f, 0.5f);       // Center pivot
        rect.anchoredPosition = Vector2.zero;       // No offset
        rect.sizeDelta = new Vector2(0, height);    // Width=0 (auto), height=fixed

        rect.localScale = Vector3.one;

        LayoutElement layoutElement = section.AddComponent<LayoutElement>();
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;

        return section;
    }
    GameObject CreateTabContainer()
    {
        GameObject container = new GameObject("TabContainer");
        container.transform.SetParent(contentPanel.transform, false);  // Add false!

        RectTransform rect = container.AddComponent<RectTransform>();

        // Add proper anchoring
        rect.anchorMin = new Vector2(0.5f, 0.5f);   // Center anchor
        rect.anchorMax = new Vector2(0.5f, 0.5f);   // Center anchor
        rect.pivot = new Vector2(0f, 0.5f);       // Center pivot
        rect.anchoredPosition = Vector2.zero;       // No offset from center
        rect.sizeDelta = new Vector2(1700, 620);    // Explicit size (match your section width)

        rect.localScale = Vector3.one;

        LayoutElement layout = container.AddComponent<LayoutElement>();
        layout.minHeight = 620;
        layout.preferredHeight = 620;
        layout.flexibleHeight = 0;

        VerticalLayoutGroup vLayout = container.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 20;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = false;
        vLayout.childForceExpandHeight = false;
        vLayout.childAlignment = TextAnchor.UpperCenter;  // Add this for consistency

        return container;
    }

    void CreateTabsWithContent(UITabSystem tabSystem)
    {
        GameObject tabButtonsContainer = new GameObject("TabButtons");
        tabButtonsContainer.transform.SetParent(tabSystem.transform, false);

        RectTransform tabButtonsRect = tabButtonsContainer.AddComponent<RectTransform>();

        // For left-aligned buttons:
        tabButtonsRect.anchorMin = new Vector2(0, 1);     // Top-left
        tabButtonsRect.anchorMax = new Vector2(1, 1);     // Top-right (stretch width)
        tabButtonsRect.pivot = new Vector2(0, 1);         // Changed: pivot at TOP-LEFT
        tabButtonsRect.anchoredPosition = Vector2.zero;
        tabButtonsRect.sizeDelta = new Vector2(0, 100);
        tabButtonsRect.localScale = Vector3.one;

        tabButtonsRect.localScale = Vector3.one;

        LayoutElement tabButtonsLayoutElement = tabButtonsContainer.AddComponent<LayoutElement>();
        tabButtonsLayoutElement.minHeight = 100;
        tabButtonsLayoutElement.preferredHeight = 100;
        tabButtonsLayoutElement.flexibleHeight = 0;

        HorizontalLayoutGroup tabButtonsLayout = tabButtonsContainer.AddComponent<HorizontalLayoutGroup>();
        tabButtonsLayout.spacing = 10;
        tabButtonsLayout.padding = new RectOffset(0, 0, 0, 0);
        tabButtonsLayout.childAlignment = TextAnchor.MiddleLeft;
        tabButtonsLayout.childControlWidth = false;
        tabButtonsLayout.childControlHeight = false;
        tabButtonsLayout.childForceExpandWidth = false;
        tabButtonsLayout.childForceExpandHeight = false;


        // Content container
        GameObject contentContainer = new GameObject("TabContents");
        contentContainer.transform.SetParent(tabSystem.transform);

        RectTransform contentRect = contentContainer.AddComponent<RectTransform>();
        contentRect.localScale = Vector3.one;
        contentRect.localPosition = Vector3.zero;

        LayoutElement contentLayout = contentContainer.AddComponent<LayoutElement>();
        contentLayout.minHeight = 500;  
        contentLayout.preferredHeight = 500;
        contentLayout.flexibleHeight = 0;

        VerticalLayoutGroup contentVLayout = contentContainer.AddComponent<VerticalLayoutGroup>();
        contentVLayout.spacing = 30;
        contentVLayout.padding = new RectOffset(50, 50, 50, 50);

        UITabButton tab1Button = CreateTabButton(tabButtonsContainer.transform, "Tab 1");
        GameObject tab1Content = CreateLoginTab(contentContainer.transform);
        tabSystem.AddTab(tab1Button, tab1Content);

        UITabButton tab2Button = CreateTabButton(tabButtonsContainer.transform, "Tab 2");
        GameObject tab2Content = CreateProfileTab(contentContainer.transform);
        tabSystem.AddTab(tab2Button, tab2Content);

        UITabButton tab3Button = CreateTabButton(tabButtonsContainer.transform, "Tab 3");
        GameObject tab3Content = CreateSettingsTab(contentContainer.transform);
        tabSystem.AddTab(tab3Button, tab3Content);
    }

    UITabButton CreateTabButton(Transform parent, string label)
    {
        GameObject buttonGO = new GameObject($"Tab_{label}");
        buttonGO.transform.SetParent(parent);

        UITabButton tabButton = buttonGO.AddComponent<UITabButton>();

        // Changed: Use pixel units instead of world units
        Vector2 buttonSize = new Vector2(500, 100); // 500x100 pixels
        tabButton.CreateTabButton(label, accentColor, new Color(0.2f, 0.2f, 0.25f, 0.8f), buttonSize);

        LayoutElement layout = buttonGO.AddComponent<LayoutElement>();
        layout.minWidth = buttonSize.x;
        layout.preferredWidth = buttonSize.x;
        layout.minHeight = buttonSize.y;
        layout.preferredHeight = buttonSize.y;
        layout.flexibleWidth = 0;
        layout.flexibleHeight = 0;

        buttonGO.transform.localScale = Vector3.one;

        return tabButton;
    }


    GameObject CreateLoginTab(Transform parent)
    {
        GameObject content = new GameObject("LoginContent");
        content.transform.SetParent(parent);

        RectTransform rect = content.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localPosition = Vector3.zero;
        rect.localScale = Vector3.one;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 30;
        layout.padding = new RectOffset(50, 50, 50, 50);


        // Add login fields
        GameObject inputone = CreateSection("Input 1", 200);
        inputone.transform.SetParent(content.transform);
        UIInputField usernameField = inputone.AddComponent<UIInputField>();
        usernameField.CreateInputField("Input 1", "Enter username", accentColor);

        GameObject inputtwo = CreateSection("Input 2", 200);
        inputtwo.transform.SetParent(content.transform);
        UIInputField passwordField = inputtwo.AddComponent<UIInputField>();
        passwordField.CreateInputField("Input 2", "••••••••", accentColor, true);

        return content;
    }

    GameObject CreateProfileTab(Transform parent)
    {
        GameObject content = new GameObject("ProfileContent");
        content.transform.SetParent(parent);

        RectTransform rect = content.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localPosition = Vector3.zero;
        rect.localScale = Vector3.one;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 30;
        layout.padding = new RectOffset(50, 50, 50, 50);

        // Add profile fields
        GameObject nameSection = CreateSection("Name", 200);
        nameSection.transform.SetParent(content.transform);
        UIInputField nameField = nameSection.AddComponent<UIInputField>();
        nameField.CreateInputField("Full Name", "John Doe", accentColor);

        GameObject emailSection = CreateSection("Email", 200);
        emailSection.transform.SetParent(content.transform);
        UIInputField emailField = emailSection.AddComponent<UIInputField>();
        emailField.CreateInputField("Email", "john@example.com", accentColor);

        return content;
    }

    GameObject CreateSettingsTab(Transform parent)
    {
        GameObject content = new GameObject("SettingsContent");
        content.transform.SetParent(parent);

        RectTransform rect = content.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localPosition = Vector3.zero;
        rect.localScale = Vector3.one;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 30;
        layout.padding = new RectOffset(50, 50, 50, 50);

        // Add toggle options or settings
        GameObject notificationSection = CreateSection("Notifications", 200);
        notificationSection.transform.SetParent(content.transform);
        // Add toggles, sliders, etc.

        return content;
    }

    void CreateDivider()
    {
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(contentPanel.transform);
        
        RectTransform rect = divider.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1600, 3);
        
        Image line = divider.AddComponent<Image>();
        line.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.3f);
        
        LayoutElement layoutElement = divider.AddComponent<LayoutElement>();
        layoutElement.minHeight = 40;
        layoutElement.preferredHeight = 40;
    }

    private UIButton CreateButton(Transform parent, string name, string buttonText, UnityEngine.Events.UnityAction onClick, Color buttonColor, Color textColor, Vector2 size)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);

        UIButton buttonComp = buttonGO.AddComponent<UIButton>();
        buttonComp.CreateButton(buttonText, onClick, buttonColor, textColor, size);

        LayoutElement layout = buttonGO.AddComponent<LayoutElement>();
        layout.preferredHeight = size.y;
        layout.minHeight = size.y;

        return buttonComp;
    }


}
