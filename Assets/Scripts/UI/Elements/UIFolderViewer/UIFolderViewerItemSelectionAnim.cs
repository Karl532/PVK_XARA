using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UI.Elements.UIFolderViewer
{
    /// <summary>
    /// Animates the background Image color to accent (blue) when the Toggle is selected.
    /// </summary>
    public class UIFolderViewerItemSelectionAnim : MonoBehaviour
    {
        private Image _image;
        private Toggle _toggle;
        private Color _normalColor;
        private Color _selectedColor;
        private float _duration;
        private Coroutine _coroutine;

        public void Init(Image image, Toggle toggle, Color normalColor, Color selectedColor, float duration = 0.2f)
        {
            _image = image;
            _toggle = toggle;
            _normalColor = normalColor;
            _selectedColor = selectedColor;
            _duration = Mathf.Max(0.05f, duration);

            if (_toggle != null)
            {
                _toggle.transition = Selectable.Transition.None;
                _image.color = _toggle.isOn ? _selectedColor : _normalColor;
                _toggle.onValueChanged.AddListener(OnToggleChanged);
            }
        }

        void OnToggleChanged(bool isOn)
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);
            _coroutine = StartCoroutine(AnimateTo(isOn ? _selectedColor : _normalColor));
        }

        IEnumerator AnimateTo(Color target)
        {
            if (_image == null) yield break;
            Color start = _image.color;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / _duration;
                float s = Mathf.SmoothStep(0f, 1f, t);
                _image.color = Color.Lerp(start, target, s);
                yield return null;
            }
            _image.color = target;
            _coroutine = null;
        }

        void OnDestroy()
        {
            if (_toggle != null)
                _toggle.onValueChanged.RemoveListener(OnToggleChanged);
            if (_coroutine != null)
                StopCoroutine(_coroutine);
        }
    }
}
