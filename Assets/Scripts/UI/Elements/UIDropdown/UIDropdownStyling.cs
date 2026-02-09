using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UI.Utils;

namespace UI.Elements.UIDropdown
{
    /// <summary>
    /// Handles visual construction and styling for <see cref="UIDropdown"/>.
    /// </summary>
    public static class UIDropdownStyling
    {
        public const float DefaultLabelFontSize = 42f;
        public const float DefaultItemFontSize = 38f;

        /// <summary>
        /// Creates the label above the dropdown.
        /// </summary>
        public static void CreateLabel(Transform parent, string labelText, Color accentColor, float fontSize)
        {
            UIPrimitives.CreateAccentLabel(parent, labelText, accentColor, fontSize);
        }

        /// <summary>
        /// Creates the dropdown container, template, and wires up the TMP_Dropdown. Returns the dropdown container GameObject.
        /// </summary>
        public static GameObject CreateDropdownElement(Transform parent, List<string> options, Color accentColor, float itemFontSize, out TMP_Dropdown dropdown)
        {
            GameObject dropdownObj = new GameObject("DropdownContainer");
            dropdownObj.transform.SetParent(parent, false);

            RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
            dropdownRect.anchorMin = new Vector2(0, 0);
            dropdownRect.anchorMax = new Vector2(1, 0.65f);
            dropdownRect.offsetMin = Vector2.zero;
            dropdownRect.offsetMax = Vector2.zero;
            dropdownRect.localScale = Vector3.one;

            Image bg = dropdownObj.AddComponent<Image>();
            bg.color = UIPrimitives.Colors.DarkBackground;

            dropdown = dropdownObj.AddComponent<TMP_Dropdown>();

            // Caption text (displays selected value)
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

            // Arrow indicator
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

            // Template (dropdown popup) - use RectMask2D instead of Mask to avoid invisible text bug
            GameObject templateObj = CreateTemplate(dropdownObj, dropdown, accentColor, itemFontSize);

            dropdown.captionText = captionText;
            dropdown.template = templateObj.GetComponent<RectTransform>();

            dropdown.ClearOptions();
            dropdown.AddOptions(options);

            return dropdownObj;
        }

        /// <summary>
        /// Creates the dropdown list template. Uses RectMask2D instead of Mask to avoid options text becoming invisible.
        /// </summary>
        static GameObject CreateTemplate(GameObject parent, TMP_Dropdown dropdown, Color accentColor, float itemFontSize)
        {
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

            // Viewport - use RectMask2D instead of Mask to avoid invisible options text (Mask+Image stencil bug)
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(templateObj.transform, false);

            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(5, 5);
            viewportRect.offsetMax = new Vector2(-5, -5);
            viewportRect.localScale = Vector3.one;

            viewportObj.AddComponent<RectMask2D>();

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

            // Item template (cloned for each option)
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

            // Item checkmark
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

            // Item label (must be assigned to dropdown.itemText)
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

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            dropdown.itemText = itemLabel;

            LayoutElement itemLayout = itemObj.AddComponent<LayoutElement>();
            itemLayout.minHeight = 80;
            itemLayout.preferredHeight = 80;

            templateObj.SetActive(false);

            return templateObj;
        }

        public static void StyleDropdown(TMP_Dropdown dropdown)
        {
            UIPrimitives.ApplyStandardSelectableColors(dropdown);
        }

        public static void AddInteractionEffects(GameObject dropdownContainer, Color accentColor)
        {
            RectTransform dropdownRect = dropdownContainer.GetComponent<RectTransform>();
            dropdownRect.offsetMin = new Vector2(5, 5);
            dropdownRect.offsetMax = new Vector2(-5, -5);

            UIPrimitives.AddOutlineAndShadow(dropdownContainer, accentColor);
        }
    }
}
