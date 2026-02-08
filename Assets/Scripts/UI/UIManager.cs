using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;
using System.Collections.Generic;

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
    [SerializeField] private Color secondaryColor = new Color(0.15f, 0.15f, 0.2f, 0.9f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float cornerRadius = 25f;
    [SerializeField] private bool useShadows = true;
    [SerializeField] private Color shadowColor = new Color(0, 0, 0, 0.5f);

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
        CreateBackgroundPanel();
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

        // TrackedDeviceGraphicRaycaster handles XR ray interactor UI events
        canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(3000, 3000);
        canvasRect.position = new Vector2(-0.4f, 1f);
        canvasRect.localScale = Vector3.one * 0.001f;
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

        // Add rounded corners
        RoundedImage rounded = bgPanel.AddComponent<RoundedImage>();
        rounded.SetRadius(cornerRadius);

        // Add shadow/glow effect
        if (useShadows)
        {
            Shadow shadow = bgPanel.AddComponent<Shadow>();
            shadow.effectColor = shadowColor;
            shadow.effectDistance = new Vector2(5, -5);
            shadow.useGraphicAlpha = true;
        }

        // Add subtle outer glow
        Outline glow = bgPanel.AddComponent<Outline>();
        glow.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.3f);
        glow.effectDistance = new Vector2(2, 2);
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
        layout.spacing = 30; // Increased spacing
        layout.padding = new RectOffset(80, 80, 100, 100); // More padding
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    void CreateUIContent()
    {
        // Create header with gradient
        GameObject headerGO = CreateSection("Header", 140);
        UISection headerSection = headerGO.AddComponent<UISection>();
        headerSection.CreateHeader("Settings", 140, accentColor, textColor, cornerRadius);

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
        section.transform.SetParent(contentPanel.transform, false);

        RectTransform rect = section.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(1, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0, height);
        rect.localScale = Vector3.one;

        LayoutElement layoutElement = section.AddComponent<LayoutElement>();
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;

        return section;
    }

    GameObject CreateTabContainer()
    {
        GameObject container = new GameObject("TabContainer");
        container.transform.SetParent(contentPanel.transform, false);

        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(2800, 680);
        rect.localScale = Vector3.one;

        LayoutElement layout = container.AddComponent<LayoutElement>();
        layout.minHeight = 680;
        layout.preferredHeight = 680;
        layout.flexibleHeight = 0;

        VerticalLayoutGroup vLayout = container.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 0;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = false;
        vLayout.childForceExpandHeight = false;
        vLayout.childAlignment = TextAnchor.UpperCenter;

        return container;
    }

    void CreateTabsWithContent(UITabSystem tabSystem)
    {
        // Tab buttons container
        GameObject tabButtonsContainer = new GameObject("TabButtons");
        tabButtonsContainer.transform.SetParent(tabSystem.transform, false);

        RectTransform tabButtonsRect = tabButtonsContainer.AddComponent<RectTransform>();
        tabButtonsRect.anchorMin = new Vector2(0, 1);
        tabButtonsRect.anchorMax = new Vector2(1, 1);
        tabButtonsRect.pivot = new Vector2(0, 1);
        tabButtonsRect.anchoredPosition = Vector2.zero;
        tabButtonsRect.sizeDelta = new Vector2(0, 110);
        tabButtonsRect.localScale = Vector3.one;

        LayoutElement tabButtonsLayoutElement = tabButtonsContainer.AddComponent<LayoutElement>();
        tabButtonsLayoutElement.minHeight = 110;
        tabButtonsLayoutElement.preferredHeight = 110;
        tabButtonsLayoutElement.flexibleHeight = 0;

        HorizontalLayoutGroup tabButtonsLayout = tabButtonsContainer.AddComponent<HorizontalLayoutGroup>();
        tabButtonsLayout.spacing = 15; // Increased spacing
        tabButtonsLayout.padding = new RectOffset(0, 0, 0, 0);
        tabButtonsLayout.childAlignment = TextAnchor.MiddleLeft;
        tabButtonsLayout.childControlWidth = false;
        tabButtonsLayout.childControlHeight = false;
        tabButtonsLayout.childForceExpandWidth = false;
        tabButtonsLayout.childForceExpandHeight = false;

        // Content container with rounded background
        GameObject contentContainer = new GameObject("TabContents");
        contentContainer.transform.SetParent(tabSystem.transform, false);

        RectTransform contentRect = contentContainer.AddComponent<RectTransform>();
        contentRect.localScale = Vector3.one;
        contentRect.localPosition = Vector3.zero;

        // Transparent - blends with main panel background
        Image contentBg = contentContainer.AddComponent<Image>();
        contentBg.color = new Color(0, 0, 0, 0);

        LayoutElement contentLayout = contentContainer.AddComponent<LayoutElement>();
        contentLayout.minHeight = 540;
        contentLayout.preferredHeight = 540;
        contentLayout.flexibleHeight = 0;

        VerticalLayoutGroup contentVLayout = contentContainer.AddComponent<VerticalLayoutGroup>();
        contentVLayout.spacing = 30;
        contentVLayout.padding = new RectOffset(60, 60, 60, 60);

        // Create tabs
        UITabButton tab1Button = CreateTabButton(tabButtonsContainer.transform, "Tab 1");
        GameObject tab1Content = CreateTab(contentContainer.transform);
        tabSystem.AddTab(tab1Button, tab1Content);

        UITabButton tab2Button = CreateTabButton(tabButtonsContainer.transform, "Tab 2");
        GameObject tab2Content = CreateTab(contentContainer.transform);
        tabSystem.AddTab(tab2Button, tab2Content);

        UITabButton tab3Button = CreateTabButton(tabButtonsContainer.transform, "Tab 3");
        GameObject tab3Content = CreateTab(contentContainer.transform);
        tabSystem.AddTab(tab3Button, tab3Content);
    }

    UITabButton CreateTabButton(Transform parent, string label)
    {
        GameObject buttonGO = new GameObject($"Tab_{label}");
        buttonGO.transform.SetParent(parent, false);

        UITabButton tabButton = buttonGO.AddComponent<UITabButton>();

        Vector2 buttonSize = new Vector2(720, 110);
        Color inactiveColor = new Color(0.18f, 0.18f, 0.23f, 0.85f);
        tabButton.CreateTabButton(label, accentColor, inactiveColor, buttonSize);

        // Add rounded corners to tab button
        Image btnImage = buttonGO.GetComponent<Image>();
        if (btnImage != null)
        {
            RoundedImage btnRounded = buttonGO.AddComponent<RoundedImage>();
            btnRounded.SetRadius(cornerRadius * 0.7f);
        }

        // Add subtle shadow
        if (useShadows)
        {
            Shadow btnShadow = buttonGO.AddComponent<Shadow>();
            btnShadow.effectColor = new Color(0, 0, 0, 0.3f);
            btnShadow.effectDistance = new Vector2(0, -2);
        }

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

    GameObject CreateTab(Transform parent)
    {
        GameObject content = new GameObject("LoginContent");
        content.transform.SetParent(parent, false);

        RectTransform rect = content.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localPosition = Vector3.zero;
        rect.localScale = Vector3.one;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 35;
        layout.padding = new RectOffset(50, 50, 50, 50);

        // Row 1: Height and Width side by side
        GameObject row1 = CreateHorizontalRow(content.transform, 220, 30, "BlockDimensions1");

        GameObject blockHeightInput = CreateInputSection("Block height", 220, 1300f);
        blockHeightInput.transform.SetParent(row1.transform, false);
        UIInputField blockHeightField = blockHeightInput.AddComponent<UIInputField>();
        blockHeightField.CreateInputField("Block height", "Enter height", accentColor);

        GameObject blockWidthInput = CreateInputSection("Block width", 220, 1300f);
        blockWidthInput.transform.SetParent(row1.transform, false);
        UIInputField blockWidthField = blockWidthInput.AddComponent<UIInputField>();
        blockWidthField.CreateInputField("Block width", "Enter width", accentColor, false);

        // Row 2: Length input + dropdown side by side
        GameObject row2 = CreateHorizontalRow(content.transform, 220, 30, "BlockDimensions2");

        GameObject blockLengthInput = CreateInputSection("Block length", 220, 1300f);
        blockLengthInput.transform.SetParent(row2.transform, false);
        UIInputField blockLengthField = blockLengthInput.AddComponent<UIInputField>();
        blockLengthField.CreateInputField("Block length", "Enter length", accentColor, false);

        List<string> units = new List<string> { "Meters", "Centimeters", "Inches" };
        CreateDropdownElement(row2.transform, "Units", "Unit", units, 220, 1300f);

        // Row 3: Checkbox
        CreateCheckboxElement(content.transform, "AutoScale", "Auto-scale block", false, 120);

        return content;
    }

    GameObject CreateInputSection(string name, float height, float width = -1f)
    {
        GameObject section = new GameObject($"InputSection_{name}");
        section.transform.SetParent(contentPanel.transform, false);

        RectTransform rect = section.AddComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;

        LayoutElement layoutElement = section.AddComponent<LayoutElement>();
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;

        if (width > 0)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            layoutElement.minWidth = width;
            layoutElement.preferredWidth = width;
        }
        else
        {
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.sizeDelta = new Vector2(-100, height);
        }

        // Transparent background - blends with parent
        Image bg = section.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0);

        return section;
    }

    GameObject CreateHorizontalRow(Transform parent, float height, float spacing = 20f, string name = "Row")
    {
        GameObject row = new GameObject($"HorizontalRow_{name}");
        row.transform.SetParent(parent, false);

        RectTransform rect = row.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(1, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0, height);
        rect.localScale = Vector3.one;

        LayoutElement layoutElement = row.AddComponent<LayoutElement>();
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return row;
    }

    GameObject CreateCheckboxElement(Transform parent, string name, string label, bool defaultValue = false, float height = 120f, float width = -1f)
    {
        GameObject section = CreateInputSection(name, height, width);
        section.transform.SetParent(parent, false);
        UICheckbox checkbox = section.AddComponent<UICheckbox>();
        checkbox.CreateCheckbox(label, accentColor, defaultValue);
        return section;
    }

    GameObject CreateDropdownElement(Transform parent, string name, string label, List<string> options, float height = 220f, float width = -1f)
    {
        GameObject section = CreateInputSection(name, height, width);
        section.transform.SetParent(parent, false);
        UIDropdown dropdown = section.AddComponent<UIDropdown>();
        dropdown.CreateDropdown(label, options, accentColor);
        return section;
    }
}