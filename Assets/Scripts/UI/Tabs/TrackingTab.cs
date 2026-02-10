using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrackingTab : MonoBehaviour
{
    public static GameObject Create(Transform parent, UIStyle style)
    {
        Color accentColor = style.accentColor;
        Color textColor = style.textColor;

        GameObject content = new GameObject("TrackingContent");
        content.transform.SetParent(parent, false);

        RectTransform rect = content.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 35;
        layout.padding = new RectOffset(50, 50, 50, 50);
        layout.childAlignment = TextAnchor.UpperLeft;

        // Header
        GameObject headerSection = UILayoutFactory.CreateLayoutSection(content.transform, "TrackingHeader", 90);
        UILayoutFactory.CreateHeader(headerSection, "Tracking", 90, accentColor, textColor, 15f, 2800f, 42f);

        // "Set origin now" button
        CreateSetOriginButton(content.transform, accentColor, textColor, style.cornerRadius);

        return content;
    }

    private static void CreateSetOriginButton(Transform parent, Color accentColor, Color textColor, float cornerRadius)
    {
        GameObject buttonGO = new GameObject("SetOriginButton");
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(0, 0.5f);
        rect.pivot = new Vector2(0, 0.5f);
        rect.sizeDelta = new Vector2(900f, 140f);
        rect.localScale = Vector3.one;

        LayoutElement le = buttonGO.AddComponent<LayoutElement>();
        le.minWidth = 900f;
        le.preferredWidth = 900f;
        le.minHeight = 140f;
        le.preferredHeight = 140f;

        Image bg = buttonGO.AddComponent<Image>();
        bg.color = accentColor;

        RoundedImage rounded = buttonGO.AddComponent<RoundedImage>();
        rounded.SetRadius(cornerRadius);

        Button button = buttonGO.AddComponent<Button>();

        // Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(buttonGO.transform, false);

        RectTransform labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = "Set origin now";
        label.alignment = TextAlignmentOptions.Center;
        label.color = textColor;
        label.fontSize = 42f;
        label.enableWordWrapping = false;

        button.onClick.AddListener(() =>
        {
            var controller = Object.FindFirstObjectByType<CalibrationOriginController>();
            if (controller != null)
            {
                controller.CalibrateNow();
            }
            else
            {
                Debug.LogWarning("[TrackingTab] No CalibrationOriginController found in scene when trying to set origin.");
            }
        });
    }
}

