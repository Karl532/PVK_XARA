using UnityEngine;

public static class SdfModelUtility
{
    public static GameObject FindRuntimeModel(string modelRootName)
    {
        if (string.IsNullOrEmpty(modelRootName))
            return null;

        return GameObject.Find(modelRootName);
    }

    public static bool IsModelReady(string modelRootName, Settings settings)
    {
        if (FindRuntimeModel(modelRootName) != null)
            return true;

        return settings != null && settings.targetSculpturePrefab != null;
    }
}
