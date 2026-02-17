using System;
using UnityEngine;

public partial class SdfGenerator : IDisposable
{
    // ===== Internal defaults (no Settings clutter) =====
    private int _globalRes = 128;
    private int _localRes = 256;

    private const float GLOBAL_MU = 0.05f; // 5cm
    private const float LOCAL_MU = 0.01f; // 1cm

    private static readonly Vector3 LOCAL_MIN_EXTENT = new Vector3(0.10f, 0.10f, 0.10f); // 10cm min size
    private const float LOCAL_MARGIN = LOCAL_MU; // pad around point AABB

    // ===== Shaders =====
    private readonly ComputeShader _csPointAabb;
    private readonly ComputeShader _csClear;
    private readonly ComputeShader _csVoxelizeSeeds;
    private readonly ComputeShader _csJumpFlood;
    private readonly ComputeShader _csFinalize;

    // ===== Model triangles =====
    private readonly MeshTriangleUploader _triUploader = new MeshTriangleUploader();

    // ===== Volume internals =====
    private VolumeInternals _global;
    private VolumeInternals _local;

    // workspace bounds for global
    private Vector3 _workspaceCorner;
    private Vector3 _workspaceSize;

    // cached local bounds (debug)
    private Vector3 _lastLocalCorner;
    private Vector3 _lastLocalSize;
    private float _lastLocalBuildTime = -999f;
    private float _lastGlobalBuildTime = -999f;

    public bool EnableLocalVolume { get; set; } = true;
    public float GlobalRebuildMinIntervalSeconds { get; set; } = 0f;
    public float LocalRebuildMinIntervalSeconds { get; set; } = 0f;
    public int GlobalResolution => _globalRes;
    public int LocalResolution => _localRes;
    public int MaxStagesPerFrame { get; set; } = 1;

    public SdfVolumeData Global => new SdfVolumeData(_global.Tsdf, _global.Corner, _global.Size, _global.Resolution, _global.Mu);
    public SdfVolumeData Local => new SdfVolumeData(_local.Tsdf, _local.Corner, _local.Size, _local.Resolution, _local.Mu);

    public SdfGenerator(
        ComputeShader pointAabbCS,
        ComputeShader clearCS,
        ComputeShader voxelizeSeedsCS,
        ComputeShader jumpFloodCS,
        ComputeShader finalizeCS)
    {
        _csPointAabb = pointAabbCS;
        _csClear = clearCS;
        _csVoxelizeSeeds = voxelizeSeedsCS;
        _csJumpFlood = jumpFloodCS;
        _csFinalize = finalizeCS;

        if (_csPointAabb == null || _csClear == null || _csVoxelizeSeeds == null || _csJumpFlood == null || _csFinalize == null)
            throw new ArgumentException("SDF: Missing one or more compute shaders.");
    }

    /// <summary>
    /// Uploads mesh triangles into a GPU buffer in WORKSPACE space.
    /// You must provide the model's local->workspace matrix.
    /// </summary>
    public void Initialize(Mesh modelMesh, Matrix4x4 modelLocalToWorkspace)
    {
        SdfDebug.Log($"[SdfGenerator] Initialize mesh={modelMesh.name} verts={modelMesh.vertexCount} tris={modelMesh.triangles.Length / 3}");
        _triUploader.Upload(modelMesh, modelLocalToWorkspace);
        _lastGlobalBuildTime = -999f;
        _lastLocalBuildTime = -999f;
    }

    public void ForceRebuildGlobal() => _lastGlobalBuildTime = -999f;
    public void ForceRebuildLocal() => _lastLocalBuildTime = -999f;

    public void SetResolutions(int globalRes, int localRes)
    {
        _globalRes = Mathf.Clamp(globalRes, 32, 256);
        _localRes = Mathf.Clamp(localRes, 32, 512);
    }

