using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UITabButton : MonoBehaviour
{
    private const float TransitionDuration = 0.2f;
    private const float ActiveScale = 1.02f;
    private const float InactiveScale = 1f;

    private Button button;
    private Image background;
    private TextMeshProUGUI text;
    private Color activeColor;
    private Color inactiveColor;
    private bool isActive;
    private Coroutine transitionCoroutine;

    public void CreateTabButton(string labelText, Color active, Color inactive, Vector2 size, float fontSize = 48f, Color? textColor = null)
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
        text.color = textColor ?? Color.white;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.localScale = Vector3.one;
        textRect.localPosition = Vector3.zero;
    }

    public void SetActive(bool active)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        isActive = active;
        text.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
        transitionCoroutine = StartCoroutine(AnimateTransition(active));
    }

    private IEnumerator AnimateTransition(bool toActive)
    {
        Color fromColor = background.color;
        Color toColor = toActive ? activeColor : inactiveColor;
        float fromScale = transform.localScale.x;
        float toScale = toActive ? ActiveScale : InactiveScale;
        float elapsed = 0f;

        while (elapsed < TransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / TransitionDuration);
            t = t * t * (3f - 2f * t); // smoothstep

            background.color = Color.Lerp(fromColor, toColor, t);
            float scale = Mathf.Lerp(fromScale, toScale, t);
            transform.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }

        background.color = toColor;
        transform.localScale = new Vector3(toScale, toScale, 1f);
        transitionCoroutine = null;
    }

    public Button GetButton()
    {
        return button;
    }
}