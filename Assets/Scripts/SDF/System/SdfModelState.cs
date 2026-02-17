using UnityEngine;

public sealed class SdfModelState
{
    public GameObject ModelInstance { get; private set; }
    public bool IsInitialized { get; private set; }
    public bool IsDirty { get; private set; }

    public void SetModel(GameObject model)
    {
        if (ModelInstance == model)
            return;

        ModelInstance = model;
        IsDirty = true;
        IsInitialized = false;
        SdfDebug.Log($"[SdfModelState] Model set: {(model ? model.name : "null")}", model);
    }

    public void MarkWorkspaceChanged()
    {
        if (ModelInstance == null)
            return;

        IsDirty = true;
        IsInitialized = false;
        SdfDebug.LogVerbose("[SdfModelState] Workspace changed; model marked dirty.", ModelInstance);
    }

    public bool TryInitialize(SdfGenerator core, WorkspaceInfo workspace)
    {
        if (core == null || ModelInstance == null)
            return false;

        if (!WorkspaceMeshConverter.TryGetModelMesh(
                ModelInstance,
                workspace,
                out Mesh mesh,
                out Matrix4x4 modelLocalToWorkspace,
                out _))
        {
            Debug.LogWarning("[SdfSystem] Could not find MeshFilter/Mesh on modelInstance.");
            return false;
        }

        SdfDebug.Log($"[SdfModelState] Initializing model mesh: {mesh.name} verts={mesh.vertexCount} tris={mesh.triangles.Length / 3}", ModelInstance);
        core.Initialize(mesh, modelLocalToWorkspace);
        IsInitialized = true;
        IsDirty = false;
        SdfDebug.Log("[SdfModelState] Model initialized.", ModelInstance);
        return true;
    }
}
