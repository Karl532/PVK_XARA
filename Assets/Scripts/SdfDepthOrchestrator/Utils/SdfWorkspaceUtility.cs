using UnityEngine;

public static class SdfWorkspaceUtility
{
    public static void ResolveInputMatrices(Transform workspaceRoot, out Matrix4x4 inputToWorld, out Matrix4x4 inputToWorkspace)
    {
        inputToWorld = Matrix4x4.identity;
        inputToWorkspace = Matrix4x4.identity;

        if (workspaceRoot == null)
            return;

        inputToWorkspace = workspaceRoot.worldToLocalMatrix;
    }
}
