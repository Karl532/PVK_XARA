using UnityEngine;
using Assets.Scripts.Debug;
using UnityEngine.Rendering;

public sealed class SdfSculptGuidePointRenderer
{
    public event System.Action CacheCleared;

    private readonly int _maxPointRenderCount;
    private readonly SdfSculptGuideCache _cache = new SdfSculptGuideCache();
    private readonly SdfSculptGuideCachePointsRenderer _cachePointsRenderer = new SdfSculptGuideCachePointsRenderer();
    private readonly SdfSculptGuideCarver _carver = new SdfSculptGuideCarver();

    private ComputeBuffer _pointBuffer;
    private int _pointCount;
    private int _pointStride = 1;
    private int _pointRenderFrameIndex;
    private int _depthCarveFrameIndex;
    private float _lastPointRenderTime = -999f;
    private float _lastCacheUpdateTime = -999f;
    private float _lastAccumulationTime = -999f;
    private ComputeBuffer _indirectArgsBuffer;
    private readonly uint[] _indirectArgs = new uint[5] { 0, 1, 0, 0, 0 };

    private Vector3 _lastCacheCorner;
    private Vector3 _lastCacheSize;
    private int _lastCacheResolution;
    private Vector3 _lastCameraWorld;
    private bool _hasCamera;

    public SdfSculptGuidePointRenderer(int maxPointRenderCount)
    {
        _maxPointRenderCount = maxPointRenderCount;
        _cache.CacheCleared += OnCacheCleared;
    }

    public ComputeBuffer PointBuffer => _pointBuffer;
    public int PointCount => _pointCount;

    public void MarkCacheDirtyIfWorkspaceMoved(Transform workspaceRoot)
    {
        _cache.MarkCacheDirtyIfWorkspaceMoved(workspaceRoot);
    }

    public void Release()
    {
        _cache.CacheCleared -= OnCacheCleared;
        _cache.Release();
        _cachePointsRenderer.Release();
        if (_indirectArgsBuffer != null)
        {
            _indirectArgsBuffer.Release();
            _indirectArgsBuffer = null;
        }
    }

    public void UpdatePointCloud(ComputeBuffer points, int count, SculptGuideSettings settings)
    {
        _pointBuffer = points;
        _pointCount = Mathf.Max(0, count);
        _pointStride = ComputePointStride(_pointCount, settings);
        int renderCount = Mathf.Max(0, _pointCount / _pointStride);
        UpdateIndirectArgs(renderCount);
    }

    public bool HasPoints => _pointBuffer != null && _pointCount > 0;

    public bool HasCache(SculptGuideSettings settings) =>
        settings.EnableCache && _cache.CacheTexture != null;

    public void UpdateVisualizationData(
        SdfVisualizationData data,
        SculptGuideSettings settings,
        Matrix4x4 worldToWorkspace)
    {
        if (!settings.EnableCache)
            return;

        if (settings.CacheUpdateSeconds > 0f)
        {
            if (Time.unscaledTime - _lastCacheUpdateTime < settings.CacheUpdateSeconds)
                return;
            _lastCacheUpdateTime = Time.unscaledTime;
        }

        _cache.Ensure(settings.CacheResolution, data.WorkspaceCorner, data.WorkspaceSize);
        if (_pointBuffer != null && _pointCount > 0)
        {
            bool canAccumulate = true;
            if (settings.CacheAccumulationSeconds > 0f)
            {
                if (Time.unscaledTime - _lastAccumulationTime < settings.CacheAccumulationSeconds)
                    canAccumulate = false;
                else
                    _lastAccumulationTime = Time.unscaledTime;
            }

            if (canAccumulate)
            {
                _cache.AccumulatePoints(
                    _pointBuffer,
                    _pointCount,
                    settings.CacheDownsample,
                    settings.CacheMinVoxelSpacing,
                    0,
                    worldToWorkspace,
                    data.WorkspaceCorner,
                    data.WorkspaceSize);
            }
        }

        if (_lastCacheCorner != data.WorkspaceCorner || _lastCacheSize != data.WorkspaceSize || _lastCacheResolution != settings.CacheResolution)
        {
            _lastCacheCorner = data.WorkspaceCorner;
            _lastCacheSize = data.WorkspaceSize;
            _lastCacheResolution = settings.CacheResolution;
            _cachePointsRenderer.MarkDirty();
        }

        if (_cache.CacheTexture != null)
        {
            _cachePointsRenderer.BuildFromCache(
                _cache.CacheTexture,
                settings.CacheResolution,
                _lastCacheCorner,
                _lastCacheSize);
        }
    }

