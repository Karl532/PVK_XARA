using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Visual helpers for runtime-loaded models (wireframe application, material tweaks, etc.).
/// </summary>
public static class RuntimeModelVisualsUtility
{
    /// <summary>
    /// Applies the Azerilo wireframe effect by adding a WireframeShader component
    /// to each renderer in the loaded model hierarchy and disabling the original renderer,
    /// so only the wireframe is visible.
    /// </summary>
    public static void ApplyWireframeEffect(Transform root, Material wireframeMat)
    {
        if (root == null || wireframeMat == null)
        {
            Debug.LogWarning("[RuntimeModelVisualsUtility] ApplyWireframeEffect called with null root or material.");
            return;
        }

        // Configure the wireframe material so it draws clearly on top of the block
        // and other opaque geometry (bright and not occluded).
        ConfigureWireframeMaterial(wireframeMat);

        var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
        int wiredCount = 0;

        foreach (var renderer in renderers)
        {
            if (renderer == null)
                continue;

            // WireframeShader expects a MeshFilter or SkinnedMeshRenderer on the same GameObject.
            var go = renderer.gameObject;

            // Avoid duplicates if something already added one.
            var existing = go.GetComponent<WireframeShader>();
            if (existing == null)
            {
                var wf = go.AddComponent<WireframeShader>();
                wf.wireframeMaterial = wireframeMat;
            }

            // Hide the original shaded mesh so we don't see textures underneath.
            renderer.enabled = false;
            wiredCount++;
        }

        Debug.Log($"[RuntimeModelVisualsUtility] Applied wireframe effect '{wireframeMat.name}' to {wiredCount} renderers.");
    }

    /// <summary>
    /// Tweaks the Azerilo wireframe material so it is always visible and slightly glowing,
    /// even when inside or behind other geometry like the placement block.
    /// </summary>
    private static void ConfigureWireframeMaterial(Material mat)
    {
        if (mat == null)
            return;

        // Render very late so it appears on top of most things.
        mat.renderQueue = (int)RenderQueue.Overlay;

        // If the shader supports depth properties, disable depth writes and relax depth testing.
        if (mat.HasProperty("_ZWrite"))
        {
            mat.SetInt("_ZWrite", 0);
        }
        if (mat.HasProperty("_ZTest"))
        {
            mat.SetInt("_ZTest", (int)CompareFunction.Always);
        }

        // Azerilo shader exposes _ZMode; -1 usually corresponds to a more permissive depth mode.
        if (mat.HasProperty("_ZMode"))
        {
            mat.SetFloat("_ZMode", -1f);
        }

        // Make the wireframe color fully opaque and slightly brighter for readability.
        if (mat.HasProperty("_WireColor"))
        {
            var c = mat.GetColor("_WireColor");
            c.a = 1f;
            c *= 1.3f;
            mat.SetColor("_WireColor", c);
        }
        else if (mat.HasProperty("_Color"))
        {
            var c = mat.GetColor("_Color");
            c.a = 1f;
            c *= 1.3f;
            mat.SetColor("_Color", c);
        }
    }
}

