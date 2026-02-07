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
        
        // Add vertical layout group for automatic spacing
        VerticalLayoutGroup layout = contentPanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20;
        layout.padding = new RectOffset(50, 50, 80, 80);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
    }

    void CreateUIContent()
    {
        // Create header section
        GameObject headerGO = CreateSection("Header", 120);
        UISection headerSection = headerGO.AddComponent<UISection>();
        headerSection.CreateHeader("User Login", 120, accentColor, textColor);

        // Create input sections
        GameObject usernameSection = CreateSection("Username", 200);
        UIInputField usernameField = usernameSection.AddComponent<UIInputField>();
        usernameField.CreateInputField("Username", "Enter your username", accentColor);

        GameObject emailSection = CreateSection("Email", 200);
        UIInputField emailField = emailSection.AddComponent<UIInputField>();
        emailField.CreateInputField("Email", "your@email.com", accentColor);

        GameObject passwordSection = CreateSection("Password", 200);
        UIInputField passwordField = passwordSection.AddComponent<UIInputField>();
        passwordField.CreateInputField("Password", "••••••••", accentColor, true);

        // Create divider
        //CreateDivider();

        //GameObject actionsSection = CreateSection("Actions", 150);
    }

    GameObject CreateSection(string name, float height)
    {
        GameObject section = new GameObject($"Section_{name}");
        section.transform.SetParent(contentPanel.transform);

        RectTransform rect = section.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1700, height);
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;

        LayoutElement layoutElement = section.AddComponent<LayoutElement>();
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;

        return section;
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
