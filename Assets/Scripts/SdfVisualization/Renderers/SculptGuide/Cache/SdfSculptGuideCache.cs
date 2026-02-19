using UnityEngine;
using Assets.Scripts.Debug;
using UnityEngine.Rendering;

public sealed class SdfSculptGuideCache
{
    public event System.Action CacheCleared;
    private RenderTexture _cacheTex;
    private ComputeShader _cs;
    private int _kClear;
    private int _kAccumulatePoints;
    private int _kCarveRays;
    private int _kDecay;
    private int _kCarveDepthRays;
    private bool _ready;
    private ComputeShader _blurCs;
    private int _kBlur = -1;
    private RenderTexture _blurTex;

    private Vector3 _lastCorner;
    private Vector3 _lastSize;
    private int _lastResolution;
    private bool _hasAccumulated;
    private WorkspaceMovementState _workspaceMovement;
    private float _lastWorkspaceMoveTime = -999f;
    private int _frameIndex;
    private int _readbackFrameIndex;
    private bool _readbackPending;
    private float[] _readbackBuffer;
    private int _pointOffset;
    private int _lastPointCount;
    private float _lastAccumulationLogTime = -999f;

    public RenderTexture CacheTexture => _cacheTex;
    public bool HasAccumulated => _hasAccumulated;

    public void MarkCacheDirtyIfWorkspaceMoved(Transform workspaceRoot)
    {
        if (workspaceRoot == null)
            return;

        if (_workspaceMovement == null || _workspaceMovement.transform != workspaceRoot)
            _workspaceMovement = workspaceRoot.GetComponent<WorkspaceMovementState>();

        if (_workspaceMovement == null)
            return;

        if (_workspaceMovement.LastMoveTime > _lastWorkspaceMoveTime)
        {
            _lastWorkspaceMoveTime = _workspaceMovement.LastMoveTime;
            Clear();
            CacheCleared?.Invoke();
        }
    }

    public void Ensure(int resolution, Vector3 corner, Vector3 size)
    {
        if (resolution <= 0)
            return;

        bool needsRealloc =
            _cacheTex == null ||
            !_cacheTex.IsCreated() ||
            _lastResolution != resolution;

        if (needsRealloc)
        {
            Release();
            _cacheTex = CreateCacheTex(resolution);
            _blurTex = CreateCacheTex(resolution);
            _lastResolution = resolution;
            _lastCorner = corner;
            _lastSize = size;
            _hasAccumulated = false;
            _frameIndex = 0;
            Clear();
            return;
        }

        if (_lastCorner != corner || _lastSize != size)
        {
            _lastCorner = corner;
            _lastSize = size;
            _hasAccumulated = false;
            _frameIndex = 0;
            Clear();
        }
    }

    public void Clear()
    {
        EnsureKernel();
        if (!_ready || _cacheTex == null)
            return;

        _cs.SetInt("_CacheResolution", _lastResolution);
        _cs.SetTexture(_kClear, "_CacheTex", _cacheTex);
        int g = Mathf.CeilToInt(_lastResolution / 8f);
        _cs.Dispatch(_kClear, g, g, g);
        _hasAccumulated = false;
        _frameIndex = 0;
    }

    public void Decay(float decayFactor)
    {
        if (_cacheTex == null || decayFactor >= 0.999f)
            return;

        EnsureKernel();
        if (!_ready || _kDecay < 0)
            return;

        _cs.SetInt("_CacheResolution", _lastResolution);
        _cs.SetFloat("_DecayFactor", Mathf.Clamp01(decayFactor));
        _cs.SetTexture(_kDecay, "_CacheTex", _cacheTex);
        int g = Mathf.CeilToInt(_lastResolution / 8f);
        _cs.Dispatch(_kDecay, g, g, g);
    }

