using UnityEngine;
using Assets.Scripts.Debug;

public static class ComponentUtility
{
    public static T GetOrAddComponent<T>(GameObject host, Object logContext = null) where T : Component
    {
        if (host == null)
            return null;

        var existing = host.GetComponent<T>();
        if (existing != null)
            return existing;

        var added = host.AddComponent<T>();
        DebugService.Log($"[ComponentUtility] Added component: {typeof(T).Name} (host={host.name})", logContext);
        return added;
    }
}




