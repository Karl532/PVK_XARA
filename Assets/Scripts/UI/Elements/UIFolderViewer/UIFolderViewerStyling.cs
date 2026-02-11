using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI.Utils;

namespace UI.Elements.UIFolderViewer
{
    /// <summary>
    /// Handles visual construction for <see cref="UIFolderViewer"/>.
    /// </summary>
    public static class UIFolderViewerStyling
    {
        public const float DefaultLabelFontSize = 42f;
        public const float DefaultItemFontSize = 38f;
        public const float ItemHeight = 128f;

        /// <summary>
        /// Creates a label that fills the parent (use with a LayoutElement-controlled container for layout-driven UIs).
        /// </summary>
        public static void CreateLabel(Transform parent, string labelText, Color color, float fontSize)
        {
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(parent, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(20, 0);
            labelRect.offsetMax = new Vector2(-20, 0);
            labelRect.localScale = Vector3.one;

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = color;
        }

        /// <summary>
        /// Creates the scrollable file list container. Returns the list GameObject; scrollRect.content is where items go.
        /// </summary>
        public static GameObject CreateFileList(Transform parent, out ScrollRect scrollRect)
        {
            GameObject listObj = new GameObject("FileList");
            listObj.transform.SetParent(parent, false);

            RectTransform listRect = listObj.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0);
            listRect.anchorMax = new Vector2(1, 1);
            listRect.offsetMin = Vector2.zero;
            listRect.offsetMax = Vector2.zero;
            listRect.localScale = Vector3.one;

            LayoutElement listLayout = listObj.AddComponent<LayoutElement>();
            listLayout.minHeight = 440;
            listLayout.flexibleHeight = 1;

            Image listBg = listObj.AddComponent<Image>();
            listBg.color = new Color(0.1f, 0.1f, 0.14f, 0.92f);

            var listRounded = listObj.AddComponent<global::RoundedImage>();
            listRounded.SetRadius(14f);

            scrollRect = listObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // Viewport
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(listObj.transform, false);

            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(5, 5);
            viewportRect.offsetMax = new Vector2(-25, -5);
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
            contentLayout.spacing = 6;
            contentLayout.padding = new RectOffset(12, 12, 12, 12);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = contentObj.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            return listObj;
        }

        static readonly Color ItemNormalColor = new Color(0.14f, 0.14f, 0.18f, 0.96f);
        const float SelectionAnimDuration = 0.22f;

        public static GameObject CreateFileListItem(Transform content, ToggleGroup toggleGroup, string fileName, string fullPath, Color accentColor)
        {
            GameObject itemObj = new GameObject($"Item_{fileName}");
            itemObj.transform.SetParent(content, false);

            var itemData = itemObj.AddComponent<UIFolderViewerItemData>();
            itemData.FullPath = fullPath;

            RectTransform itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, ItemHeight);
            itemRect.localScale = Vector3.one;

            Image itemBg = itemObj.AddComponent<Image>();
            itemBg.color = ItemNormalColor;

            var itemRounded = itemObj.AddComponent<global::RoundedImage>();
            itemRounded.SetRadius(10f);

            Toggle toggle = itemObj.AddComponent<Toggle>();
            toggle.targetGraphic = itemBg;
            toggle.group = toggleGroup;

            Color selectedColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.98f);
            var selectionAnim = itemObj.AddComponent<UIFolderViewerItemSelectionAnim>();
            selectionAnim.Init(itemBg, toggle, ItemNormalColor, selectedColor, SelectionAnimDuration);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(itemObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(20, 12);
            labelRect.offsetMax = new Vector2(-16, -12);
            labelRect.localScale = Vector3.one;

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = fileName;
            label.fontSize = DefaultItemFontSize;
            label.color = new Color(0.95f, 0.95f, 0.98f, 1f);
            label.alignment = TextAlignmentOptions.Left;

            LayoutElement itemLayout = itemObj.AddComponent<LayoutElement>();
            itemLayout.minHeight = ItemHeight;
            itemLayout.preferredHeight = ItemHeight;

            return itemObj;
        }

        /// <summary>
        /// Creates a "Load model" button at bottom right. Returns the button GameObject.
        /// </summary>
        public static Button CreateLoadButton(Transform parent, Color accentColor, UnityEngine.Events.UnityAction onClick)
        {
            GameObject rowObj = new GameObject("LoadButtonRow");
            rowObj.transform.SetParent(parent, false);

            RectTransform rowRect = rowObj.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 0);
            rowRect.anchorMax = new Vector2(1, 0);
            rowRect.pivot = new Vector2(1, 0);
            rowRect.anchoredPosition = Vector2.zero;
            rowRect.sizeDelta = new Vector2(0, 90);
            rowRect.localScale = Vector3.one;

