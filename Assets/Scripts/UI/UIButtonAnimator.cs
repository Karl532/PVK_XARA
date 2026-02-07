using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class UIButtonAnimator : MonoBehaviour
{
    private RectTransform rectTransform;
    private Vector3 originalScale = Vector3.one;
    private Vector3 targetScale = Vector3.one;
    private Color baseColor;
    private bool isHovered = false;
    private bool isPressed = false;
    
    private const float HOVER_SCALE = 1.05f;
    private const float PRESS_SCALE = 0.95f;
    private const float ANIMATION_SPEED = 8f;
    
    public void Initialize(Color buttonColor)
    {
        rectTransform = GetComponent<RectTransform>();
        baseColor = buttonColor;
        originalScale = rectTransform.localScale;
    }
    
    void Update()
    {
        // Smooth scale animation
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.Lerp(
                rectTransform.localScale,
                targetScale,
                Time.deltaTime * ANIMATION_SPEED
            );
        }
    }
    
    public void OnPointerEnter()
    {
        isHovered = true;
        UpdateScale();
    }
    
    public void OnPointerExit()
    {
        isHovered = false;
        UpdateScale();
    }
    
    public void OnPointerDown()
    {
        isPressed = true;
        UpdateScale();
    }
    
    public void OnPointerUp()
    {
        isPressed = false;
        UpdateScale();
    }
    
    void UpdateScale()
    {
        if (isPressed)
        {
            targetScale = originalScale * PRESS_SCALE;
        }
        else if (isHovered)
        {
            targetScale = originalScale * HOVER_SCALE;
        }
        else
        {
            targetScale = originalScale;
        }
    }
}
