using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI.Utils;
using KeyBinding;

namespace UI.Elements.UIRebindableKeyBinding
{
    public static class UIRebindableKeyBindingStyling
    {
        const float LabelFontSize = 42f;
        const float KeyBoxWidth = 220f;
        const float KeyBoxHeight = 90f;
        const float KeyBoxFontSize = 36f;

        public static void Create(UIRebindableKeyBinding component, string label, OVRInput.Button defaultButton, Color textColor)
        {
            Transform root = component.transform;

            HorizontalLayoutGroup row = root.GetComponent<HorizontalLayoutGroup>();
            if (row == null)
            {
                row = root.gameObject.AddComponent<HorizontalLayoutGroup>();
                row.spacing = 30;
                row.padding = new RectOffset(0, 0, 0, 0);
                row.childAlignment = TextAnchor.MiddleLeft;
                row.childControlWidth = false;
                row.childControlHeight = false;
                row.childForceExpandWidth = false;
            }

            LayoutElement rootLE = root.GetComponent<LayoutElement>();
            if (rootLE == null) rootLE = root.gameObject.AddComponent<LayoutElement>();
            rootLE.minHeight = KeyBoxHeight;
            rootLE.preferredHeight = KeyBoxHeight;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(root, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0, 0.5f);
            labelRect.pivot = new Vector2(0, 0.5f);
            labelRect.anchoredPosition = new Vector2(0, 0);
            labelRect.sizeDelta = new Vector2(800, KeyBoxHeight);

            LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 800;
            labelLE.minWidth = 400;
            labelLE.minHeight = KeyBoxHeight;
            labelLE.preferredHeight = KeyBoxHeight;

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = LabelFontSize;
            labelText.fontStyle = FontStyles.Bold;
            labelText.color = textColor;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;

            GameObject keyBoxObj = new GameObject("KeyBox");
            keyBoxObj.transform.SetParent(root, false);

            RectTransform keyBoxRect = keyBoxObj.AddComponent<RectTransform>();
            keyBoxRect.anchorMin = new Vector2(1, 0.5f);
            keyBoxRect.anchorMax = new Vector2(1, 0.5f);
            keyBoxRect.pivot = new Vector2(1, 0.5f);
            keyBoxRect.anchoredPosition = new Vector2(0, 0);
            keyBoxRect.sizeDelta = new Vector2(KeyBoxWidth, KeyBoxHeight);

            LayoutElement keyBoxLE = keyBoxObj.AddComponent<LayoutElement>();
            keyBoxLE.minWidth = KeyBoxWidth;
            keyBoxLE.preferredWidth = KeyBoxWidth;
            keyBoxLE.minHeight = KeyBoxHeight;
            keyBoxLE.preferredHeight = KeyBoxHeight;

            Image keyBoxBg = keyBoxObj.AddComponent<Image>();
            keyBoxBg.color = UIPrimitives.Colors.DarkBackground;

            global::RoundedImage keyBoxRounded = keyBoxObj.AddComponent<global::RoundedImage>();
            keyBoxRounded.SetRadius(10f);

            Button keyBoxButton = keyBoxObj.AddComponent<Button>();
            keyBoxButton.targetGraphic = keyBoxBg;
            UIPrimitives.ApplyStandardSelectableColors(keyBoxButton);

            GameObject keyTextObj = new GameObject("KeyText");
            keyTextObj.transform.SetParent(keyBoxObj.transform, false);

            RectTransform keyTextRect = keyTextObj.AddComponent<RectTransform>();
            keyTextRect.anchorMin = Vector2.zero;
            keyTextRect.anchorMax = Vector2.one;
            keyTextRect.offsetMin = new Vector2(12, 8);
            keyTextRect.offsetMax = new Vector2(-12, -8);

            TextMeshProUGUI keyDisplayText = keyTextObj.AddComponent<TextMeshProUGUI>();
            keyDisplayText.text = UIRebindableKeyBinding.GetButtonDisplayName(defaultButton);
            keyDisplayText.fontSize = KeyBoxFontSize;
            keyDisplayText.fontStyle = FontStyles.Bold;
            keyDisplayText.color = textColor;
            keyDisplayText.alignment = TextAlignmentOptions.Midline;

            KeyBindInput input = KeyBindRegistry.AddInput<KeyBindInput>();
            input.Button = defaultButton;
            input.Mode = KeyBindInput.TriggerMode.OnPress;
            input.Controller = OVRInput.Controller.Touch;

            component.SetReferences(input, keyDisplayText, keyBoxButton);
        }
    }
}
