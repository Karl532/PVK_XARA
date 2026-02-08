using UnityEngine;

namespace KeyBinding
{
    /// <summary>
    /// Holds KeyBindingHandler components. Parent should always be active so bindings work globally.
    /// </summary>
    public static class KeyBindingHolder
    {
        private static Transform _root;

        public static void SetRoot(Transform root) => _root = root;

        public static T AddKeyBinding<T>() where T : KeyBindingHandler
        {
            if (_root == null)
            {
                Debug.LogWarning("[KeyBindingHolder] Root not set. Keybindings will not work.");
                return null;
            }
            return _root.gameObject.AddComponent<T>();
        }
    }
}
