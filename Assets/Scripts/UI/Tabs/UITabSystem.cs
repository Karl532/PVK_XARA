using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using UI.Utils;

public struct UIStyle
{
    public Color accentColor;
    public Color inactiveColor;
    public Color textColor;
    public float cornerRadius;
    public bool useShadows;
    public KeyBinding.KeyBindActions keyBindActions;
}

public struct TabDefinition
{
    public string label;
    public Func<Transform, UIStyle, GameObject> createContent;
}

public class UITabSystem : MonoBehaviour
{
    private const float TabFadeInDuration = 0.25f;

    private List<UITabButton> tabButtons = new List<UITabButton>();
    private List<GameObject> tabContents = new List<GameObject>();
    private List<CanvasGroup> tabContentCanvasGroups = new List<CanvasGroup>();
    private int currentTabIndex;
    private Coroutine fadeCoroutine;

    public static GameObject Build(Transform parent, UIStyle style, params TabDefinition[] tabs)
    {
        // Tab container
        GameObject container = UIPrimitives.CreateUIElement("TabContainer", parent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(2800, 680), null, null,
            new Vector2(0.5f, 0.5f), Vector2.zero);

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
            GameObject content = tab.createContent(contentContainer.transform, style);
            tabSystem.AddTab(button, content);
        }

        tabSystem.Initialize();
        return container;
    }

    static GameObject CreateTabButtonsContainer(Transform parent)
    {
        GameObject tabButtonsContainer = UIPrimitives.CreateUIElement("TabButtons", parent,
            new Vector2(0, 1), new Vector2(1, 1),
            null, new Vector2(0, -110), new Vector2(0, 0));

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
        GameObject contentContainer = UIPrimitives.CreateUIElement("TabContents", parent,
            Vector2.zero, Vector2.one);

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
        tabButton.CreateTabButton(label, style.accentColor, style.inactiveColor, buttonSize, 48f, style.textColor);

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

        CanvasGroup cg = content.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = content.AddComponent<CanvasGroup>();
        tabContentCanvasGroups.Add(cg);

        button.GetButton().onClick.AddListener(() => SelectTab(index));

        content.SetActive(false);
    }

    /// <summary>Returns the currently active tab index (0-based).</summary>
    public int GetActiveTabIndex() => currentTabIndex;

    public void SelectTab(int index)
    {
        if (index < 0 || index >= tabButtons.Count) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        int previousIndex = currentTabIndex;
        currentTabIndex = index;

        for (int i = 0; i < tabButtons.Count; i++)
        {
            tabButtons[i].SetActive(i == index);
            bool show = i == index;
            tabContents[i].SetActive(show);
            if (tabContentCanvasGroups[i] != null)
                tabContentCanvasGroups[i].alpha = show ? 0f : 1f;
        }

        if (index < tabContentCanvasGroups.Count && tabContentCanvasGroups[index] != null)
        {
            tabContents[index].SetActive(true);
            if (index != previousIndex)
                fadeCoroutine = StartCoroutine(FadeInTabContent(index));
            else
                tabContentCanvasGroups[index].alpha = 1f;
        }
    }

    private IEnumerator FadeInTabContent(int index)
    {
        CanvasGroup cg = tabContentCanvasGroups[index];
        if (cg == null) yield break;

        cg.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < TabFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / TabFadeInDuration);
            t = t * t * (3f - 2f * t); // smoothstep
            cg.alpha = t;
            yield return null;
        }

        cg.alpha = 1f;
        fadeCoroutine = null;
    }

    public void Initialize()
    {
        if (tabButtons.Count > 0)
        {
            SelectTab(0);
        }
    }
}