    public void CarveDepthRays(
        RenderTexture depthTex,
        Vector2Int depthSize,
        int eyeSlice,
        bool flipY,
        Matrix4x4 invDepthViewProj,
        Matrix4x4 trackingToWorld,
        int downsample,
        int maxSteps,
        Matrix4x4 worldToWorkspace,
        Vector3 workspaceCorner,
        Vector3 workspaceSize,
        Vector3 cameraWorld)
    {
        if (depthTex == null || _cacheTex == null)
            return;

        EnsureKernel();
        if (!_ready || _kCarveDepthRays < 0)
            return;

        _cs.SetTexture(_kCarveDepthRays, "_DepthTex", depthTex);
        _cs.SetTexture(_kCarveDepthRays, "_CacheTex", _cacheTex);
        _cs.SetInts("_DepthSize", depthSize.x, depthSize.y);
        _cs.SetInt("_EyeSlice", eyeSlice);
        _cs.SetInt("_FlipY", flipY ? 1 : 0);
        _cs.SetMatrix("_InvDepthViewProj", invDepthViewProj);
        _cs.SetMatrix("_TrackingToWorld", trackingToWorld);
        _cs.SetInt("_Downsample", Mathf.Max(1, downsample));
        _cs.SetInt("_CacheResolution", _lastResolution);
        _cs.SetInt("_CarveMaxSteps", Mathf.Max(1, maxSteps));
        _cs.SetMatrix("_WorldToWorkspace", worldToWorkspace);
        _cs.SetVector("_WorkspaceCorner", workspaceCorner);
        _cs.SetVector("_WorkspaceSize", workspaceSize);
        _cs.SetVector("_CameraWorld", cameraWorld);

        int gx = Mathf.CeilToInt(depthSize.x / 8f);
        int gy = Mathf.CeilToInt(depthSize.y / 8f);
        _cs.Dispatch(_kCarveDepthRays, gx, gy, 1);
    }

    public void CarveDepthRaysIfReady(
        int updateInterval,
        RenderTexture depthTex,
        Vector2Int depthSize,
        int eyeSlice,
        bool flipY,
        Matrix4x4 invDepthViewProj,
        Matrix4x4 trackingToWorld,
        int downsample,
        int maxSteps,
        Matrix4x4 worldToWorkspace,
        Vector3 workspaceCorner,
        Vector3 workspaceSize,
        Vector3 cameraWorld,
        ref int frameIndex)
    {
        if (updateInterval <= 0)
            return;

        frameIndex++;
        if (frameIndex % updateInterval != 0)
            return;

        CarveDepthRays(
            depthTex,
            depthSize,
            eyeSlice,
            flipY,
            invDepthViewProj,
            trackingToWorld,
            downsample,
            maxSteps,
            worldToWorkspace,
            workspaceCorner,
            workspaceSize,
            cameraWorld);
    }

    public RenderTexture BlurCache(int radius)
    {
        if (_cacheTex == null || radius <= 0)
            return _cacheTex;

        EnsureBlurKernel();
        if (_blurCs == null || _kBlur < 0 || _blurTex == null)
            return _cacheTex;

        _blurCs.SetTexture(_kBlur, "_Src", _cacheTex);
        _blurCs.SetTexture(_kBlur, "_Dst", _blurTex);
        _blurCs.SetInt("_CacheResolution", _lastResolution);
        _blurCs.SetInt("_BlurRadius", Mathf.Clamp(radius, 1, 3));

        int g = Mathf.CeilToInt(_lastResolution / 4f);
        _blurCs.Dispatch(_kBlur, g, g, g);
        return _blurTex;
    }

    public void AccumulatePoints(
        ComputeBuffer points,
        int pointCount,
        int downsample,
        int minVoxelSpacing,
        int pointStart,
        Matrix4x4 worldToWorkspace,
        Vector3 workspaceCorner,
        Vector3 workspaceSize)
    {
        if (points == null || _cacheTex == null || pointCount <= 0)
            return;

        EnsureKernel();
        if (!_ready || _kAccumulatePoints < 0)
            return;

        _cs.SetBuffer(_kAccumulatePoints, "_Points", points);
        _cs.SetTexture(_kAccumulatePoints, "_CacheTex", _cacheTex);
        _cs.SetInt("_PointCount", pointCount);
        _cs.SetInt("_Downsample", Mathf.Max(1, downsample));
        _cs.SetInt("_MinVoxelSpacing", Mathf.Max(0, minVoxelSpacing));
        _cs.SetInt("_PointStart", Mathf.Max(0, pointStart));
        _cs.SetInt("_CacheResolution", _lastResolution);
        _cs.SetMatrix("_WorldToWorkspace", worldToWorkspace);
        _cs.SetVector("_WorkspaceCorner", workspaceCorner);
        _cs.SetVector("_WorkspaceSize", workspaceSize);

        int gx = Mathf.CeilToInt(pointCount / 256f);
        _cs.Dispatch(_kAccumulatePoints, gx, 1, 1);
        _hasAccumulated = true;
    }

