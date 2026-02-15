using System;
using UnityEngine;

public class SdfGenerator : IDisposable
{
    // ===== Internal defaults (no Settings clutter) =====
    private const int GLOBAL_RES = 128;
    private const int LOCAL_RES = 256;

    private const float GLOBAL_MU = 0.05f; // 5cm
    private const float LOCAL_MU = 0.01f; // 1cm

    private static readonly Vector3 LOCAL_MIN_EXTENT = new Vector3(0.10f, 0.10f, 0.10f); // 10cm min size
    private const float LOCAL_MARGIN = LOCAL_MU; // pad around point AABB

    // How much movement/size change triggers local rebuild
    private const float LOCAL_REBUILD_CENTER_FRAC = 0.25f; // 25% of local size
    private const float LOCAL_REBUILD_SIZE_FRAC = 0.15f; // 15% size delta

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

    // cached local bounds
    private Vector3 _lastLocalCorner;
    private Vector3 _lastLocalSize;
    private bool _localHasBounds = false;

    private bool _globalDirty = true;
    private bool _localDirty = true;

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
        _triUploader.Upload(modelMesh, modelLocalToWorkspace);
        _globalDirty = true;
        _localDirty = true;
    }

    public void ForceRebuildGlobal() => _globalDirty = true;
    public void ForceRebuildLocal() => _localDirty = true;

    /// <summary>
    /// Update SDF volumes. All inputs are WORKSPACE space.
    /// pointBuffer layout: float4 per point, position in .xyz
    /// </summary>
    public void Update(ComputeBuffer pointBuffer, int pointCount, Vector3 workspaceCorner, Vector3 workspaceSize)
    {
        if (_triUploader.TriangleBuffer == null || _triUploader.TriangleCount == 0)
            throw new InvalidOperationException("SDF: Initialize() must be called with a model mesh first.");

        if (pointBuffer == null || pointCount <= 0)
        {
            // No points: you can choose to skip local, keep last local, or mark invalid.
            // We'll just skip local updates.
            EnsureGlobal(workspaceCorner, workspaceSize);
            if (_globalDirty) BuildGlobal();
            return;
        }

        EnsureGlobal(workspaceCorner, workspaceSize);

        // 1) Compute point cloud AABB in WORKSPACE space (GPU reduction)
        var aabb = ComputePointAabb(pointBuffer, pointCount);

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

        // 3) Decide whether to rebuild local
        if (!_localHasBounds)
        {
            _localHasBounds = true;
            _localDirty = true;
        }
        else
        {
            Vector3 lastCenter = _lastLocalCorner + _lastLocalSize * 0.5f;
            Vector3 newCenter = localCorner + localSize * 0.5f;

            Vector3 centerDelta = newCenter - lastCenter;
            Vector3 centerThresh = _lastLocalSize * LOCAL_REBUILD_CENTER_FRAC;

            bool moved =
                Mathf.Abs(centerDelta.x) > centerThresh.x ||
                Mathf.Abs(centerDelta.y) > centerThresh.y ||
                Mathf.Abs(centerDelta.z) > centerThresh.z;

            Vector3 sizeDelta = localSize - _lastLocalSize;
            bool resized =
                Mathf.Abs(sizeDelta.x) > _lastLocalSize.x * LOCAL_REBUILD_SIZE_FRAC ||
                Mathf.Abs(sizeDelta.y) > _lastLocalSize.y * LOCAL_REBUILD_SIZE_FRAC ||
                Mathf.Abs(sizeDelta.z) > _lastLocalSize.z * LOCAL_REBUILD_SIZE_FRAC;

            if (moved || resized) _localDirty = true;
        }

        _lastLocalCorner = localCorner;
        _lastLocalSize = localSize;

        EnsureLocal(localCorner, localSize);

        // 4) Build as needed
        if (_globalDirty) BuildGlobal();
        if (_localDirty) BuildLocal();
    }

    // ===== Build Global/Local =====

    private void EnsureGlobal(Vector3 workspaceCorner, Vector3 workspaceSize)
    {
        bool boundsChanged = workspaceCorner != _workspaceCorner || workspaceSize != _workspaceSize;
        if (boundsChanged)
        {
            _workspaceCorner = workspaceCorner;
            _workspaceSize = workspaceSize;
            _globalDirty = true;
        }

        if (!_global.IsAllocated)
        {
            _global = VolumeInternals.Create(GLOBAL_RES, GLOBAL_MU);
            _globalDirty = true;
        }

        _global.Corner = _workspaceCorner;
        _global.Size = _workspaceSize;
    }

    private void EnsureLocal(Vector3 corner, Vector3 size)
    {
        if (!_local.IsAllocated)
        {
            _local = VolumeInternals.Create(LOCAL_RES, LOCAL_MU);
            _localDirty = true;
        }

        _local.Corner = corner;
        _local.Size = size;
    }

    private void BuildGlobal()
    {
        BuildTsdfVolume(_global);
        _globalDirty = false;
    }

    private void BuildLocal()
    {
        BuildTsdfVolume(_local);
        _localDirty = false;
    }

    private void BuildTsdfVolume(VolumeInternals v)
    {
        // 1) Clear seeds + tsdf
        DispatchClear(v);

        // 2) Voxelize mesh into seed volume (writes nearest point seeds on surface voxels)
        DispatchVoxelizeSeeds(v);

        // 3) Jump flood propagate nearest seeds
        DispatchJumpFlood(v);

        // 4) Finalize: compute distance to nearest seed, clamp to Mu, write TSDF (unsigned)
        DispatchFinalize(v);
    }

    // ===== Point AABB reduction =====

    private (Vector3 min, Vector3 max) ComputePointAabb(ComputeBuffer pointBuffer, int pointCount)
    {
        int kernelInit = _csPointAabb.FindKernel("CSInit");
        int kernelReduce = _csPointAabb.FindKernel("CSReduce");

        // First pass outputs one AABB per threadgroup into _aabbGroups
        int threads = 256;
        int groupCount = Mathf.CeilToInt(pointCount / (float)threads);

        EnsureAabbBuffers(groupCount);

        _csPointAabb.SetInt("_PointCount", pointCount);
        _csPointAabb.SetBuffer(kernelInit, "_Points", pointBuffer);
        _csPointAabb.SetBuffer(kernelInit, "_GroupAabbs", _aabbGroups);
        _csPointAabb.Dispatch(kernelInit, groupCount, 1, 1);

        // Reduce group AABBs down to one
        int currentCount = groupCount;
        while (currentCount > 1)
        {
            int outCount = Mathf.CeilToInt(currentCount / (float)threads);
            EnsureAabbScratch(outCount);

            _csPointAabb.SetInt("_AabbCount", currentCount);
            _csPointAabb.SetBuffer(kernelReduce, "_GroupAabbs", _aabbGroups);
            _csPointAabb.SetBuffer(kernelReduce, "_OutAabbs", _aabbScratch);
            _csPointAabb.Dispatch(kernelReduce, outCount, 1, 1);

            // swap
            (_aabbGroups, _aabbScratch) = (_aabbScratch, _aabbGroups);
            currentCount = outCount;
        }

        // Read back the final AABB (2 float3s)
        _aabbReadback ??= new Vector3[2];
        _aabbGroups.GetData(_aabbReadback, 0, 0, 2);

        return (_aabbReadback[0], _aabbReadback[1]);
    }

    // ===== Dispatch helpers =====

    private void DispatchClear(VolumeInternals v)
    {
        int k = _csClear.FindKernel("CSClear");
        _csClear.SetInt("_Resolution", v.Resolution);
        _csClear.SetTexture(k, "_SeedTex", v.SeedA);
        _csClear.SetTexture(k, "_TsdfTex", v.Tsdf);

        int g = Mathf.CeilToInt(v.Resolution / 8f);
        _csClear.Dispatch(k, g, g, g);
    }

    private void DispatchVoxelizeSeeds(VolumeInternals v)
    {
        int k = _csVoxelizeSeeds.FindKernel("CSVoxelizeSeeds");

        _csVoxelizeSeeds.SetInt("_Resolution", v.Resolution);
        _csVoxelizeSeeds.SetVector("_Corner", v.Corner);
        _csVoxelizeSeeds.SetVector("_Size", v.Size);

        _csVoxelizeSeeds.SetInt("_TriangleCount", _triUploader.TriangleCount);
        _csVoxelizeSeeds.SetBuffer(k, "_Triangles", _triUploader.TriangleBuffer);

        _csVoxelizeSeeds.SetTexture(k, "_SeedTex", v.SeedA);

        // One thread per triangle
        int threads = 64;
        int groups = Mathf.CeilToInt((_triUploader.TriangleCount) / (float)threads);
        _csVoxelizeSeeds.Dispatch(k, groups, 1, 1);
    }

    private void DispatchJumpFlood(VolumeInternals v)
    {
        int k = _csJumpFlood.FindKernel("CSJumpFlood");

        int res = v.Resolution;
        int g = Mathf.CeilToInt(res / 8f);

        // Ping-pong between SeedA and SeedB
        bool ping = true;
        for (int step = HighestPowerOfTwoAtMost(res / 2); step >= 1; step /= 2)
        {
            _csJumpFlood.SetInt("_Resolution", res);
            _csJumpFlood.SetInt("_Step", step);
            _csJumpFlood.SetVector("_Corner", v.Corner);
            _csJumpFlood.SetVector("_Size", v.Size);

            if (ping)
            {
                _csJumpFlood.SetTexture(k, "_SeedIn", v.SeedA);
                _csJumpFlood.SetTexture(k, "_SeedOut", v.SeedB);
            }
            else
            {
                _csJumpFlood.SetTexture(k, "_SeedIn", v.SeedB);
                _csJumpFlood.SetTexture(k, "_SeedOut", v.SeedA);
            }

            _csJumpFlood.Dispatch(k, g, g, g);
            ping = !ping;
        }

        // Ensure final seeds end up in SeedA for finalize
        if (!ping)
        {
            // last write went to SeedB, so swap references
            (v.SeedA, v.SeedB) = (v.SeedB, v.SeedA);
        }
    }

    private void DispatchFinalize(VolumeInternals v)
    {
        int k = _csFinalize.FindKernel("CSFinalize");
        int res = v.Resolution;
        int g = Mathf.CeilToInt(res / 8f);

        _csFinalize.SetInt("_Resolution", res);
        _csFinalize.SetFloat("_Mu", v.Mu);
        _csFinalize.SetVector("_Corner", v.Corner);
        _csFinalize.SetVector("_Size", v.Size);

        _csFinalize.SetTexture(k, "_SeedTex", v.SeedA);
        _csFinalize.SetTexture(k, "_TsdfTex", v.Tsdf);

        _csFinalize.Dispatch(k, g, g, g);
    }

    private static int HighestPowerOfTwoAtMost(int x)
    {
        int p = 1;
        while (p * 2 <= x) p *= 2;
        return p;
    }

    // ===== AABB buffers =====
    // Each AABB is 2 float3s: min, max
    private ComputeBuffer _aabbGroups;
    private ComputeBuffer _aabbScratch;
    private Vector3[] _aabbReadback;

    private void EnsureAabbBuffers(int groupCount)
    {
        // Needs at least groupCount entries; each entry stores min+max => 2 Vector3
        int entries = groupCount * 2;
        if (_aabbGroups == null || _aabbGroups.count < entries)
        {
            _aabbGroups?.Release();
            _aabbGroups = new ComputeBuffer(entries, sizeof(float) * 3, ComputeBufferType.Structured);
        }
    }

    private void EnsureAabbScratch(int outCount)
    {
        int entries = outCount * 2;
        if (_aabbScratch == null || _aabbScratch.count < entries)
        {
            _aabbScratch?.Release();
            _aabbScratch = new ComputeBuffer(entries, sizeof(float) * 3, ComputeBufferType.Structured);
        }
    }

    // ===== Volume internals =====
    private struct VolumeInternals
    {
        public int Resolution;
        public float Mu;
        public Vector3 Corner;
        public Vector3 Size;

        public RenderTexture SeedA; // ARGBFloat: xyz=seed pos, w=valid (1/0)
        public RenderTexture SeedB; // ping-pong
        public RenderTexture Tsdf;  // RHalf: unsigned distance clamped to Mu

        public bool IsAllocated => SeedA != null;

        public static VolumeInternals Create(int resolution, float mu)
        {
            var v = new VolumeInternals
            {
                Resolution = resolution,
                Mu = mu,
                SeedA = CreateTex3D(resolution, RenderTextureFormat.ARGBFloat, "SDF_SeedA"),
                SeedB = CreateTex3D(resolution, RenderTextureFormat.ARGBFloat, "SDF_SeedB"),
                Tsdf = CreateTex3D(resolution, RenderTextureFormat.RHalf, "SDF_TSDF"),
            };
            return v;
        }

        private static RenderTexture CreateTex3D(int res, RenderTextureFormat fmt, string name)
        {
            var rt = new RenderTexture(res, res, 0, fmt)
            {
                dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
                volumeDepth = res,
                enableRandomWrite = true,
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = name
            };
            rt.Create();
            return rt;
        }

        public void Release()
        {
            SeedA?.Release(); SeedA = null;
            SeedB?.Release(); SeedB = null;
            Tsdf?.Release(); Tsdf = null;
        }
    }

    public void Dispose()
    {
        _triUploader.Release();

        _global.Release();
        _local.Release();

        _aabbGroups?.Release(); _aabbGroups = null;
        _aabbScratch?.Release(); _aabbScratch = null;
    }
}

