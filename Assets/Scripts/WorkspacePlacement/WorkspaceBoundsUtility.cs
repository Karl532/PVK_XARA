using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Helper methods for creating and managing the workspace bounds cube used in placement mode.
/// Handles spawning, material setup, XR grab configuration, and visibility.
/// </summary>
public static class WorkspaceBoundsUtility
{
    public static GameObject FindWorkspace(string workspaceName)
    {
        if (string.IsNullOrEmpty(workspaceName))
            return null;
        return GameObject.Find(workspaceName);
    }

    /// <summary>
    /// Creates the workspace bounds in front of the given camera, sized from settings,
    /// parented under the calibration origin, and configured for XR grabbing.
    /// </summary>
    public static GameObject CreateWorkspace(Camera xrCamera, float spawnDistance, Color workspaceColor, Color glowColor)
    {
        Settings settings = SettingsManager.Instance?.settings;
        Vector3 dimensions = settings != null ? settings.stoneBlockDimensions : Vector3.one;

        Transform cameraTransform = xrCamera != null ? xrCamera.transform : null;
        if (cameraTransform == null)
        {
            Debug.LogWarning("[WorkspaceBoundsUtility] xrCamera is null, using world origin as fallback.");
        }

        GameObject workspace = GameObject.CreatePrimitive(PrimitiveType.Cube);
        workspace.name = "Workspace";
        workspace.transform.localScale = dimensions;

        // Position in front of camera
        Vector3 spawnPos = cameraTransform != null
            ? cameraTransform.position + cameraTransform.forward * spawnDistance
            : new Vector3(0, dimensions.y * 0.5f, spawnDistance);

        if (cameraTransform != null)
            spawnPos.y = Mathf.Max(spawnPos.y, cameraTransform.position.y - 0.5f); // Don't spawn too low

        workspace.transform.position = spawnPos;

        // Parent under the calibration origin so placement is relative to the fiducial-tracked world origin.
        CalibrationOriginUtility.AttachToOrigin(workspace.transform, worldPositionStays: true);

        // Semi-transparent visual material
        Renderer renderer = workspace.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = CreateTransparentWorkspaceMaterial(workspaceColor, glowColor);
            renderer.material = mat;
            renderer.material.renderQueue = (int)RenderQueue.Overlay +1;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // Ensure collider is enabled so XR grabs / rays can hit it.
        var col = workspace.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        // Make the workspace grabbable / movable / rotatable with XR controllers, with no gravity.
        TryMakeWorkspaceGrabbable(workspace);

        if (workspace.GetComponent<WorkspaceMovementState>() == null)
            workspace.AddComponent<WorkspaceMovementState>();

        // Newly created workspace should be visible/interactive while in placement mode.
        SetWorkspaceVisibility(workspace, true);

        return workspace;
    }

    /// <summary>
    /// Shows/hides the workspace visuals and interaction, without destroying it.
    /// </summary>
    public static void SetWorkspaceVisibility(GameObject workspace, bool visible)
    {
        SetWorkspaceState(workspace, visible, visible);
    }

    /// <summary>
    /// Shows/hides the workspace visuals independently from interaction.
    /// </summary>
    public static void SetWorkspaceState(GameObject workspace, bool visible, bool interactable)
    {
        if (workspace == null)
            return;

        // Only toggle the workspace's own renderer; do not touch child renderers,
        // so models parented under the workspace remain visible.
        var renderer = workspace.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = visible;

        // Toggle main collider so it can only be grabbed / raycasted when interactable.
        var col = workspace.GetComponent<Collider>();
        if (col != null)
            col.enabled = interactable;

        // Toggle XRGrabInteractable (via reflection) if present so it can't be grabbed when disabled.
        var type = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
        if (type == null)
        {
            type = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
        }

        if (type != null)
        {
            var grab = workspace.GetComponent(type);
            if (grab is Behaviour behaviour)
            {
                behaviour.enabled = interactable;
            }
        }
    }

    private static Material CreateTransparentWorkspaceMaterial(Color workspaceColor, Color glowColor)
    {
        Shader shader = Shader.Find("PVK/PlacementBlockDepthChams")
            ?? Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard");
        Material mat = new Material(shader);

        if (mat.HasProperty("_FrontColor"))
            mat.SetColor("_FrontColor", workspaceColor);
        if (mat.HasProperty("_BackColor"))
            mat.SetColor("_BackColor", glowColor);
        if (mat.HasProperty("_Alpha"))
            mat.SetFloat("_Alpha", Mathf.Clamp01(workspaceColor.a > 0f ? workspaceColor.a : 0.35f));
        if (mat.HasProperty("_OcclusionBias"))
            mat.SetFloat("_OcclusionBias", 0.02f);
        if (mat.HasProperty("_OcclusionSoftness"))
            mat.SetFloat("_OcclusionSoftness", 0.06f);
        if (mat.HasProperty("_UseEnvDepth"))
        {
            var envDepth = Shader.GetGlobalTexture("_EnvironmentDepthTexture");
            mat.SetFloat("_UseEnvDepth", envDepth != null ? 1f : 0f);
        }

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", workspaceColor);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", workspaceColor);
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", glowColor);

        return mat;
    }

    private static void TryMakeWorkspaceGrabbable(GameObject target)
    {
        // Try both pre-3.0 and 3.x namespaces for XRGrabInteractable.
        var type = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
        if (type == null)
        {
            type = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
        }

        if (type == null) return;
        if (target.GetComponent(type) != null) return;

        // Rigidbody configured for no gravity and kinematic so it doesn't fall.
        var rb = target.GetComponent<Rigidbody>();
        if (rb == null)
            rb = target.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

        // Ensure there is an enabled collider for interaction.
        var col = target.GetComponent<Collider>();
        if (col != null)
            col.enabled = true;

        var grab = target.AddComponent(type);

        // Best-effort: disable rotation tracking if supported.
        try
        {
            var trackRotationProp = type.GetProperty("trackRotation");
            if (trackRotationProp != null && trackRotationProp.CanWrite)
            {
                trackRotationProp.SetValue(grab, false);
            }
        }
        catch
        {
            // Ignore if not supported.
        }

        // Best-effort: set movementType to VelocityTracking if available.
        try
        {
            var baseType = type.BaseType;
            while (baseType != null && baseType.FullName != null && !baseType.FullName.Contains("XRBaseInteractable"))
            {
                baseType = baseType.BaseType;
            }

            if (baseType != null)
            {
                var movementTypeProp = baseType.GetProperty("movementType");
                if (movementTypeProp != null && movementTypeProp.CanWrite)
                {
                    var movementEnumType = movementTypeProp.PropertyType;
                    var velocityValue = Enum.Parse(movementEnumType, "VelocityTracking", ignoreCase: true);
                    movementTypeProp.SetValue(grab, velocityValue);
                }
            }
        }
        catch
        {
            // Ignore – defaults are fine if reflection fails.
        }
    }
}