    public void CarveRays(
        ComputeBuffer points,
        int pointCount,
        int downsample,
        int pointStart,
        int maxSteps,
        Matrix4x4 worldToWorkspace,
        Vector3 workspaceCorner,
        Vector3 workspaceSize,
        Vector3 cameraWorld)
    {
        if (points == null || _cacheTex == null || pointCount <= 0)
            return;

        EnsureKernel();
        if (!_ready || _kCarveRays < 0)
            return;

        _cs.SetBuffer(_kCarveRays, "_Points", points);
        _cs.SetTexture(_kCarveRays, "_CacheTex", _cacheTex);
        _cs.SetInt("_PointCount", pointCount);
        _cs.SetInt("_Downsample", Mathf.Max(1, downsample));
        _cs.SetInt("_PointStart", Mathf.Max(0, pointStart));
        _cs.SetInt("_CacheResolution", _lastResolution);
        _cs.SetInt("_CarveMaxSteps", Mathf.Max(1, maxSteps));
        _cs.SetMatrix("_WorldToWorkspace", worldToWorkspace);
        _cs.SetVector("_WorkspaceCorner", workspaceCorner);
        _cs.SetVector("_WorkspaceSize", workspaceSize);
        _cs.SetVector("_CameraWorld", cameraWorld);

        int gx = Mathf.CeilToInt(pointCount / 256f);
        _cs.Dispatch(_kCarveRays, gx, 1, 1);
    }

    public void AccumulatePointsIfReady(
        int updateInterval,
        ComputeBuffer points,
        int pointCount,
        int downsample,
        int minVoxelSpacing,
        int pointStart,
        Matrix4x4 worldToWorkspace,
        Vector3 workspaceCorner,
        Vector3 workspaceSize)
    {
        if (updateInterval <= 0)
            return;

        _frameIndex++;
        if (_frameIndex % updateInterval != 0)
            return;

        AccumulatePoints(
            points,
            pointCount,
            downsample,
            minVoxelSpacing,
            pointStart,
            worldToWorkspace,
            workspaceCorner,
            workspaceSize);
    }

    public void AccumulatePointsChunked(
        ComputeBuffer points,
        int totalCount,
        int batchSize,
        int downsample,
        int minVoxelSpacing,
        Matrix4x4 worldToWorkspace,
        Vector3 workspaceCorner,
        Vector3 workspaceSize)
    {
        if (totalCount <= 0 || points == null)
            return;

        if (_lastPointCount != totalCount)
        {
            _pointOffset = 0;
            _lastPointCount = totalCount;
        }

        int size = Mathf.Clamp(batchSize, 1, totalCount);
        int start = _pointOffset;
        int count = Mathf.Min(size, totalCount - start);

        AccumulatePoints(
            points,
            count,
            downsample,
            minVoxelSpacing,
            start,
            worldToWorkspace,
            workspaceCorner,
            workspaceSize);

        float now = Time.realtimeSinceStartup;
        if (now - _lastAccumulationLogTime > 1f)
        {
            _lastAccumulationLogTime = now;
            DebugService.Log($"[SdfSculptGuideCache] Accumulate chunk start={start} count={count} total={totalCount} downsample={downsample} spacing={minVoxelSpacing}", null);
        }

        _pointOffset += count;
        if (_pointOffset >= totalCount)
            _pointOffset = 0;
    }

