using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UITabButton : MonoBehaviour
{
    private Button button;
    private Image background;
    private TextMeshProUGUI text;
    private Color activeColor;
    private Color inactiveColor;
    private bool isActive = false;

    public void CreateTabButton(string labelText, Color active, Color inactive, Vector2 size, float fontSize = 48f)
    {
        activeColor = active;
        inactiveColor = inactive;

        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.sizeDelta = size;

        background = gameObject.AddComponent<Image>();
        background.color = inactiveColor;

        button = gameObject.AddComponent<Button>();

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(transform);
        text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = labelText;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.localScale = Vector3.one;
        textRect.localPosition = Vector3.zero;
    }

    public void SetActive(bool active)
    {
        isActive = active;
        background.color = active ? activeColor : inactiveColor;
        text.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
    }

    public Button GetButton()
    {
        return button;
    }
}