    public void Render(
        ScriptableRenderContext context,
        Camera camera,
        SculptGuideSettings settings,
        SdfVolumeData global,
        Matrix4x4 worldToWorkspace,
        Material pointMaterial,
        Material cacheMaterial,
        DepthFrameData depthFrame,
        bool hasDepthFrame)
    {
        _lastCameraWorld = camera.transform.position;
        _hasCamera = true;

        if (settings.CacheCarveEnabled && _hasCamera)
        {
            if (settings.CacheCarveUseDepthTexture && hasDepthFrame)
            {
                _cache.CarveDepthRaysIfReady(
                    settings.CacheCarveInterval,
                    depthFrame.DepthTexture,
                    depthFrame.DepthResolution,
                    depthFrame.EyeSlice,
                    depthFrame.FlipY,
                    depthFrame.InvDepthViewProj,
                    depthFrame.TrackingToWorld,
                    settings.CacheCarveDownsample,
                    settings.CacheCarveMaxSteps,
                    worldToWorkspace,
                    _lastCacheCorner,
                    _lastCacheSize,
                    _lastCameraWorld,
                    ref _depthCarveFrameIndex);
            }
            else if (_pointBuffer != null && _pointCount > 0)
            {
                _carver.CarveIfReady(
                    settings.CacheCarveInterval,
                    _cache,
                    _pointBuffer,
                    _pointCount,
                    settings.CacheCarveDownsample,
                    settings.CacheCarveMaxSteps,
                    0,
                    worldToWorkspace,
                    _lastCacheCorner,
                    _lastCacheSize,
                    _lastCameraWorld);
            }
        }

        bool renderPointsThisFrame = ShouldRenderPointsThisFrame(settings);
        if (settings.RenderPoints && renderPointsThisFrame && pointMaterial != null && _pointBuffer != null && _pointCount > 0)
        {
            float pointStart = Time.realtimeSinceStartup;
            ApplyGlobalProperties(pointMaterial, global);
            ApplyVisualProperties(pointMaterial, settings);
            pointMaterial.SetMatrix(SdfSculptGuideShaderIds.WorldToWorkspace, worldToWorkspace);
            pointMaterial.SetBuffer(SdfSculptGuideShaderIds.Points, _pointBuffer);
            pointMaterial.SetFloat(SdfSculptGuideShaderIds.PointSizePx, settings.PointSizePx);
            pointMaterial.SetInt(SdfSculptGuideShaderIds.PointCount, _pointCount);
            pointMaterial.SetInt(SdfSculptGuideShaderIds.PointStride, _pointStride);
            Bounds bounds = new Bounds(camera.transform.position, Vector3.one * 2000f);
            Graphics.DrawProceduralIndirect(
                pointMaterial,
                bounds,
                MeshTopology.Triangles,
                _indirectArgsBuffer,
                0,
                camera);
            float pointMs = (Time.realtimeSinceStartup - pointStart) * 1000f;
            if (pointMs > 2f && Time.realtimeSinceStartup - _lastPointRenderTime > 1f)
            {
                _lastPointRenderTime = Time.realtimeSinceStartup;
                DebugService.Log($"[SdfSculptGuidePointRenderer] Point render {pointMs:F2} ms (count={_pointCount} stride={_pointStride}).", null);
            }
        }

        if (settings.EnableCache)
        {
            if (_cache.CacheTexture != null && cacheMaterial != null)
            {
                _cachePointsRenderer.Render(
                    cacheMaterial,
                    camera,
                    settings.PointSizePx,
                    worldToWorkspace.inverse,
                    global,
                    _cache.CacheTexture,
                    settings.CacheResolution,
                    settings.Alpha);
            }
        }
    }

    public void Reset()
    {
        _cachePointsRenderer.MarkDirty();
        _carver.Reset();
        _depthCarveFrameIndex = 0;
    }

    private void OnCacheCleared()
    {
        _cachePointsRenderer.MarkDirty();
        _carver.Reset();
        CacheCleared?.Invoke();
        DebugService.Log("[SdfSculptGuidePointRenderer] Cleared cache due to workspace movement.", null);
    }

    private void UpdateIndirectArgs(int pointCount)
    {
        if (_indirectArgsBuffer == null)
            _indirectArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);

        _indirectArgs[0] = (uint)(6 * pointCount);
        _indirectArgs[1] = 1;
        _indirectArgs[2] = 0;
        _indirectArgs[3] = 0;
        _indirectArgs[4] = 0;
        _indirectArgsBuffer.SetData(_indirectArgs);
    }

    private int ComputePointStride(int pointCount, SculptGuideSettings settings)
    {
        int targetMax = settings.PointRenderMaxCount > 0 ? settings.PointRenderMaxCount : _maxPointRenderCount;
        if (pointCount <= 0 || targetMax <= 0)
            return 1;
        if (pointCount <= targetMax)
            return 1;
        return Mathf.CeilToInt(pointCount / (float)targetMax);
    }

    private bool ShouldRenderPointsThisFrame(SculptGuideSettings settings)
    {
        int interval = Mathf.Max(1, settings.PointRenderInterval);
        if (interval <= 1)
            return true;
        _pointRenderFrameIndex++;
        return (_pointRenderFrameIndex % interval) == 0;
    }

    private static void ApplyGlobalProperties(Material material, SdfVolumeData global)
    {
        material.SetTexture(SdfSculptGuideShaderIds.GlobalTsdf3D, global.Tsdf);
        material.SetVector(SdfSculptGuideShaderIds.GlobalCorner, global.Corner);
        material.SetVector(SdfSculptGuideShaderIds.GlobalSize, global.Size);
        material.SetFloat(SdfSculptGuideShaderIds.GlobalMu, global.Mu);
    }

    private static void ApplyVisualProperties(Material material, SculptGuideSettings settings)
    {
        material.SetFloat(SdfSculptGuideShaderIds.Alpha, settings.Alpha);
        material.SetColor(SdfSculptGuideShaderIds.InsideColor, new Color(0f, 0.4f, 1f, 1f));
        material.SetColor(SdfSculptGuideShaderIds.OutsideColor, new Color(1f, 0.2f, 0.2f, 1f));
        material.SetColor(SdfSculptGuideShaderIds.SurfaceColor, new Color(0f, 1f, 0f, 1f));
        material.SetFloat(SdfSculptGuideShaderIds.PointSizePx, settings.PointSizePx);
    }
}




