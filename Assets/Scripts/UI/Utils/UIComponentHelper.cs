using UnityEngine;

namespace UI.Utils
{
    /// <summary>
    /// Shared helpers for UI component access. Use when a component may or may not already exist.
    /// </summary>
    public static class UIComponentHelper
    {
    /// <summary>
    /// Returns the component of type T if present, otherwise adds and returns it.
    /// </summary>
    public static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
            component = go.AddComponent<T>();
        return component;
    }
}
}
