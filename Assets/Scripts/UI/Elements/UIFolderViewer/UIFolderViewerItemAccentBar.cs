using UnityEngine;
using UnityEngine.UI;

namespace UI.Elements.UIFolderViewer
{
    /// <summary>
    /// Shows/hides a left accent bar based on the parent Toggle's selection state.
    /// </summary>
    public class UIFolderViewerItemAccentBar : MonoBehaviour
    {
        private Image _bar;
        private Toggle _toggle;

        public void Init(Color color, Toggle toggle)
        {
            _bar = GetComponent<Image>();
            _toggle = toggle;
            if (_bar != null) _bar.color = color;
            if (_toggle != null)
            {
                _bar.enabled = _toggle.isOn;
                _toggle.onValueChanged.AddListener(OnToggleChanged);
            }
        }

        void OnToggleChanged(bool isOn)
        {
            if (_bar != null) _bar.enabled = isOn;
        }

        void OnDestroy()
        {
            if (_toggle != null)
                _toggle.onValueChanged.RemoveListener(OnToggleChanged);
        }
    }
}
