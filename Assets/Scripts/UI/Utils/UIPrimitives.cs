using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.Utils
{
    /// <summary>
    /// Low-level UI element creation and styling. Use for primitives (rects, text, outlines, backgrounds).
    /// </summary>
    public static class UIPrimitives
    {
        // --- Colors ---
        public static class Colors
        {
            /// <summary>Standard dark background for input fields, dropdowns, and similar containers.</summary>
            public static readonly Color DarkBackground = new Color(0.12f, 0.12f, 0.18f, 0.95f);

            /// <summary>Background for templates, dropdowns, etc.</summary>
            public static readonly Color TemplateBackground = new Color(0.15f, 0.15f, 0.2f, 0.98f);

            public static Color DimmedAccent(Color accent, float factor = 0.3f)
            {
                return new Color(accent.r * factor, accent.g * factor, accent.b * factor, 0.95f);
            }

            public static Color WithAlpha(Color color, float alpha)
            {
                return new Color(color.r, color.g, color.b, alpha);
            }
        }

        // --- Sizing ---
        public static class Sizing
        {
            public const float DefaultBorderRadius = 8f;
            public const float DefaultOutlineSize = 2f;
            public const float CheckmarkSizeRatio = 0.65f;
            public const float ArrowSizeRatio = 0.7f;
        }

        // --- Element creation ---

        /// <summary>Creates a GameObject with RectTransform. Parent must not be null.</summary>
        public static GameObject CreateUIElement(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2? sizeDelta = null,
            Vector2? offsetMin = null,
            Vector2? offsetMax = null,
            Vector2? pivot = null,
            Vector2? anchoredPosition = null)
        {
            if (parent == null)
                throw new System.ArgumentNullException(nameof(parent));

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.localScale = Vector3.one;

            if (pivot.HasValue)
                rect.pivot = pivot.Value;
            if (sizeDelta.HasValue)
                rect.sizeDelta = sizeDelta.Value;
            if (offsetMin.HasValue)
                rect.offsetMin = offsetMin.Value;
            if (offsetMax.HasValue)
                rect.offsetMax = offsetMax.Value;
            if (anchoredPosition.HasValue)
                rect.anchoredPosition = anchoredPosition.Value;

            return obj;
        }

        /// <summary>Adds TextMeshProUGUI to the given GameObject. Parent must not be null.</summary>
        public static TextMeshProUGUI CreateText(
            GameObject parent,
            string text,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment,
            FontStyles style = FontStyles.Normal)
        {
            if (parent == null)
                throw new System.ArgumentNullException(nameof(parent));

            TextMeshProUGUI tmp = parent.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = style;

            return tmp;
        }

        /// <summary>Adds standard outline with accent color.</summary>
        public static void AddStandardOutline(GameObject target, Color accentColor, float alpha = 0.4f)
        {
            if (target == null) return;

            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = Colors.WithAlpha(accentColor, alpha);
            outline.effectDistance = new Vector2(Sizing.DefaultOutlineSize, Sizing.DefaultOutlineSize);
        }

        /// <summary>Adds Image component with color. Returns the Image.</summary>
        public static Image AddBackgroundImage(GameObject target, Color color)
        {
            if (target == null)
                throw new System.ArgumentNullException(nameof(target));

            Image img = target.AddComponent<Image>();
            img.color = color;
            return img;
        }

        // --- Migrated from UIStylingHelper ---

        /// <summary>Creates an accent-colored label above a control area. Used by UIDropdown, UIInputField, etc.</summary>
        public static TextMeshProUGUI CreateAccentLabel(Transform parent, string labelText, Color accentColor, float fontSize = 42f)
        {
            GameObject labelObj = CreateUIElement("Label", parent,
                new Vector2(0, 0.7f), new Vector2(1, 1),
                null, new Vector2(20, 0), new Vector2(-20, 0));

            return CreateText(labelObj, labelText, fontSize, accentColor, TextAlignmentOptions.Left, FontStyles.Bold);
        }

        /// <summary>Adds the standard outline and drop shadow to an interactive container (input field, dropdown, etc.).</summary>
        public static void AddOutlineAndShadow(GameObject go, Color accentColor)
        {
            if (go == null) return;

            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = Colors.WithAlpha(accentColor, 0.4f);
            outline.effectDistance = new Vector2(Sizing.DefaultOutlineSize, Sizing.DefaultOutlineSize);

            Shadow shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(3, -3);
        }

        /// <summary>Applies the standard ColorBlock to Selectables (TMP_Dropdown, TMP_InputField, etc.).</summary>
        public static void ApplyStandardSelectableColors(Selectable selectable)
        {
            if (selectable == null) return;

            selectable.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = selectable.colors;
            colors.normalColor = Colors.DarkBackground;
            colors.highlightedColor = new Color(0.18f, 0.18f, 0.25f, 0.95f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.22f, 0.95f);
            colors.selectedColor = new Color(0.18f, 0.18f, 0.25f, 0.95f);
            colors.disabledColor = new Color(0.08f, 0.08f, 0.12f, 0.5f);
            colors.colorMultiplier = 1.2f;
            selectable.colors = colors;
        }
    }
}
