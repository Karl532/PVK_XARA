using System;
using UnityEngine;

    /// <summary>
    /// Extracts a Mesh from a model GameObject and provides the transform to workspace space.
    /// V1: assumes a single MeshFilter in children is the model.
    /// </summary>
    public static class WorkspaceMeshConverter
    {
        public static bool TryGetModelMesh(
            GameObject modelRoot,
            WorkspaceInfo workspace,
            out Mesh mesh,
            out Matrix4x4 modelLocalToWorkspace,
            out MeshFilter meshFilterUsed)
        {
            mesh = null;
            modelLocalToWorkspace = Matrix4x4.identity;
            meshFilterUsed = null;

            if (modelRoot == null) return false;

            // Pick the first MeshFilter found
            var mf = modelRoot.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return false;

            meshFilterUsed = mf;
            mesh = mf.sharedMesh;

            // Model local -> World -> Workspace
            Matrix4x4 worldToWorkspace = workspace.WorldToWorkspace;
            Matrix4x4 modelLocalToWorld = mf.transform.localToWorldMatrix;

            modelLocalToWorkspace = worldToWorkspace * modelLocalToWorld;
            return true;
        }

        /// <summary>
        /// Convenience: computes camera->workspace from camera->world and workspace.WorldToWorkspace.
        /// </summary>
        public static Matrix4x4 CameraToWorkspace(Camera cam, WorkspaceInfo workspace)
        {
            if (cam == null) throw new ArgumentNullException(nameof(cam));
            // camera local -> world is cam.transform.localToWorldMatrix
            return workspace.WorldToWorkspace * cam.transform.localToWorldMatrix;
        }
    }