    public void RequestReadbackIfReady(
        int updateInterval,
        System.Action<float[], int, Vector3, Vector3> onComplete)
    {
        if (updateInterval <= 0 || _cacheTex == null || onComplete == null)
            return;

        if (_readbackPending)
            return;

        _readbackFrameIndex++;
        if (_readbackFrameIndex % updateInterval != 0)
            return;

        _readbackPending = true;
        int res = _lastResolution;
        Vector3 corner = _lastCorner;
        Vector3 size = _lastSize;

        AsyncGPUReadback.Request(_cacheTex, 0, request =>
        {
            _readbackPending = false;
            if (request.hasError)
            {
                DebugService.LogEvery(
                    "SdfSculptGuideCache.ReadbackError",
                    "[SdfSculptGuideCache] GPU readback failed.",
                    1f);
                return;
            }

            var data = request.GetData<float>();
            int count = data.Length;
            if (_readbackBuffer == null || _readbackBuffer.Length != count)
                _readbackBuffer = new float[count];
            data.CopyTo(_readbackBuffer);
            if (_readbackBuffer.Length < res * res * res)
            {
                DebugService.LogEvery(
                    "SdfSculptGuideCache.ReadbackSizeMismatch",
                    $"[SdfSculptGuideCache] Readback size mismatch. res={res} len={_readbackBuffer.Length}",
                    1f);
                return;
            }
            onComplete(_readbackBuffer, res, corner, size);
        });
    }

    public void Release()
    {
        if (_cacheTex != null)
        {
            _cacheTex.Release();
            _cacheTex = null;
        }
        if (_blurTex != null)
        {
            _blurTex.Release();
            _blurTex = null;
        }
        _hasAccumulated = false;
        _frameIndex = 0;
        _readbackFrameIndex = 0;
        _readbackPending = false;
        _pointOffset = 0;
        _lastPointCount = 0;
    }

    private void EnsureKernel()
    {
        if (_ready)
            return;

        _cs = Resources.Load<ComputeShader>("SDF/Compute/Visualization/SdfSculptGuideCache");
        if (_cs == null)
        {
            DebugService.Warn("[SdfSculptGuideCache] Missing compute shader: SDF/Compute/Visualization/SdfSculptGuideCache");
            return;
        }

        _kClear = _cs.FindKernel("CSClear");
        _kAccumulatePoints = _cs.FindKernel("CSAccumulatePoints");
        _kCarveRays = _cs.FindKernel("CSCarveRays");
        _kDecay = _cs.FindKernel("CSDecay");
        _kCarveDepthRays = _cs.FindKernel("CSCarveDepthRays");
        _ready = _kClear >= 0;
        if (!_ready)
            DebugService.Warn("[SdfSculptGuideCache] Missing kernel CSClear.");
        if (_kAccumulatePoints < 0)
            DebugService.Warn("[SdfSculptGuideCache] Missing kernel CSAccumulatePoints.");
        if (_kCarveRays < 0)
            DebugService.Warn("[SdfSculptGuideCache] Missing kernel CSCarveRays.");
        if (_kDecay < 0)
            DebugService.Warn("[SdfSculptGuideCache] Missing kernel CSDecay.");
        if (_kCarveDepthRays < 0)
            DebugService.Warn("[SdfSculptGuideCache] Missing kernel CSCarveDepthRays.");
    }

    private void EnsureBlurKernel()
    {
        if (_blurCs != null)
            return;
        _blurCs = Resources.Load<ComputeShader>("SDF/Compute/Visualization/SdfSculptGuideCacheBlur");
        if (_blurCs == null)
        {
            DebugService.Warn("[SdfSculptGuideCache] Missing compute shader: SDF/Compute/Visualization/SdfSculptGuideCacheBlur");
            return;
        }
        _kBlur = _blurCs.FindKernel("CSBlur");
    }

    private static RenderTexture CreateCacheTex(int res)
    {
        var desc = new RenderTextureDescriptor(res, res, RenderTextureFormat.RFloat, 0)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = res,
            enableRandomWrite = true
        };
        var rt = new RenderTexture(desc)
        {
            name = "SdfSculptGuideCache",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        rt.Create();
        return rt;
    }
}






