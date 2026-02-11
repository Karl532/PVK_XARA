using UnityEngine;

/// <summary>
/// Positioning helpers for runtime-loaded models relative to the workspace bounds
/// and Settings (offset and scale).
/// </summary>
public static class RuntimeModelPositionUtility
{
    /// <summary>
    /// Resolves the workspace transform (explicit or runtime-created) and performs
    /// an initial alignment of the model. Returns true if a valid workspace was found.
    /// </summary>
    public static bool TryPositionModelInsideWorkspace(
        Transform modelRoot,
        Transform explicitWorkspaceTransform,
        out Transform resolvedWorkspace,
        Settings settings)
    {
        resolvedWorkspace = null;

        if (modelRoot == null)
            return false;

        // Resolve the workspace transform:
        // 1) Prefer the explicitly assigned reference (if any).
        // 2) Otherwise, try to find the runtime-created workspace by name ("PlacementBlock").
        Transform workspace = explicitWorkspaceTransform;
        if (workspace == null)
        {
            GameObject workspaceObj = GameObject.Find("PlacementBlock");
            if (workspaceObj != null)
            {
                workspace = workspaceObj.transform;
                Debug.Log("[RuntimeModelPositionUtility] Using runtime-created 'PlacementBlock' as workspace.");
            }
        }

        if (workspace == null)
        {
            Debug.LogWarning("[RuntimeModelPositionUtility] No workspace assigned and no 'PlacementBlock' found.");
            return false;
        }

        resolvedWorkspace = workspace;

        // Initial alignment; ongoing updates are handled by RepositionModelRelativeToWorkspace.
        RepositionModelRelativeToWorkspace(modelRoot, workspace, settings);
        return true;
    }

    /// <summary>
    /// Applies model offset and scale from Settings in real time, keeping the model
    /// aligned inside the workspace even after placement and while settings change.
    /// </summary>
    public static void RepositionModelRelativeToWorkspace(
        Transform modelRoot,
        Transform workspace,
        Settings settings)
    {
        if (modelRoot == null || workspace == null || settings == null)
            return;

        // Apply uniform scale from settings.modelScale.
        modelRoot.localScale = Vector3.one * settings.modelScale;

        Vector3 offset = settings.modelOffset;

        // Compute world-space bounds of the workspace.
        var workspaceRenderers = workspace.GetComponentsInChildren<Renderer>(includeInactive: true);
        if (workspaceRenderers.Length == 0)
        {
            Debug.LogWarning("[RuntimeModelPositionUtility] workspace has no renderers. Cannot compute workspace bounds.");
            return;
        }

        Bounds workspaceBounds = workspaceRenderers[0].bounds;
        for (int i = 1; i < workspaceRenderers.Length; i++)
        {
            workspaceBounds.Encapsulate(workspaceRenderers[i].bounds);
        }

        // Compute world-space bounds of the model.
        var modelRenderers = modelRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
        if (modelRenderers.Length == 0)
        {
            Debug.LogWarning("[RuntimeModelPositionUtility] Loaded model has no renderers. Cannot compute model bounds.");
            return;
        }

        Bounds modelBounds = modelRenderers[0].bounds;
        for (int i = 1; i < modelRenderers.Length; i++)
        {
            modelBounds.Encapsulate(modelRenderers[i].bounds);
        }

        Vector3 workspaceCenter = workspaceBounds.center;
        float workspaceBottomY = workspaceBounds.min.y;

        Vector3 modelCenter = modelBounds.center;
        Vector3 modelExtents = modelBounds.extents;

        // Interpret offset:
        // - X/Z: offset from workspace center in local workspace space.
        // - Y:   extra height above the workspace bottom.
        Vector3 offsetXZWorld =
            workspace.right * offset.x +
            workspace.forward * offset.z;

        float targetCenterY = workspaceBottomY + offset.y + modelExtents.y;
        Vector3 targetCenterWorld = new Vector3(workspaceCenter.x, targetCenterY, workspaceCenter.z) + offsetXZWorld;

        Vector3 delta = targetCenterWorld - modelCenter;
        modelRoot.position += delta;
    }
}

