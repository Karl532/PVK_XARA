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
        // 2) Otherwise, try to find the runtime-created workspace by name ("Workspace").
        Transform workspace = explicitWorkspaceTransform;
        if (workspace == null)
        {
            GameObject workspaceObj = GameObject.Find("Workspace");
            if (workspaceObj != null)
            {
                workspace = workspaceObj.transform;
                Debug.Log("[RuntimeModelPositionUtility] Using runtime-created 'Workspace' as workspace.");
            }
        }

        if (workspace == null)
        {
            if (settings == null)
            {
                Debug.LogWarning("[RuntimeModelPositionUtility] No workspace assigned and no 'Workspace' found.");
                return false;
            }

            // Create a virtual workspace transform from Settings.
            GameObject ws = new GameObject("Workspace_FromSettings");
            ws.transform.position = settings.workspacePosition;
            ws.transform.rotation = Quaternion.Euler(settings.workspaceRotationEuler);
            ws.transform.localScale = settings.stoneBlockDimensions;
            workspace = ws.transform;
        }

        resolvedWorkspace = workspace;
        modelRoot.SetParent(workspace, worldPositionStays: true);

        // Auto-fit: scale model so its largest axis fills the workspace (one-time, sets settings.modelScale).
        if (settings.modelFitToWorkspace)
            FitModelToWorkspace(modelRoot, workspace, settings);

        // Initial alignment; ongoing updates are handled by RepositionModelRelativeToWorkspace.
        RepositionModelRelativeToWorkspace(modelRoot, workspace, settings);
        return true;
    }

    /// <summary>
    /// Scales the model uniformly so that its longest world-space extent matches
    /// the corresponding workspace dimension, filling the workspace as tightly as possible.
    /// Sets settings.modelScale so subsequent frames keep it stable.
    /// </summary>
    public static void FitModelToWorkspace(Transform modelRoot, Transform workspace, Settings settings)
    {
        if (modelRoot == null || workspace == null || settings == null)
            return;

        // Reset scale to 1 first so we measure the model's natural size
        modelRoot.localScale = Vector3.one;

        var renderers = modelRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
        if (renderers.Length == 0) return;

        Bounds mb = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            mb.Encapsulate(renderers[i].bounds);

        Vector3 modelSize = mb.size;
        Vector3 wsSize    = workspace.localScale; // workspace localScale == physical dimensions

        if (modelSize.x < 0.0001f || modelSize.y < 0.0001f || modelSize.z < 0.0001f)
            return;

        // Find the uniform scale that makes the model's largest axis fill the matching workspace axis
        float scaleX = wsSize.x / modelSize.x;
        float scaleY = wsSize.y / modelSize.y;
        float scaleZ = wsSize.z / modelSize.z;

        // Use the smallest ratio so the model always fits inside (no axis overflows)
        float fitScale = Mathf.Min(scaleX, Mathf.Min(scaleY, scaleZ));
        settings.modelScale  = fitScale;
        settings.modelOffset = Vector3.zero;  // centering handled by RepositionModelRelativeToWorkspace

        Debug.Log($"[RuntimeModelPositionUtility] Auto-fitted model scale={fitScale:F3}");
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

        // Compute workspace bounds. Prefer the workspace's own renderer/collider to avoid including the model itself.
        Bounds workspaceBounds;
        var rootRenderer = workspace.GetComponent<Renderer>();
        if (rootRenderer != null)
        {
            workspaceBounds = rootRenderer.bounds;
        }
        else
        {
            var rootCollider = workspace.GetComponent<Collider>();
            if (rootCollider != null)
            {
                workspaceBounds = rootCollider.bounds;
            }
            else
            {
                var workspaceRenderers = workspace.GetComponentsInChildren<Renderer>(includeInactive: true);
                bool found = false;
                workspaceBounds = new Bounds(workspace.position, Vector3.zero);
                for (int i = 0; i < workspaceRenderers.Length; i++)
                {
                    var r = workspaceRenderers[i];
                    if (r == null) continue;
                    if (r.transform != null && r.transform.IsChildOf(modelRoot)) continue;
                    if (!found)
                    {
                        workspaceBounds = r.bounds;
                        found = true;
                    }
                    else
                    {
                        workspaceBounds.Encapsulate(r.bounds);
                    }
                }

                if (!found)
                {
                    Vector3 size = workspace.localScale;
                    workspaceBounds = new Bounds(workspace.position, size);
                }
            }
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

