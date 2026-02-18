using UnityEngine;

public sealed class SdfSculptGuideCarver
{
    private int _frameIndex;

    public void Reset()
    {
        _frameIndex = 0;
    }

    public void CarveIfReady(
        int updateInterval,
        SdfSculptGuideCache cache,
        ComputeBuffer points,
        int pointCount,
        int downsample,
        int maxSteps,
        int pointStart,
        Matrix4x4 worldToWorkspace,
        Vector3 workspaceCorner,
        Vector3 workspaceSize,
        Vector3 cameraWorld)
    {
        if (updateInterval <= 0 || cache == null)
            return;

        _frameIndex++;
        if (_frameIndex % updateInterval != 0)
            return;

        cache.CarveRays(
            points,
            pointCount,
            downsample,
            pointStart,
            maxSteps,
            worldToWorkspace,
            workspaceCorner,
            workspaceSize,
            cameraWorld);
    }
}
