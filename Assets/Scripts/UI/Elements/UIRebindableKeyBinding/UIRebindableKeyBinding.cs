using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using UI.Utils;
using KeyBinding;

namespace UI.Elements.UIRebindableKeyBinding
{
    /// <summary>
    /// UI element: label + tappable box. Tap the box to enter listen mode, then press a controller button to bind.
    /// </summary>
    public class UIRebindableKeyBinding : MonoBehaviour
    {
        private static readonly OVRInput.Button[] RebindableButtons =
        {
            OVRInput.Button.One,
            OVRInput.Button.Two,
            OVRInput.Button.Three,
            OVRInput.Button.Four,
            OVRInput.Button.PrimaryIndexTrigger,
            OVRInput.Button.SecondaryIndexTrigger,
            OVRInput.Button.PrimaryHandTrigger,
            OVRInput.Button.SecondaryHandTrigger,
            OVRInput.Button.Start
        };

        private static readonly string[] ButtonDisplayNames =
        {
            "X/A", "Y/B", "Menu", "B",
            "L Trigger", "R Trigger", "L Grip", "R Grip",
            "Start"
        };

        private KeyBindInput _keyBindInput;
        private TextMeshProUGUI _keyDisplayText;
        private Button _keyBox;
        private bool _listening;
        private UnityAction _onAction;
        private OVRInput.Controller _controller;

        public void Create(string label, OVRInput.Button defaultButton, UnityAction onAction, Color accentColor, Color textColor)
        {
            _onAction = onAction;
            _controller = OVRInput.Controller.Touch;
            UIRebindableKeyBindingStyling.Create(this, label, defaultButton, textColor);
        }

        internal void SetReferences(KeyBindInput keyBindInput, TextMeshProUGUI keyDisplayText, Button keyBox)
        {
            _keyBindInput = keyBindInput;
            _keyDisplayText = keyDisplayText;
            _keyBox = keyBox;

            _keyBox.onClick.AddListener(OnKeyBoxClicked);
            _keyBindInput.SetBinding(_keyBindInput.Button, _onAction);
            UpdateDisplay();
        }

        void Update()
        {
            if (!_listening) return;

            foreach (var btn in RebindableButtons)
            {
                if (OVRInput.Get(btn, _controller))
                {
                    _keyBindInput.Button = btn;
                    _keyBindInput.SetBinding(btn, _onAction);
                    _keyBindInput.enabled = true;
                    _listening = false;
                    UpdateDisplay();
                    _keyBox.interactable = true;
                    return;
                }
            }
        }

        void OnKeyBoxClicked()
        {
            if (_listening) return;
            _listening = true;
            _keyDisplayText.text = "Press...";
            _keyBox.interactable = false;
            if (_keyBindInput != null) _keyBindInput.enabled = false;
        }

        void UpdateDisplay()
        {
            if (_keyDisplayText == null) return;
            int index = System.Array.IndexOf(RebindableButtons, _keyBindInput.Button);
            _keyDisplayText.text = index >= 0 ? ButtonDisplayNames[index] : _keyBindInput.Button.ToString();
        }

        public static string GetButtonDisplayName(OVRInput.Button button)
        {
            int i = System.Array.IndexOf(RebindableButtons, button);
            return i >= 0 ? ButtonDisplayNames[i] : button.ToString();
        }
    }
}
