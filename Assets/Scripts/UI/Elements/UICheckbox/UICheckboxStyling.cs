using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.Elements.UICheckbox
{
    /// <summary>
    /// Handles visual construction and styling for <see cref="UICheckbox"/>.
    /// </summary>
    public static class UICheckboxStyling
    {
        public const float DefaultCheckboxSize = 60f;
        public const float DefaultFontSize = 42f;
        public const float BoxPadding = 20f;
        public const float LabelLeftMargin = 40f;

        /// <summary>
        /// Creates the checkbox box (background, outline, checkmark) and returns references for toggle and visuals.
        /// </summary>
        public static void CreateCheckboxBox(
            Transform parent,
            Color accentColor,
            float checkboxSize,
            out Image checkboxBackground,
            out TextMeshProUGUI checkmarkText)
        {
            GameObject boxObj = new GameObject("CheckboxBox");
            boxObj.transform.SetParent(parent, false);

            RectTransform boxRect = boxObj.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0, 0.5f);
            boxRect.anchorMax = new Vector2(0, 0.5f);
            boxRect.pivot = new Vector2(0, 0.5f);
            boxRect.anchoredPosition = new Vector2(BoxPadding, 0);
            boxRect.sizeDelta = new Vector2(checkboxSize, checkboxSize);

            checkboxBackground = boxObj.AddComponent<Image>();
            checkboxBackground.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

            global::RoundedImage rounded = boxObj.AddComponent<global::RoundedImage>();
            rounded.SetRadius(8f);

            Outline outline = boxObj.AddComponent<Outline>();
            outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.4f);
            outline.effectDistance = new Vector2(2, 2);

            GameObject checkmarkObj = new GameObject("Checkmark");
            checkmarkObj.transform.SetParent(boxObj.transform, false);

            RectTransform checkRect = checkmarkObj.AddComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;

            checkmarkText = checkmarkObj.AddComponent<TextMeshProUGUI>();
            checkmarkText.text = "\u2713";
            checkmarkText.fontSize = checkboxSize * 0.65f;
            checkmarkText.color = accentColor;
            checkmarkText.alignment = TextAlignmentOptions.Center;
            checkmarkText.fontStyle = FontStyles.Bold;
        }

        /// <summary>
        /// Creates the label to the right of the checkbox box.
        /// </summary>
        public static void CreateCheckboxLabel(Transform parent, string labelText, float fontSize, float checkboxSize)
        {
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(parent, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(checkboxSize + LabelLeftMargin, 0);
            labelRect.offsetMax = new Vector2(-BoxPadding, 0);

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
        }

        /// <summary>
        /// Updates checkbox visuals based on checked state.
        /// </summary>
        public static void UpdateVisuals(bool isOn, Color accentColor, Image checkboxBackground, TextMeshProUGUI checkmarkText)
        {
            if (checkmarkText != null)
                checkmarkText.enabled = isOn;

            if (checkboxBackground != null)
            {
                checkboxBackground.color = isOn
                    ? new Color(accentColor.r * 0.3f, accentColor.g * 0.3f, accentColor.b * 0.3f, 0.95f)
                    : new Color(0.12f, 0.12f, 0.18f, 0.95f);
            }
        }
    }
}
