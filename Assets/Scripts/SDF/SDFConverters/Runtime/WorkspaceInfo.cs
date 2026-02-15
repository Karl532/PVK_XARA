using UnityEngine;

    /// <summary>
    /// Defines the workspace frame and bounds.
    /// Workspace space is the local space of WorkspaceRoot.
    /// CornerWS/SizeWS are expressed in workspace space coordinates.
    /// </summary>
    public readonly struct WorkspaceInfo
    {
        public readonly Transform WorkspaceRoot;
        public readonly Vector3 CornerWS;   // workspace-space min corner
        public readonly Vector3 SizeWS;     // workspace-space size (meters)

        public WorkspaceInfo(Transform workspaceRoot, Vector3 cornerWS, Vector3 sizeWS)
        {
            WorkspaceRoot = workspaceRoot;
            CornerWS = cornerWS;
            SizeWS = sizeWS;
        }

        public Matrix4x4 WorldToWorkspace =>
            WorkspaceRoot != null ? WorkspaceRoot.worldToLocalMatrix : Matrix4x4.identity;

        public Matrix4x4 WorkspaceToWorld =>
            WorkspaceRoot != null ? WorkspaceRoot.localToWorldMatrix : Matrix4x4.identity;
    }

