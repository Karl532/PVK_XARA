using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;

public class UIDropdown : MonoBehaviour
{
    private TMP_Dropdown dropdown;
    private Image backgroundImage;

    public void CreateDropdown(string label, List<string> options, Color accentColor, float labelFontSize = 42f, float itemFontSize = 38f)
    {
        RectTransform containerRect = gameObject.GetComponent<RectTransform>();
        if (containerRect == null)
        {
            containerRect = gameObject.AddComponent<RectTransform>();
        }

        // Create label
        CreateLabel(label, accentColor, labelFontSize);

        // Create dropdown element
        GameObject dropdownContainer = CreateDropdownElement(options, accentColor, itemFontSize);

        // Style the dropdown
        StyleDropdown(accentColor);

        // Add interaction effects
        AddInteractionEffects(dropdownContainer, accentColor);
    }

    void CreateLabel(string labelText, Color accentColor, float fontSize)
    {
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(transform, false);

        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.7f);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.offsetMin = new Vector2(20, 0);
        labelRect.offsetMax = new Vector2(-20, 0);
        labelRect.localScale = Vector3.one;

        TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
        labelTMP.text = labelText;
        labelTMP.fontSize = fontSize;
        labelTMP.fontStyle = FontStyles.Bold;
        labelTMP.color = accentColor;
        labelTMP.alignment = TextAlignmentOptions.Left;
    }

    GameObject CreateDropdownElement(List<string> options, Color accentColor, float itemFontSize)
    {
        // Dropdown container (same anchor layout as UIInputField's input container)
        GameObject dropdownObj = new GameObject("DropdownContainer");
        dropdownObj.transform.SetParent(transform, false);

        RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
        dropdownRect.anchorMin = new Vector2(0, 0);
        dropdownRect.anchorMax = new Vector2(1, 0.65f);
        dropdownRect.offsetMin = new Vector2(0, 0);
        dropdownRect.offsetMax = new Vector2(0, 0);
        dropdownRect.localScale = Vector3.one;

        backgroundImage = dropdownObj.AddComponent<Image>();
        backgroundImage.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

        // Add TMP_Dropdown component
        dropdown = dropdownObj.AddComponent<TMP_Dropdown>();

        // Create caption text (displays selected value)
        GameObject captionObj = new GameObject("CaptionText");
        captionObj.transform.SetParent(dropdownObj.transform, false);

        RectTransform captionRect = captionObj.AddComponent<RectTransform>();
        captionRect.anchorMin = Vector2.zero;
        captionRect.anchorMax = Vector2.one;
        captionRect.offsetMin = new Vector2(20, 10);
        captionRect.offsetMax = new Vector2(-60, -10);
        captionRect.localScale = Vector3.one;

        TextMeshProUGUI captionText = captionObj.AddComponent<TextMeshProUGUI>();
        captionText.fontSize = itemFontSize;
        captionText.color = Color.white;
        captionText.alignment = TextAlignmentOptions.Left;

        // Create arrow indicator
        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(dropdownObj.transform, false);

        RectTransform arrowRect = arrowObj.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1, 0);
        arrowRect.anchorMax = new Vector2(1, 1);
        arrowRect.pivot = new Vector2(1, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-15, 0);
        arrowRect.sizeDelta = new Vector2(40, 0);
        arrowRect.localScale = Vector3.one;

        TextMeshProUGUI arrowText = arrowObj.AddComponent<TextMeshProUGUI>();
        arrowText.text = "\u25BC";
        arrowText.fontSize = itemFontSize * 0.7f;
        arrowText.color = new Color(1f, 1f, 1f, 0.6f);
        arrowText.alignment = TextAlignmentOptions.Center;

        // Create template (dropdown popup) - must be inactive initially
        GameObject templateObj = CreateTemplate(dropdownObj, accentColor, itemFontSize);

        // Wire up references
        dropdown.captionText = captionText;
        dropdown.template = templateObj.GetComponent<RectTransform>();

        // Add options
        dropdown.ClearOptions();
        dropdown.AddOptions(options);

        return dropdownObj;
    }

    GameObject CreateTemplate(GameObject parent, Color accentColor, float itemFontSize)
    {
        // Template root
        GameObject templateObj = new GameObject("Template");
        templateObj.transform.SetParent(parent.transform, false);

        RectTransform templateRect = templateObj.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.anchoredPosition = Vector2.zero;
        templateRect.sizeDelta = new Vector2(0, 400);
        templateRect.localScale = Vector3.one;

        Image templateBg = templateObj.AddComponent<Image>();
        templateBg.color = new Color(0.15f, 0.15f, 0.2f, 0.98f);

        ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(templateObj.transform, false);

        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(5, 5);
        viewportRect.offsetMax = new Vector2(-5, -5);
        viewportRect.localScale = Vector3.one;

        viewportObj.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        Mask viewportMask = viewportObj.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        // Content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);

        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);
        contentRect.localScale = Vector3.one;

        VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 2;
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter contentFitter = contentObj.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Item template
        GameObject itemObj = new GameObject("Item");
        itemObj.transform.SetParent(contentObj.transform, false);

        RectTransform itemRect = itemObj.AddComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0, 80);
        itemRect.localScale = Vector3.one;

        Image itemBg = itemObj.AddComponent<Image>();
        itemBg.color = new Color(0.15f, 0.15f, 0.2f, 0.98f);

        Toggle itemToggle = itemObj.AddComponent<Toggle>();
        itemToggle.targetGraphic = itemBg;

        ColorBlock itemColors = itemToggle.colors;
        itemColors.normalColor = new Color(0.15f, 0.15f, 0.2f, 0.98f);
        itemColors.highlightedColor = new Color(accentColor.r * 0.4f, accentColor.g * 0.4f, accentColor.b * 0.4f, 0.95f);
        itemColors.pressedColor = new Color(accentColor.r * 0.3f, accentColor.g * 0.3f, accentColor.b * 0.3f, 0.95f);
        itemColors.selectedColor = new Color(accentColor.r * 0.5f, accentColor.g * 0.5f, accentColor.b * 0.5f, 0.95f);
        itemColors.colorMultiplier = 1f;
        itemToggle.colors = itemColors;

        // Item checkmark (small accent indicator on selected item)
        GameObject checkObj = new GameObject("Item Checkmark");
        checkObj.transform.SetParent(itemObj.transform, false);

        RectTransform checkRect = checkObj.AddComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0, 0.1f);
        checkRect.anchorMax = new Vector2(0, 0.9f);
        checkRect.pivot = new Vector2(0, 0.5f);
        checkRect.anchoredPosition = new Vector2(10, 0);
        checkRect.sizeDelta = new Vector2(6, 0);
        checkRect.localScale = Vector3.one;

        Image checkImage = checkObj.AddComponent<Image>();
        checkImage.color = accentColor;

        itemToggle.graphic = checkImage;

        // Item label
        GameObject itemLabelObj = new GameObject("Item Label");
        itemLabelObj.transform.SetParent(itemObj.transform, false);

        RectTransform itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(25, 5);
        itemLabelRect.offsetMax = new Vector2(-10, -5);
        itemLabelRect.localScale = Vector3.one;

        TextMeshProUGUI itemLabel = itemLabelObj.AddComponent<TextMeshProUGUI>();
        itemLabel.fontSize = itemFontSize;
        itemLabel.color = Color.white;
        itemLabel.alignment = TextAlignmentOptions.Left;

        // Wire up scroll rect
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        // Wire up dropdown item text reference
        dropdown.itemText = itemLabel;

        // Add layout element for item height
        LayoutElement itemLayout = itemObj.AddComponent<LayoutElement>();
        itemLayout.minHeight = 80;
        itemLayout.preferredHeight = 80;

        // Template must be inactive
        templateObj.SetActive(false);

        return templateObj;
    }

    void StyleDropdown(Color accentColor)
    {
        dropdown.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = dropdown.colors;
        colors.normalColor = new Color(0.12f, 0.12f, 0.18f, 0.95f);
        colors.highlightedColor = new Color(0.18f, 0.18f, 0.25f, 0.95f);
        colors.pressedColor = new Color(0.15f, 0.15f, 0.22f, 0.95f);
        colors.selectedColor = new Color(0.18f, 0.18f, 0.25f, 0.95f);
        colors.disabledColor = new Color(0.08f, 0.08f, 0.12f, 0.5f);
        colors.colorMultiplier = 1.2f;
        dropdown.colors = colors;
    }

    void AddInteractionEffects(GameObject dropdownContainer, Color accentColor)
    {
        RectTransform dropdownRect = dropdownContainer.GetComponent<RectTransform>();
        dropdownRect.offsetMin = new Vector2(5, 5);
        dropdownRect.offsetMax = new Vector2(-5, -5);

        Outline outline = dropdownContainer.AddComponent<Outline>();
        outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.4f);
        outline.effectDistance = new Vector2(2, 2);

        Shadow shadow = dropdownContainer.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(3, -3);
    }

    public int GetValue()
    {
        return dropdown != null ? dropdown.value : 0;
    }

    public void SetValue(int index)
    {
        if (dropdown != null)
        {
            dropdown.value = index;
        }
    }

    public string GetSelectedText()
    {
        if (dropdown != null && dropdown.options.Count > dropdown.value)
        {
            return dropdown.options[dropdown.value].text;
        }
        return "";
    }

    public void OnValueChanged(UnityAction<int> callback)
    {
        if (dropdown != null)
        {
            dropdown.onValueChanged.AddListener(callback);
        }
    }
}