    /// <summary>
    /// Update SDF volumes. All inputs are WORKSPACE space.
    /// pointBuffer layout: float4 per point, position in .xyz
    /// </summary>
    public void Update(ComputeBuffer pointBuffer, int pointCount, Vector3 workspaceCorner, Vector3 workspaceSize)
    {
        SdfDebug.Log($"[SdfGenerator] Update enter: pointBufferNull={pointBuffer == null} pointCount={pointCount} wsCorner={workspaceCorner} wsSize={workspaceSize} localEnabled={EnableLocalVolume}");

        if (_triUploader.TriangleBuffer == null || _triUploader.TriangleCount == 0)
            throw new InvalidOperationException("SDF: Initialize() must be called with a model mesh first.");

        if (pointBuffer == null || pointCount <= 0)
        {
            // No points: you can choose to skip local, keep last local, or mark invalid.
            // We'll just skip local updates.
            EnsureGlobal(workspaceCorner, workspaceSize);
            TryBuildGlobal();
            SdfDebug.Log("[SdfGenerator] No points; only global updated if due.");
            return;
        }

        EnsureGlobal(workspaceCorner, workspaceSize);

        if (!EnableLocalVolume)
        {
            TryBuildGlobal();
            SdfDebug.Log("[SdfGenerator] Local volume disabled; only global updated if due.");
            return;
        }

        var (localCorner, localSize) = ComputeLocalBounds(pointBuffer, pointCount, workspaceCorner, workspaceSize);

        EnsureLocal(localCorner, localSize);

        // 4) Build based on time intervals
        TryBuildGlobal();
        TryBuildLocal();
    }

    private void TryBuildGlobal()
    {
        float now = Time.unscaledTime;
        if (GlobalRebuildMinIntervalSeconds > 0f &&
            now - _lastGlobalBuildTime < GlobalRebuildMinIntervalSeconds)
        {
            return;
        }

        SdfDebug.Log("[SdfGenerator] BuildGlobal triggered.");
        EnqueueBuildJob(_global, isGlobal: true);
        _lastGlobalBuildTime = now;
    }

    private void TryBuildLocal()
    {
        float now = Time.unscaledTime;
        if (LocalRebuildMinIntervalSeconds > 0f &&
            now - _lastLocalBuildTime < LocalRebuildMinIntervalSeconds)
        {
            return;
        }

        SdfDebug.Log("[SdfGenerator] BuildLocal triggered.");
        EnqueueBuildJob(_local, isGlobal: false);
        _lastLocalBuildTime = now;
    }

    // ===== Build Global/Local =====

    private void EnsureGlobal(Vector3 workspaceCorner, Vector3 workspaceSize)
    {
        bool boundsChanged = workspaceCorner != _workspaceCorner || workspaceSize != _workspaceSize;
        if (boundsChanged)
        {
            _workspaceCorner = workspaceCorner;
            _workspaceSize = workspaceSize;
            _lastGlobalBuildTime = -999f;
            SdfDebug.LogVerbose($"[SdfGenerator] Global bounds changed. corner={_workspaceCorner} size={_workspaceSize}");
        }

        if (!_global.IsAllocated)
        {
            _global = VolumeInternals.Create(_globalRes, GLOBAL_MU);
            _lastGlobalBuildTime = -999f;
            SdfDebug.LogVerbose($"[SdfGenerator] Global volume allocated res={_globalRes} mu={GLOBAL_MU}");
        }
        else if (_global.Resolution != _globalRes)
        {
            _global.Release();
            _global = VolumeInternals.Create(_globalRes, GLOBAL_MU);
            _lastGlobalBuildTime = -999f;
            SdfDebug.LogVerbose($"[SdfGenerator] Global volume reallocated res={_globalRes} mu={GLOBAL_MU}");
        }

        _global.Corner = _workspaceCorner;
        _global.Size = _workspaceSize;
    }

    private void EnsureLocal(Vector3 corner, Vector3 size)
    {
        if (!_local.IsAllocated)
        {
            _local = VolumeInternals.Create(_localRes, LOCAL_MU);
            _lastLocalBuildTime = -999f;
            SdfDebug.LogVerbose($"[SdfGenerator] Local volume allocated res={_localRes} mu={LOCAL_MU}");
        }
        else if (_local.Resolution != _localRes)
        {
            _local.Release();
            _local = VolumeInternals.Create(_localRes, LOCAL_MU);
            _lastLocalBuildTime = -999f;
            SdfDebug.LogVerbose($"[SdfGenerator] Local volume reallocated res={_localRes} mu={LOCAL_MU}");
        }

        _local.Corner = corner;
        _local.Size = size;
    }

    // BuildGlobal/BuildLocal removed; enqueue directly in TryBuildGlobal/TryBuildLocal.

    public void Dispose()
    {
        _triUploader.Release();

        _global.Release();
        _local.Release();

        _aabbGroups?.Release(); _aabbGroups = null;
        _aabbScratch?.Release(); _aabbScratch = null;
        SdfDebug.LogVerbose("[SdfGenerator] Disposed.");
    }
}

