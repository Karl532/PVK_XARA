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
            GameObject dropdownObj = UIPrimitives.CreateUIElement("DropdownContainer", parent,
                new Vector2(0, 0), new Vector2(1, 0.65f), null, Vector2.zero, Vector2.zero);

            Image bg = dropdownObj.AddComponent<Image>();
            bg.color = UIPrimitives.Colors.DarkBackground;

            dropdown = dropdownObj.AddComponent<TMP_Dropdown>();

            // Caption text (displays selected value)
            GameObject captionObj = UIPrimitives.CreateUIElement("CaptionText", dropdownObj.transform,
                Vector2.zero, Vector2.one, null, new Vector2(20, 10), new Vector2(-60, -10));

            TextMeshProUGUI captionText = captionObj.AddComponent<TextMeshProUGUI>();
            captionText.fontSize = itemFontSize;
            captionText.color = Color.white;
            captionText.alignment = TextAlignmentOptions.Left;

            // Arrow indicator
            GameObject arrowObj = UIPrimitives.CreateUIElement("Arrow", dropdownObj.transform,
                new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(40, 0), null, null,
                new Vector2(1, 0.5f), new Vector2(-15, 0));

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
            GameObject templateObj = UIPrimitives.CreateUIElement("Template", parent.transform,
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, 400), null, null,
                new Vector2(0.5f, 1f), Vector2.zero);

            Image templateBg = templateObj.AddComponent<Image>();
            templateBg.color = new Color(0.15f, 0.15f, 0.2f, 0.98f);

            ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Viewport - use RectMask2D instead of Mask to avoid invisible options text (Mask+Image stencil bug)
            GameObject viewportObj = UIPrimitives.CreateUIElement("Viewport", templateObj.transform,
                Vector2.zero, Vector2.one, null, new Vector2(5, 5), new Vector2(-5, -5));

            viewportObj.AddComponent<RectMask2D>();

            // Content
            GameObject contentObj = UIPrimitives.CreateUIElement("Content", viewportObj.transform,
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, 0), null, null,
                new Vector2(0.5f, 1f), Vector2.zero);

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
            GameObject itemObj = UIPrimitives.CreateUIElement("Item", contentObj.transform,
                Vector2.zero, Vector2.one, new Vector2(0, 80));

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
            GameObject checkObj = UIPrimitives.CreateUIElement("Item Checkmark", itemObj.transform,
                new Vector2(0, 0.1f), new Vector2(0, 0.9f),
                new Vector2(6, 0), null, null,
                new Vector2(0, 0.5f), new Vector2(10, 0));

            Image checkImage = checkObj.AddComponent<Image>();
            checkImage.color = accentColor;

            itemToggle.graphic = checkImage;

            // Item label (must be assigned to dropdown.itemText)
            GameObject itemLabelObj = UIPrimitives.CreateUIElement("Item Label", itemObj.transform,
                Vector2.zero, Vector2.one, null, new Vector2(25, 5), new Vector2(-10, -5));

            TextMeshProUGUI itemLabel = itemLabelObj.AddComponent<TextMeshProUGUI>();
            itemLabel.fontSize = itemFontSize;
            itemLabel.color = Color.white;
            itemLabel.alignment = TextAlignmentOptions.Left;

            scrollRect.viewport = viewportObj.GetComponent<RectTransform>();
            scrollRect.content = contentObj.GetComponent<RectTransform>();

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
