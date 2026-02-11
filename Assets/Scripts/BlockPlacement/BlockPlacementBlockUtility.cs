using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Helper methods for creating and managing the stone block used in block placement mode.
/// Handles spawning, material setup, XR grab configuration, and visibility.
/// </summary>
public static class BlockPlacementBlockUtility
{
    /// <summary>
    /// Creates the placement block in front of the given camera, sized from settings,
    /// parented under the calibration origin, and configured for XR grabbing.
    /// </summary>
    public static GameObject CreateBlock(Camera xrCamera, float spawnDistance, Color blockColor, Color glowColor)
    {
        Settings settings = SettingsManager.Instance?.settings;
        Vector3 dimensions = settings != null ? settings.stoneBlockDimensions : Vector3.one;

        Transform cameraTransform = xrCamera != null ? xrCamera.transform : null;
        if (cameraTransform == null)
        {
            Debug.LogWarning("[BlockPlacementBlockUtility] xrCamera is null, using world origin as fallback.");
        }

        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = "PlacementBlock";
        block.transform.localScale = dimensions;

        // Position in front of camera
        Vector3 spawnPos = cameraTransform != null
            ? cameraTransform.position + cameraTransform.forward * spawnDistance
            : new Vector3(0, dimensions.y * 0.5f, spawnDistance);

        if (cameraTransform != null)
            spawnPos.y = Mathf.Max(spawnPos.y, cameraTransform.position.y - 0.5f); // Don't spawn too low

        block.transform.position = spawnPos;

        // Parent under the calibration origin so placement is relative to the fiducial-tracked world origin.
        CalibrationOriginUtility.AttachToOrigin(block.transform, worldPositionStays: true);

        // Semi-transparent visual material
        Renderer renderer = block.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = CreateTransparentBlockMaterial(blockColor, glowColor);
            renderer.material = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // Ensure collider is enabled so XR grabs / rays can hit it.
        var col = block.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        // Newly created block should be visible/interactive while in placement mode.
        SetBlockVisibility(block, true);

        return block;
    }

    /// <summary>
    /// Shows/hides the block visuals and interaction, without destroying it.
    /// </summary>
    public static void SetBlockVisibility(GameObject block, bool visible)
    {
        if (block == null)
            return;

        // Only toggle the block's own renderer; do not touch child renderers,
        // so models parented under the block remain visible.
        var renderer = block.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = visible;

        // Toggle main collider so it can only be grabbed / raycasted in placement mode.
        var col = block.GetComponent<Collider>();
        if (col != null)
            col.enabled = visible;

        // Toggle XRGrabInteractable (via reflection) if present so it can't be grabbed when hidden.
        var type = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
        if (type == null)
        {
            type = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
        }

        if (type != null)
        {
            var grab = block.GetComponent(type);
            if (grab is Behaviour behaviour)
            {
                behaviour.enabled = visible;
            }
        }
    }

    private static Material CreateTransparentBlockMaterial(Color blockColor, Color glowColor)
    {
        // Back to basics: visually match the dark UI background color and let the
        // shader handle everything else with its defaults. No depth tricks or
        // custom transparency logic here.
        //
        // Dark theme UI backgroundColor is approximately (0.08, 0.08, 0.12, 0.95).

        Color uiDark = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard");
        Material mat = new Material(shader);

        // URP Lit uses _BaseColor, Standard uses _Color. We just set both if available
        // and otherwise leave all other properties at their defaults.
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", uiDark);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", uiDark);

        // No emission, no renderQueue, no ZWrite/ZTest changes – keep it simple.
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", Color.black);

        return mat;
    }


}

