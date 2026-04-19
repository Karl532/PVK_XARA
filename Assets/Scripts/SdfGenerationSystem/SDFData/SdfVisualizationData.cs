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

    //fix for the scaling issues of the renderers, if we create the matricies here we don't have to do it in the actual renderers
    //  use this one to transform workspace coords to world coords
    public Matrix4x4 UnscaledWorkspaceToWorldMatrix => WorkspaceRoot != null
    ? Matrix4x4.TRS(WorkspaceRoot.position, WorkspaceRoot.rotation, Vector3.one)
    : Matrix4x4.identity;
    //  and use this one to transform world coords to workspace coords
    public Matrix4x4 UnscaledWorldToWorkspaceMatrix => UnscaledWorkspaceToWorldMatrix.inverse;


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
