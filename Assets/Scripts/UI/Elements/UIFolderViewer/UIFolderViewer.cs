using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UI.Utils;

namespace UI.Elements.UIFolderViewer
{
    /// <summary>
    /// Displays files from a folder. User selects a file, then presses "Load model" to invoke the callback.
    /// If the folder is empty or invalid, shows seeded example items.
    /// </summary>
    public class UIFolderViewer : MonoBehaviour
    {
        private static readonly string[] ExampleFiles = { "example_model.obj", "sample_data.json", "default_config.txt" };

        private UnityEngine.Events.UnityAction<string> _onLoadRequested;
        private string _folderPath;
        private ToggleGroup _toggleGroup;
        private RectTransform _scrollContent;

        /// <summary>
        /// Creates the folder viewer. Callback is invoked when "Load model" is pressed with the selected file's full path.
        /// </summary>
        /// <param name="folderPath">Directory to list files from. If empty/invalid, example items are shown.</param>
        /// <param name="onLoadRequested">Invoked with the selected file's full path when Load model is pressed.</param>
        /// <param name="labelColor">Color for the label. Defaults to white.</param>
        /// <param name="accentColor">Color for selection highlight and Load button.</param>
        public void CreateFolderViewer(string folderPath, UnityEngine.Events.UnityAction<string> onLoadRequested, Color? labelColor = null, Color? accentColor = null)
        {
            _folderPath = string.IsNullOrEmpty(folderPath) ? "" : folderPath.Trim();
            _onLoadRequested = onLoadRequested;
            Color accent = accentColor ?? new Color(0.3f, 0.5f, 0.9f, 1f);

            RectTransform rect = UIComponentHelper.GetOrAddComponent<RectTransform>(gameObject);

            VerticalLayoutGroup layout = gameObject.GetComponent<VerticalLayoutGroup>();
            if (layout == null) layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.padding = new RectOffset(0, 8, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            // 1) Label at top (fixed height, layout-first so it reserves space)
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(transform, false);
            labelObj.transform.SetAsFirstSibling();
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(0, 60);
            labelRect.localScale = Vector3.one;
            LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
            labelLE.minHeight = 60;
            labelLE.preferredHeight = 60;
            labelLE.flexibleHeight = 0;
            UIFolderViewerStyling.CreateLabel(labelObj.transform, "Select file", labelColor ?? Color.white, 42f);

            // 2) File list in middle (flexible height)
            GameObject listContainer = UIFolderViewerStyling.CreateFileList(transform, out ScrollRect scrollRect);
            listContainer.transform.SetSiblingIndex(1);
            LayoutElement listLE = listContainer.GetComponent<LayoutElement>();
            if (listLE != null)
            {
                listLE.minHeight = 440;
                listLE.flexibleHeight = 1;
            }

            _toggleGroup = gameObject.AddComponent<ToggleGroup>();
            _toggleGroup.allowSwitchOff = false;

            List<string> files = GetFilesToDisplay();
            if (files.Count == 0)
            {
                string basePath = string.IsNullOrWhiteSpace(_folderPath) ? Application.dataPath : _folderPath.Trim();
                foreach (string example in ExampleFiles)
                    files.Add(Path.Combine(basePath, example));
            }
            _scrollContent = scrollRect.content;
            CreateFileItems(files, scrollRect.content, accent);

            // Force scroll content height so all items are visible (ContentSizeFitter may not run when built inactive)
            float contentHeight = files.Count * UIFolderViewerStyling.ItemHeight + Mathf.Max(0, files.Count - 1) * 6 + 24;
            _scrollContent.sizeDelta = new Vector2(_scrollContent.sizeDelta.x, Mathf.Max(contentHeight, 400));

            // 3) Load button row at bottom
            UIFolderViewerStyling.CreateLoadButton(transform, accent, OnLoadButtonPressed);
            Transform loadRow = transform.Find("LoadButtonRow");
            if (loadRow != null)
                loadRow.SetSiblingIndex(2);

            StartCoroutine(RebuildLayoutNextFrame(scrollRect.content));
        }

        void OnEnable()
        {
            var rect = transform as RectTransform;
            if (rect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            if (_scrollContent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollContent);
        }

        IEnumerator RebuildLayoutNextFrame(RectTransform content)
        {
            yield return null;
            if (content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                Canvas.ForceUpdateCanvases();
            }
        }

        List<string> GetFilesToDisplay()
        {
            var result = new List<string>();

            string pathToUse = string.IsNullOrWhiteSpace(_folderPath) ? "" : _folderPath.Trim();

            if (!string.IsNullOrEmpty(pathToUse) && Directory.Exists(pathToUse))
            {
                try
                {
                    string[] files = Directory.GetFiles(pathToUse);
                    foreach (string f in files)
                        result.Add(f);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"UIFolderViewer: Could not read folder {pathToUse}: {e.Message}");
                }
            }

            if (result.Count == 0)
            {
                string basePath = string.IsNullOrEmpty(pathToUse) ? Application.dataPath : pathToUse;
                foreach (string example in ExampleFiles)
                {
                    result.Add(Path.Combine(basePath, example));
                }
            }

            return result;
        }

        void CreateFileItems(List<string> files, RectTransform content, Color accentColor)
        {
            bool first = true;
            foreach (string fullPath in files)
            {
                string fileName = Path.GetFileName(fullPath);
                GameObject item = UIFolderViewerStyling.CreateFileListItem(content, _toggleGroup, fileName, fullPath, accentColor);
                if (first)
                {
                    var toggle = item.GetComponent<Toggle>();
                    if (toggle != null) toggle.isOn = true;
                    first = false;
                }
            }
        }

        void OnLoadButtonPressed()
        {
            if (_toggleGroup == null) return;

            Toggle active = _toggleGroup.ActiveToggles().FirstOrDefault();
            if (active == null) return;

            var itemData = active.GetComponent<UIFolderViewerItemData>();
            if (itemData != null)
                _onLoadRequested?.Invoke(itemData.FullPath);
        }
    }
}
