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
    /// Displays glTF/glb files from a folder. User selects a file, then presses "Load model" to load it.
    /// If the folder is empty or invalid, shows a message telling the user where to put files.
    /// </summary>
    public class UIFolderViewer : MonoBehaviour
    {
        private UnityEngine.Events.UnityAction<string> _onLoadRequested;
        private string _folderPath;
        private ToggleGroup _toggleGroup;
        private RectTransform _scrollContent;
        private Color _labelColor = Color.white;
        private Color _accentColor = new Color(0.3f, 0.5f, 0.9f, 1f);

        /// <summary>
        /// Creates the folder viewer. Only shows .glb and .gltf files. Loads models via RuntimeModelLoader when "Load model" is pressed.
        /// </summary>
        /// <param name="folderPath">Directory to list files from. If empty/invalid or contains no glTF files, shows a message.</param>
        /// <param name="onLoadRequested">Invoked with the selected file's full path when Load model is pressed.</param>
        /// <param name="labelColor">Color for the label. Defaults to white.</param>
        /// <param name="accentColor">Color for selection highlight and Load button.</param>
        public void CreateFolderViewer(string folderPath, UnityEngine.Events.UnityAction<string> onLoadRequested, Color? labelColor = null, Color? accentColor = null)
        {
            _folderPath = string.IsNullOrEmpty(folderPath) ? "" : folderPath.Trim();
            _onLoadRequested = onLoadRequested;
            _labelColor = labelColor ?? Color.white;
            _accentColor = accentColor ?? new Color(0.3f, 0.5f, 0.9f, 1f);
            Color accent = _accentColor;

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

            _scrollContent = scrollRect.content;
            RebuildFileList();

            // 3) Load button row at bottom
            UIFolderViewerStyling.CreateLoadButton(transform, accent, OnLoadButtonPressed);
            UIFolderViewerStyling.CreateRefreshButton(transform, accent, OnRefreshButtonPressed);

            Transform refreshRow = transform.Find("RefreshButtonRow");
            Transform loadRow = transform.Find("LoadButtonRow");
            if (refreshRow != null)
                refreshRow.SetSiblingIndex(2);
            if (loadRow != null)
                loadRow.SetSiblingIndex(3);
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

            // If nothing was configured, fall back to a sensible default so it still works.
            if (string.IsNullOrEmpty(pathToUse))
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                // Quest / Android builds: use app-private persistent storage.
                pathToUse = Path.Combine(Application.persistentDataPath, "Models");
#else
                // Editor / PC: use a Models folder under the project Assets.
                pathToUse = Path.Combine(Application.dataPath, "Models");
#endif
                Debug.Log($"[UIFolderViewer] _folderPath was empty. Falling back to default path: '{pathToUse}'.");
                _folderPath = pathToUse;
            }

            if (!Directory.Exists(pathToUse))
            {
                Debug.LogWarning($"[UIFolderViewer] Directory does NOT exist, creating: '{pathToUse}'.");
                try
                {
                    Directory.CreateDirectory(pathToUse);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[UIFolderViewer] Failed to create directory '{pathToUse}': {e.Message}");
                    return result;
                }
            }

            try
            {
                string[] files = Directory.GetFiles(pathToUse);
                Debug.Log($"[UIFolderViewer] Directory.Exists('{pathToUse}') == true. Total files (all types): {files.Length}");

                foreach (string f in files)
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    Debug.Log($"[UIFolderViewer] Found file: '{f}' (ext='{ext}')");

                    // Only show glTF/glb files
                    if (ext == ".glb" || ext == ".gltf")
                    {
                        result.Add(f);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UIFolderViewer] Could not read folder '{pathToUse}': {e.Message}");
            }

            Debug.Log($"[UIFolderViewer] Listing '{pathToUse}', found {result.Count} .gltf/.glb files.");

            return result;
        }

        void RebuildFileList()
        {
            if (_scrollContent == null)
                return;

            // Clear existing entries
            for (int i = _scrollContent.childCount - 1; i >= 0; i--)
            {
                Destroy(_scrollContent.GetChild(i).gameObject);
            }

            List<string> files = GetFilesToDisplay();

            if (files.Count == 0)
            {
                CreateNoFilesMessage(_scrollContent, _labelColor);
            }
            else
            {
                CreateFileItems(files, _scrollContent, _accentColor);
            }

            // Force scroll content height so all items are visible (ContentSizeFitter may not run when built inactive)
            float contentHeight;
            if (files.Count > 0)
            {
                contentHeight = files.Count * UIFolderViewerStyling.ItemHeight +
                                Mathf.Max(0, files.Count - 1) * 6 + 24;
            }
            else
            {
                // Empty message doesn't need specific height, let ContentSizeFitter handle it
                contentHeight = 400;
            }

            _scrollContent.sizeDelta = new Vector2(_scrollContent.sizeDelta.x, Mathf.Max(contentHeight, 400));

            StartCoroutine(RebuildLayoutNextFrame(_scrollContent));
        }

        public void RefreshFiles()
        {
            RebuildFileList();
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
            if (itemData == null) return;

            string fullPath = itemData.FullPath;

            // Load the model with RuntimeModelLoader (we only show .glb/.gltf files anyway)
            if (RuntimeModelLoader.Instance != null)
            {
                RuntimeModelLoader.Instance.LoadFromPath(fullPath);
                Debug.Log($"[UIFolderViewer] Loading model from: {fullPath}");
            }
            else
            {
                Debug.LogError("[UIFolderViewer] RuntimeModelLoader.Instance is null. Make sure RuntimeModelLoader is added to a GameObject in the scene.");
            }

            // Still invoke callback for backwards compatibility / custom handling
            _onLoadRequested?.Invoke(fullPath);
        }

        void OnRefreshButtonPressed()
        {
            Debug.Log("[UIFolderViewer] Refresh button pressed, rebuilding file list.");
            RebuildFileList();
        }

        void CreateNoFilesMessage(RectTransform content, Color textColor)
        {
            GameObject messageObj = new GameObject("NoFilesMessage");
            messageObj.transform.SetParent(content, false);

            RectTransform messageRect = messageObj.AddComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0, 0.5f);
            messageRect.anchorMax = new Vector2(1, 0.5f);
            messageRect.pivot = new Vector2(0.5f, 0.5f);
            messageRect.anchoredPosition = Vector2.zero;
            // Give it a bit more vertical space so header/description don't feel cramped.
            messageRect.sizeDelta = new Vector2(0, 260);
            messageRect.localScale = Vector3.one;

            VerticalLayoutGroup layout = messageObj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16;
            layout.padding = new RectOffset(40, 40, 20, 20);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            // Let the layout control child heights so texts don't clip each other.
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Main message text
            GameObject mainTextObj = new GameObject("MainText");
            mainTextObj.transform.SetParent(messageObj.transform, false);

            RectTransform mainTextRect = mainTextObj.AddComponent<RectTransform>();
            mainTextRect.anchorMin = Vector2.zero;
            mainTextRect.anchorMax = Vector2.one;
            mainTextRect.sizeDelta = Vector2.zero;
            mainTextRect.localScale = Vector3.one;

            TextMeshProUGUI mainText = mainTextObj.AddComponent<TextMeshProUGUI>();
            mainText.text = "No glTF/glb files found";
            mainText.fontSize = 44;
            mainText.fontStyle = FontStyles.Bold;
            mainText.color = textColor;
            mainText.alignment = TextAlignmentOptions.Center;

            LayoutElement mainTextLayout = mainTextObj.AddComponent<LayoutElement>();
            mainTextLayout.minHeight = 60;
            mainTextLayout.preferredHeight = 60;

            // Directory path text
            GameObject pathTextObj = new GameObject("PathText");
            pathTextObj.transform.SetParent(messageObj.transform, false);

            RectTransform pathTextRect = pathTextObj.AddComponent<RectTransform>();
            pathTextRect.anchorMin = Vector2.zero;
            pathTextRect.anchorMax = Vector2.one;
            pathTextRect.sizeDelta = Vector2.zero;
            pathTextRect.localScale = Vector3.one;

            TextMeshProUGUI pathText = pathTextObj.AddComponent<TextMeshProUGUI>();
            string displayPath = string.IsNullOrWhiteSpace(_folderPath)
                ? Path.Combine(Application.persistentDataPath, "Models")
                : _folderPath.Trim();
            pathText.text = $"To load models, place .glb or .gltf files in:\n<color=#88AAFF>{displayPath}</color>";
            pathText.fontSize = 32;
            pathText.color = new Color(textColor.r * 0.85f, textColor.g * 0.85f, textColor.b * 0.85f, textColor.a);
            pathText.alignment = TextAlignmentOptions.Center;
            pathText.enableWordWrapping = true;

            LayoutElement pathTextLayout = pathTextObj.AddComponent<LayoutElement>();
            pathTextLayout.minHeight = 120;
            pathTextLayout.preferredHeight = 120;
            pathTextLayout.flexibleHeight = 1;
        }
    }
}
