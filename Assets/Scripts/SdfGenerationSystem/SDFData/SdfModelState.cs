using UnityEngine;
using Assets.Scripts.Debug;

public sealed class SdfModelState
{
    public GameObject ModelInstance { get; private set; }
    public bool IsInitialized { get; private set; }
    public bool IsDirty { get; private set; }
    private Vector3 _lastPosition;
    private Quaternion _lastRotation = Quaternion.identity;
    private Vector3 _lastScale = Vector3.one;
    private bool _hasLastTransform;

    public void SetModel(GameObject model)
    {
        if (ModelInstance == model)
            return;

        ModelInstance = model;
        IsDirty = true;
        IsInitialized = false;
        _hasLastTransform = false;
        DebugService.Log($"[SdfModelState] Model set: {(model ? model.name : "null")}", model);
    }

    public void MarkWorkspaceChanged()
    {
        if (ModelInstance == null)
            return;

        IsDirty = true;
        IsInitialized = false;
        DebugService.LogVerbose("[SdfModelState] Workspace changed; model marked dirty.", ModelInstance);
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
            DebugService.Warn("[SdfGenerationSystem] Could not find MeshFilter/Mesh on modelInstance.");
            return false;
        }

        DebugService.Log($"[SdfModelState] Initializing model mesh: {mesh.name} verts={mesh.vertexCount} tris={mesh.triangles.Length / 3}", ModelInstance);
        core.Initialize(mesh, modelLocalToWorkspace);
        IsInitialized = true;
        IsDirty = false;
        CaptureTransformSnapshot();
        DebugService.Log("[SdfModelState] Model initialized.", ModelInstance);
        return true;
    }

    public bool MarkDirtyIfTransformChanged(
        float positionEps = 1e-4f,
        float rotationEpsDeg = 0.1f,
        float scaleEps = 1e-4f)
    {
        if (ModelInstance == null)
            return false;

        var t = ModelInstance.transform;
        if (!_hasLastTransform)
        {
            CaptureTransformSnapshot();
            return false;
        }

        bool changed =
            Vector3.Distance(_lastPosition, t.position) > positionEps ||
            Quaternion.Angle(_lastRotation, t.rotation) > rotationEpsDeg ||
            Vector3.Distance(_lastScale, t.lossyScale) > scaleEps;

        if (changed)
        {
            IsDirty = true;
            IsInitialized = false;
            CaptureTransformSnapshot();
            DebugService.LogVerbose("[SdfModelState] Model transform changed; model marked dirty.", ModelInstance);
        }

        return changed;
    }

    private void CaptureTransformSnapshot()
    {
        if (ModelInstance == null)
            return;

        var t = ModelInstance.transform;
        _lastPosition = t.position;
        _lastRotation = t.rotation;
        _lastScale = t.lossyScale;
        _hasLastTransform = true;
    }
}





