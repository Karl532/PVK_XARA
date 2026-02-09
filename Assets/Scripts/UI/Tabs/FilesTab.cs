using UnityEngine;
using UnityEngine.UI;
using UI.Elements.UIFolderViewer;
using UI.Utils;

public class FilesTab : MonoBehaviour
{
    public static GameObject Create(Transform parent, UIStyle style)
    {
        GameObject content = UIPrimitives.CreateUIElement("FilesTabContent", parent,
            Vector2.zero, Vector2.one);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20;
        layout.padding = new RectOffset(50, 50, 50, 50);

        string folderPath = SettingsManager.Instance?.settings?.folderViewerPath ?? "";

        GameObject viewerGO = UIPrimitives.CreateUIElement("FolderViewer", content.transform,
            Vector2.zero, Vector2.one);
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
