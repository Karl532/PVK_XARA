using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public struct UIStyle
{
    public Color accentColor;
    public Color inactiveColor;
    public float cornerRadius;
    public bool useShadows;
}

public struct TabDefinition
{
    public string label;
    public Func<Transform, Color, GameObject> createContent;
}

public class UITabSystem : MonoBehaviour
{
    private List<UITabButton> tabButtons = new List<UITabButton>();
    private List<GameObject> tabContents = new List<GameObject>();
    private int currentTabIndex = 0;

    public static GameObject Build(Transform parent, UIStyle style, params TabDefinition[] tabs)
    {
        // Tab container
        GameObject container = new GameObject("TabContainer");
        container.transform.SetParent(parent, false);

        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(2800, 680);
        rect.localScale = Vector3.one;

        LayoutElement layoutEl = container.AddComponent<LayoutElement>();
        layoutEl.minHeight = 680;
        layoutEl.preferredHeight = 680;
        layoutEl.flexibleHeight = 0;

        VerticalLayoutGroup vLayout = container.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 0;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = false;
        vLayout.childForceExpandHeight = false;
        vLayout.childAlignment = TextAnchor.UpperCenter;

        UITabSystem tabSystem = container.AddComponent<UITabSystem>();

        // Tab buttons row
        GameObject tabButtonsContainer = CreateTabButtonsContainer(container.transform);

        // Content container
        GameObject contentContainer = CreateContentContainer(container.transform);

        // Register each tab
        foreach (var tab in tabs)
        {
            UITabButton button = CreateTabButton(tabButtonsContainer.transform, tab.label, style);
            GameObject content = tab.createContent(contentContainer.transform, style.accentColor);
            tabSystem.AddTab(button, content);
        }

        tabSystem.Initialize();
        return container;
    }

    static GameObject CreateTabButtonsContainer(Transform parent)
    {
        GameObject tabButtonsContainer = new GameObject("TabButtons");
        tabButtonsContainer.transform.SetParent(parent, false);

        RectTransform tabButtonsRect = tabButtonsContainer.AddComponent<RectTransform>();
        tabButtonsRect.anchorMin = new Vector2(0, 1);
        tabButtonsRect.anchorMax = new Vector2(1, 1);
        tabButtonsRect.pivot = new Vector2(0, 1);
        tabButtonsRect.anchoredPosition = Vector2.zero;
        tabButtonsRect.sizeDelta = new Vector2(0, 110);
        tabButtonsRect.localScale = Vector3.one;

        LayoutElement layoutEl = tabButtonsContainer.AddComponent<LayoutElement>();
        layoutEl.minHeight = 110;
        layoutEl.preferredHeight = 110;
        layoutEl.flexibleHeight = 0;

        HorizontalLayoutGroup layout = tabButtonsContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 15;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return tabButtonsContainer;
    }

    static GameObject CreateContentContainer(Transform parent)
    {
        GameObject contentContainer = new GameObject("TabContents");
        contentContainer.transform.SetParent(parent, false);

        RectTransform contentRect = contentContainer.AddComponent<RectTransform>();
        contentRect.localScale = Vector3.one;
        contentRect.localPosition = Vector3.zero;

        Image contentBg = contentContainer.AddComponent<Image>();
        contentBg.color = new Color(0, 0, 0, 0);

        LayoutElement layoutEl = contentContainer.AddComponent<LayoutElement>();
        layoutEl.minHeight = 540;
        layoutEl.preferredHeight = 540;
        layoutEl.flexibleHeight = 0;

        VerticalLayoutGroup layout = contentContainer.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 30;
        layout.padding = new RectOffset(60, 60, 60, 60);

        return contentContainer;
    }

    static UITabButton CreateTabButton(Transform parent, string label, UIStyle style)
    {
        GameObject buttonGO = new GameObject($"Tab_{label}");
        buttonGO.transform.SetParent(parent, false);

        UITabButton tabButton = buttonGO.AddComponent<UITabButton>();

        Vector2 buttonSize = new Vector2(720, 110);
        tabButton.CreateTabButton(label, style.accentColor, style.inactiveColor, buttonSize);

        Image btnImage = buttonGO.GetComponent<Image>();
        if (btnImage != null)
        {
            RoundedImage btnRounded = buttonGO.AddComponent<RoundedImage>();
            btnRounded.SetRadius(style.cornerRadius * 0.7f);
        }

        if (style.useShadows)
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

    public void AddTab(UITabButton button, GameObject content)
    {
        int index = tabButtons.Count;
        tabButtons.Add(button);
        tabContents.Add(content);

        button.GetButton().onClick.AddListener(() => SelectTab(index));

        content.SetActive(false);
    }

    public void SelectTab(int index)
    {
        if (index < 0 || index >= tabButtons.Count) return;

        currentTabIndex = index;

        for (int i = 0; i < tabButtons.Count; i++)
        {
            tabButtons[i].SetActive(i == index);
            tabContents[i].SetActive(i == index);
        }
    }

    public void Initialize()
    {
        if (tabButtons.Count > 0)
        {
            SelectTab(0);
        }
    }
}
