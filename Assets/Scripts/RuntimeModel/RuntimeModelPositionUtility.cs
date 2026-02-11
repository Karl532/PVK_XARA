using UnityEngine;

/// <summary>
/// Positioning helpers for runtime-loaded models relative to the placement block
/// and Settings (offset and scale).
/// </summary>
public static class RuntimeModelPositionUtility
{
    /// <summary>
    /// Resolves the block transform (explicit or runtime-created) and performs
    /// an initial alignment of the model. Returns true if a valid block was found.
    /// </summary>
    public static bool TryPositionModelInsideBlock(
        Transform modelRoot,
        Transform explicitBlockTransform,
        out Transform resolvedBlock,
        Settings settings)
    {
        resolvedBlock = null;

        if (modelRoot == null)
            return false;

        // Resolve the block transform:
        // 1) Prefer the explicitly assigned reference (if any).
        // 2) Otherwise, try to find the runtime-created placement block by name ("PlacementBlock").
        Transform block = explicitBlockTransform;
        if (block == null)
        {
            GameObject blockObj = GameObject.Find("PlacementBlock");
            if (blockObj != null)
            {
                block = blockObj.transform;
                Debug.Log("[RuntimeModelPositionUtility] Using runtime-created 'PlacementBlock' as blockTransform.");
            }
        }

        if (block == null)
        {
            Debug.LogWarning("[RuntimeModelPositionUtility] No blockTransform assigned and no 'PlacementBlock' found.");
            return false;
        }

        resolvedBlock = block;

        // Initial alignment; ongoing updates are handled by RepositionModelRelativeToBlock.
        RepositionModelRelativeToBlock(modelRoot, block, settings);
        return true;
    }

    /// <summary>
    /// Applies model offset and scale from Settings in real time, keeping the model
    /// aligned inside the block even after placement and while settings change.
    /// </summary>
    public static void RepositionModelRelativeToBlock(
        Transform modelRoot,
        Transform block,
        Settings settings)
    {
        if (modelRoot == null || block == null || settings == null)
            return;

        // Apply uniform scale from settings.modelScale.
        modelRoot.localScale = Vector3.one * settings.modelScale;

        Vector3 offset = settings.modelOffset;

        // Compute world-space bounds of the block.
        var blockRenderers = block.GetComponentsInChildren<Renderer>(includeInactive: true);
        if (blockRenderers.Length == 0)
        {
            Debug.LogWarning("[RuntimeModelPositionUtility] block has no renderers. Cannot compute block bounds.");
            return;
        }

        Bounds blockBounds = blockRenderers[0].bounds;
        for (int i = 1; i < blockRenderers.Length; i++)
        {
            blockBounds.Encapsulate(blockRenderers[i].bounds);
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

        Vector3 blockCenter = blockBounds.center;
        float blockBottomY = blockBounds.min.y;

        Vector3 modelCenter = modelBounds.center;
        Vector3 modelExtents = modelBounds.extents;

        // Interpret offset:
        // - X/Z: offset from block center in local block space.
        // - Y:   extra height above the block bottom.
        Vector3 offsetXZWorld =
            block.right * offset.x +
            block.forward * offset.z;

        float targetCenterY = blockBottomY + offset.y + modelExtents.y;
        Vector3 targetCenterWorld = new Vector3(blockCenter.x, targetCenterY, blockCenter.z) + offsetXZWorld;

        Vector3 delta = targetCenterWorld - modelCenter;
        modelRoot.position += delta;
    }
}

