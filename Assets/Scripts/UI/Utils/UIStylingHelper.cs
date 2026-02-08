using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.Utils
{
    /// <summary>
    /// Shared UI styling helpers used across Elements (labels, outlines, shadows).
    /// </summary>
    public static class UIStylingHelper
    {
    /// <summary>
    /// Standard dark background for input fields, dropdowns, and similar containers.
    /// </summary>
    public static readonly Color DarkBackgroundColor = new Color(0.12f, 0.12f, 0.18f, 0.95f);

    /// <summary>
    /// Creates an accent-colored label above a control area. Used by UIDropdown, UIInputField, etc.
    /// </summary>
    /// <param name="parent">Parent transform (typically the element root).</param>
    /// <param name="labelText">Label text.</param>
    /// <param name="accentColor">Label color.</param>
    /// <param name="fontSize">Font size (default 42).</param>
    /// <returns>The created TextMeshProUGUI component.</returns>
    public static TextMeshProUGUI CreateAccentLabel(Transform parent, string labelText, Color accentColor, float fontSize = 42f)
    {
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(parent, false);

        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.7f);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.offsetMin = new Vector2(20, 0);
        labelRect.offsetMax = new Vector2(-20, 0);
        labelRect.localScale = Vector3.one;

        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.color = accentColor;
        label.alignment = TextAlignmentOptions.Left;

        return label;
    }

    /// <summary>
    /// Adds the standard outline and drop shadow to an interactive container (input field, dropdown, etc.).
    /// </summary>
    public static void AddOutlineAndShadow(GameObject go, Color accentColor)
    {
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.4f);
        outline.effectDistance = new Vector2(2, 2);

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(3, -3);
    }

    /// <summary>
    /// Applies the standard ColorBlock (normal, highlighted, pressed, selected, disabled) to Selectables
    /// like TMP_Dropdown and TMP_InputField. Use alongside any component-specific styling (e.g. caret, selection).
    /// </summary>
    public static void ApplyStandardSelectableColors(Selectable selectable)
    {
        selectable.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = selectable.colors;
        colors.normalColor = DarkBackgroundColor;
        colors.highlightedColor = new Color(0.18f, 0.18f, 0.25f, 0.95f);
        colors.pressedColor = new Color(0.15f, 0.15f, 0.22f, 0.95f);
        colors.selectedColor = new Color(0.18f, 0.18f, 0.25f, 0.95f);
        colors.disabledColor = new Color(0.08f, 0.08f, 0.12f, 0.5f);
        colors.colorMultiplier = 1.2f;
        selectable.colors = colors;
    }
}
}
