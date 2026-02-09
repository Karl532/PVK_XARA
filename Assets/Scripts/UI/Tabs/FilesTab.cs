using UnityEngine;
using UnityEngine.UI;
using UI.Elements.UIFolderViewer;

public class FilesTab : MonoBehaviour
{
    public static GameObject Create(Transform parent, UIStyle style)
    {
        GameObject content = new GameObject("FilesTabContent");
        content.transform.SetParent(parent, false);

        RectTransform rect = content.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20;
        layout.padding = new RectOffset(50, 50, 50, 50);

        string folderPath = SettingsManager.Instance?.settings?.folderViewerPath ?? "";

        GameObject viewerGO = new GameObject("FolderViewer");
        viewerGO.transform.SetParent(content.transform, false);
        LayoutElement viewerLayout = viewerGO.AddComponent<LayoutElement>();
        viewerLayout.minHeight = 400;
        viewerLayout.flexibleHeight = 1;

        UIFolderViewer viewer = viewerGO.AddComponent<UIFolderViewer>();
        viewer.CreateFolderViewer(folderPath, OnLoadRequested, style.textColor, style.accentColor);

        return content;
    }

    static void OnLoadRequested(string path)
    {
        // User will implement load logic here
        Debug.Log($"[FilesTab] Load model: {path}");
    }
}
