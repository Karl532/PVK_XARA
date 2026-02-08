using UnityEngine;

namespace KeyBinding
{
    /// <summary>
    /// Registry for KeyBindInput components. Parent should always be active so bindings work globally.
    /// </summary>
    public static class KeyBindRegistry
    {
        private static Transform _root;

        public static void SetRoot(Transform root) => _root = root;

        public static T AddInput<T>() where T : KeyBindInput
        {
            if (_root == null)
            {
                Debug.LogWarning("[KeyBindRegistry] Root not set. Keybindings will not work.");
                return null;
            }
            return _root.gameObject.AddComponent<T>();
        }
    }
}
