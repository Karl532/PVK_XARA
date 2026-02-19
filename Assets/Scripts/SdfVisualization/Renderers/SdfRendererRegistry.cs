using Assets.Scripts.Depth.Quest3.OXDepth;
using UnityEngine;

public static class SdfRendererRegistry
{
    public static T GetOrCreate<T>(string name, bool attachToOrigin, Object logContext) where T : Component, ISdfRenderer
    {
        var existing = Object.FindObjectOfType<T>(true);
        if (existing != null)
            return existing;

        var go = new GameObject(name);
        if (attachToOrigin)
            CalibrationOriginUtility.AttachToOrigin(go.transform, worldPositionStays: true);

        var renderer = ComponentUtility.GetOrAddComponent<T>(go, logContext);
        SdfDebug.Log($"[SdfRendererRegistry] Created {name}.", logContext);
        return renderer;
    }
}
