using UnityEngine;

public readonly struct SdfVisualizationData
{
    public readonly SdfVolumeData Global;
    public readonly SdfVolumeData Local;
    public readonly Transform WorkspaceRoot;
    public readonly Vector3 WorkspaceCorner;
    public readonly Vector3 WorkspaceSize;
    public readonly ComputeBuffer PointsWS;
    public readonly int PointCount;

    public SdfVisualizationData(
        SdfVolumeData global,
        SdfVolumeData local,
        Transform workspaceRoot,
        Vector3 workspaceCorner,
        Vector3 workspaceSize,
        ComputeBuffer pointsWS,
        int pointCount)
    {
        Global = global;
        Local = local;
        WorkspaceRoot = workspaceRoot;
        WorkspaceCorner = workspaceCorner;
        WorkspaceSize = workspaceSize;
        PointsWS = pointsWS;
        PointCount = pointCount;
    }
}