            LayoutElement rowLayout = rowObj.AddComponent<LayoutElement>();
            rowLayout.minHeight = 90;
            rowLayout.preferredHeight = 90;

            HorizontalLayoutGroup rowLayoutGroup = rowObj.AddComponent<HorizontalLayoutGroup>();
            rowLayoutGroup.childAlignment = TextAnchor.MiddleRight;
            rowLayoutGroup.childControlWidth = false;
            rowLayoutGroup.childControlHeight = true;
            rowLayoutGroup.childForceExpandWidth = false;

            GameObject btnObj = new GameObject("LoadModelButton");
            btnObj.transform.SetParent(rowObj.transform, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(320, 80);
            btnRect.localScale = Vector3.one;

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = accentColor;

            Button button = btnObj.AddComponent<Button>();
            button.targetGraphic = btnImage;
            button.onClick.AddListener(onClick);

            button.transition = Selectable.Transition.ColorTint;
            ColorBlock btnColors = button.colors;
            btnColors.normalColor = accentColor;
            btnColors.highlightedColor = new Color(
                Mathf.Min(1f, accentColor.r * 1.25f),
                Mathf.Min(1f, accentColor.g * 1.25f),
                Mathf.Min(1f, accentColor.b * 1.25f),
                1f);
            btnColors.pressedColor = new Color(accentColor.r * 0.85f, accentColor.g * 0.85f, accentColor.b * 0.85f, 1f);
            btnColors.selectedColor = accentColor;
            btnColors.disabledColor = new Color(accentColor.r * 0.4f, accentColor.g * 0.4f, accentColor.b * 0.4f, 0.6f);
            btnColors.colorMultiplier = 1f;
            btnColors.fadeDuration = 0.15f;
            button.colors = btnColors;

            LayoutElement btnLayout = btnObj.AddComponent<LayoutElement>();
            btnLayout.minWidth = 320;
            btnLayout.preferredWidth = 320;
            btnLayout.minHeight = 80;
            btnLayout.preferredHeight = 80;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btnObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = "Load model";
            label.fontSize = 40;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;

            return button;
        }

        /// <summary>
        /// Creates a "Refresh list" button in its own row (typically above the Load button).
        /// </summary>
        public static Button CreateRefreshButton(Transform parent, Color accentColor, UnityEngine.Events.UnityAction onClick)
        {
            GameObject rowObj = new GameObject("RefreshButtonRow");
            rowObj.transform.SetParent(parent, false);

            RectTransform rowRect = rowObj.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 0);
            rowRect.anchorMax = new Vector2(1, 0);
            rowRect.pivot = new Vector2(0, 0);
            rowRect.anchoredPosition = Vector2.zero;
            rowRect.sizeDelta = new Vector2(0, 80);
            rowRect.localScale = Vector3.one;

            LayoutElement rowLayout = rowObj.AddComponent<LayoutElement>();
            rowLayout.minHeight = 80;
            rowLayout.preferredHeight = 80;

            HorizontalLayoutGroup rowLayoutGroup = rowObj.AddComponent<HorizontalLayoutGroup>();
            rowLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
            rowLayoutGroup.childControlWidth = false;
            rowLayoutGroup.childControlHeight = true;
            rowLayoutGroup.childForceExpandWidth = false;

            GameObject btnObj = new GameObject("RefreshButton");
            btnObj.transform.SetParent(rowObj.transform, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(260, 70);
            btnRect.localScale = Vector3.one;

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = accentColor;

            Button button = btnObj.AddComponent<Button>();
            button.targetGraphic = btnImage;
            button.onClick.AddListener(onClick);

            button.transition = Selectable.Transition.ColorTint;
            ColorBlock btnColors = button.colors;
            btnColors.normalColor = accentColor;
            btnColors.highlightedColor = new Color(
                Mathf.Min(1f, accentColor.r * 1.2f),
                Mathf.Min(1f, accentColor.g * 1.2f),
                Mathf.Min(1f, accentColor.b * 1.2f),
                1f);
            btnColors.pressedColor = new Color(accentColor.r * 0.85f, accentColor.g * 0.85f, accentColor.b * 0.85f, 1f);
            btnColors.selectedColor = accentColor;
            btnColors.disabledColor = new Color(accentColor.r * 0.4f, accentColor.g * 0.4f, accentColor.b * 0.4f, 0.6f);
            btnColors.colorMultiplier = 1f;
            btnColors.fadeDuration = 0.15f;
            button.colors = btnColors;

            LayoutElement btnLayout = btnObj.AddComponent<LayoutElement>();
            btnLayout.minWidth = 260;
            btnLayout.preferredWidth = 260;
            btnLayout.minHeight = 70;
            btnLayout.preferredHeight = 70;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btnObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = "Refresh list";
            label.fontSize = 34;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;

            return button;
        }
    }
}
