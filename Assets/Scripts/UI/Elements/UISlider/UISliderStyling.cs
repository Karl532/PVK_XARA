using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI.Utils;

namespace UI.Elements.UISlider
{
    /// <summary>
    /// Handles visual construction for <see cref="UISlider"/>.
    /// </summary>
    public static class UISliderStyling
    {
        public const float DefaultLabelFontSize = 42f;
        public const float SliderTrackHeight = 14f;
        public const float HandleSize = 48f;
        public const float TickLabelHeight = 48f;
        public const float TickLabelFontSize = 44f;

        public static void CreateLabel(Transform parent, string labelText, Color accentColor, Color textColor)
        {
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(parent, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.7f);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(20, 0);
            labelRect.offsetMax = new Vector2(-20, 0);

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = DefaultLabelFontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = textColor;
            label.alignment = TextAlignmentOptions.Left;
        }

        public static void CreateSliderBar(
            Transform parent,
            float minValue,
            float maxValue,
            float defaultValue,
            Color accentColor,
            out Slider slider,
            out TextMeshProUGUI valueText,
            bool showValue,
            float tickStep = 0f)
        {
            // Free (continuous) slider - no snapping
            bool wholeNumbers = false;
            int tickCount = 0;
            if (tickStep > 0)
            {
                tickCount = Mathf.RoundToInt((maxValue - minValue) / tickStep) + 1;
                tickCount = Mathf.Clamp(tickCount, 2, 15);
            }

            // Container for tick labels + track
            GameObject container = new GameObject("SliderContainer");
            container.transform.SetParent(parent, false);

            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 0.65f);
            containerRect.offsetMin = new Vector2(20, 0);
            containerRect.offsetMax = new Vector2(-20, 0);

            VerticalLayoutGroup containerLayout = container.AddComponent<VerticalLayoutGroup>();
            containerLayout.spacing = 6;
            containerLayout.padding = new RectOffset(0, 0, 0, 0);
            containerLayout.childAlignment = TextAnchor.UpperCenter;
            containerLayout.childControlWidth = true;
            containerLayout.childControlHeight = false;
            containerLayout.childForceExpandWidth = true;
            containerLayout.childForceExpandHeight = false;

            // Tick labels row (values on top) - e.g. 0, 0.5, 1, 1.5, 2, 2.5, 3
            if (tickCount > 1)
            {
                GameObject tickRow = new GameObject("TickLabels");
                tickRow.transform.SetParent(container.transform, false);

                LayoutElement tickLE = tickRow.AddComponent<LayoutElement>();
                tickLE.minHeight = TickLabelHeight;
                tickLE.preferredHeight = TickLabelHeight;

                HorizontalLayoutGroup tickLayout = tickRow.AddComponent<HorizontalLayoutGroup>();
                tickLayout.spacing = 0;
                tickLayout.childAlignment = TextAnchor.MiddleCenter;
                tickLayout.childControlWidth = true;
                tickLayout.childControlHeight = false;
                tickLayout.childForceExpandWidth = true;
                tickLayout.childForceExpandHeight = false;

                for (int i = 0; i < tickCount; i++)
                {
                    float val = minValue + i * tickStep;
                    string display = (val == Mathf.Floor(val)) ? val.ToString("F0") : val.ToString("F1");
                    GameObject tickObj = new GameObject($"Tick_{display}");
                    tickObj.transform.SetParent(tickRow.transform, false);

                    TextMeshProUGUI tickText = tickObj.AddComponent<TextMeshProUGUI>();
                    tickText.text = display;
                    tickText.fontSize = TickLabelFontSize;
                    tickText.fontStyle = FontStyles.Bold;
                    tickText.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.85f);
                    tickText.alignment = TextAlignmentOptions.Center;
                }
            }

            // Track - simple bar (black/dark blue) that runs across, markers on top
            GameObject trackObj = new GameObject("SliderTrack");
            trackObj.transform.SetParent(container.transform, false);

            LayoutElement trackLE = trackObj.AddComponent<LayoutElement>();
            trackLE.minHeight = SliderTrackHeight + 6;
            trackLE.preferredHeight = SliderTrackHeight + 6;

            trackObj.AddComponent<RectTransform>();

            Image trackBg = trackObj.AddComponent<Image>();
            trackBg.color = new Color(0.08f, 0.1f, 0.18f, 0.95f); // dark blue-black bar

            RoundedImage trackRounded = trackObj.AddComponent<RoundedImage>();
            trackRounded.SetRadius(6f);

            // Fill area (left-to-handle progress)
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(trackObj.transform, false);

            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(4, 4);
            fillAreaRect.offsetMax = new Vector2(-4, -4);

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);

            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(accentColor.r * 0.5f, accentColor.g * 0.5f, accentColor.b * 0.5f, 0.9f);

            RoundedImage fillRounded = fillObj.AddComponent<RoundedImage>();
            fillRounded.SetRadius(4f);

            // Handle (circular)
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(trackObj.transform, false);

            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(HandleSize * 0.5f, 0);
            handleAreaRect.offsetMax = new Vector2(-HandleSize * 0.5f, 0);

            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleArea.transform, false);

            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(HandleSize, HandleSize);
            handleRect.anchorMin = new Vector2(0, 0.5f);
            handleRect.anchorMax = new Vector2(0, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = accentColor;

            // Perfect circle: radius = half of square size
            RoundedImage handleRounded = handleObj.AddComponent<RoundedImage>();
            handleRounded.SetRadius(HandleSize * 0.5f);

            Outline handleOutline = handleObj.AddComponent<Outline>();
            handleOutline.effectColor = new Color(1f, 1f, 1f, 0.5f);
            handleOutline.effectDistance = new Vector2(1, 1);

            // Slider component
            slider = trackObj.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.wholeNumbers = wholeNumbers;
            slider.value = Mathf.Clamp(defaultValue, minValue, maxValue);
            slider.targetGraphic = handleImage;

            UIPrimitives.ApplyStandardSelectableColors(slider);

            valueText = null;
            if (showValue && tickCount == 0)
            {
                GameObject valueObj = new GameObject("ValueText");
                valueObj.transform.SetParent(container.transform.parent, false);

                RectTransform valueRect = valueObj.AddComponent<RectTransform>();
                valueRect.anchorMin = new Vector2(1, 0);
                valueRect.anchorMax = new Vector2(1, 0.65f);
                valueRect.pivot = new Vector2(1, 0.5f);
                valueRect.anchoredPosition = new Vector2(-10, 0);
                valueRect.sizeDelta = new Vector2(60, 0);

                valueText = valueObj.AddComponent<TextMeshProUGUI>();
                valueText.fontSize = 38;
                valueText.fontStyle = FontStyles.Bold;
                valueText.color = accentColor;
                valueText.alignment = TextAlignmentOptions.MidlineRight;
            }
        }
    }
}
