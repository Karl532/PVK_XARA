using UnityEngine;
using Assets.Scripts.Debug;

public partial class SdfGenerator
{
    private (Vector3 corner, Vector3 size) ComputeLocalBounds(
        ComputeBuffer pointBuffer,
        int pointCount,
        Vector3 workspaceCorner,
        Vector3 workspaceSize)
    {
        // 1) Compute point cloud AABB in WORKSPACE space (GPU reduction)
        var aabb = ComputePointAabb(pointBuffer, pointCount);
        DebugService.Log($"[SdfGenerator] AABB result min={aabb.min} max={aabb.max}");

        // 2) Expand + clamp + enforce min size
        var localCorner = aabb.min - new Vector3(LOCAL_MARGIN, LOCAL_MARGIN, LOCAL_MARGIN);
        var localMax = aabb.max + new Vector3(LOCAL_MARGIN, LOCAL_MARGIN, LOCAL_MARGIN);

        // Enforce minimum extent (10cm) around AABB center if needed
        Vector3 center = (localCorner + localMax) * 0.5f;
        Vector3 size = localMax - localCorner;

        size = new Vector3(
            Mathf.Max(size.x, LOCAL_MIN_EXTENT.x),
            Mathf.Max(size.y, LOCAL_MIN_EXTENT.y),
            Mathf.Max(size.z, LOCAL_MIN_EXTENT.z)
        );

        localCorner = center - size * 0.5f;
        localMax = center + size * 0.5f;

        // Clamp to workspace bounds
        Vector3 wsMin = workspaceCorner;
        Vector3 wsMax = workspaceCorner + workspaceSize;

        localCorner = new Vector3(
            Mathf.Max(localCorner.x, wsMin.x),
            Mathf.Max(localCorner.y, wsMin.y),
            Mathf.Max(localCorner.z, wsMin.z)
        );

        localMax = new Vector3(
            Mathf.Min(localMax.x, wsMax.x),
            Mathf.Min(localMax.y, wsMax.y),
            Mathf.Min(localMax.z, wsMax.z)
        );

        Vector3 localSize = localMax - localCorner;

        DebugService.Log($"[SdfGenerator] Local bounds corner={localCorner} size={localSize}");

        _lastLocalCorner = localCorner;
        _lastLocalSize = localSize;

        return (localCorner, localSize);
    }
}




