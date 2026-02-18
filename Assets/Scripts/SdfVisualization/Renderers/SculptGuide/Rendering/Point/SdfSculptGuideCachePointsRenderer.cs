using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Extracts occupied cache voxels into a point buffer and renders them.
/// </summary>
public sealed class SdfSculptGuideCachePointsRenderer
{
    private ComputeShader _cs;
    private int _kExtract = -1;
    private int _kBuildArgs = -1;
    private ComputeBuffer _pointsBuffer;
    private ComputeBuffer _argsBuffer;
    private readonly uint[] _args = new uint[5] { 0, 1, 0, 0, 0 };
    private int _capacity;
    private bool _dirty = true;
    private float _lastCountLogTime = -999f;

    public void MarkDirty()
    {
        _dirty = true;
    }

    public void BuildFromCache(RenderTexture cacheTex, int resolution, Vector3 corner, Vector3 size)
    {
        if (cacheTex == null || resolution <= 0)
            return;

        EnsureKernel();
        EnsureBuffers(resolution);

        _pointsBuffer.SetCounterValue(0);
        _args[0] = 0;
        _args[1] = 1;
        _args[2] = 0;
        _args[3] = 0;
        _args[4] = 0;
        _argsBuffer.SetData(_args);

        _cs.SetTexture(_kExtract, "_CacheTex", cacheTex);
        _cs.SetBuffer(_kExtract, "_OutPoints", _pointsBuffer);
        _cs.SetInt("_CacheResolution", resolution);
        _cs.SetVector("_WorkspaceCorner", corner);
        _cs.SetVector("_WorkspaceSize", size);

        int groups = Mathf.CeilToInt(resolution / 4f);
        _cs.Dispatch(_kExtract, groups, groups, groups);

        ComputeBuffer.CopyCount(_pointsBuffer, _argsBuffer, 0);
        _cs.SetBuffer(_kBuildArgs, "_Args", _argsBuffer);
        _cs.Dispatch(_kBuildArgs, 1, 1, 1);

        if (Time.realtimeSinceStartup - _lastCountLogTime > 1f)
        {
            _lastCountLogTime = Time.realtimeSinceStartup;
            AsyncGPUReadback.Request(_argsBuffer, request =>
            {
                if (request.hasError)
                    return;
                var args = request.GetData<uint>();
                if (args.Length > 0)
                {
                    uint vertCount = args[0];
                    uint pointCount = vertCount / 6;
                    SdfDebug.Log($"[SdfSculptGuideCachePointsRenderer] Cache points={pointCount}", null);
                }
            });
        }

        _dirty = false;
    }

    public void Render(
        Material material,
        Camera camera,
        float pointSizePx,
        Matrix4x4 workspaceToWorld,
        SdfVolumeData global,
        RenderTexture cacheTex,
        int cacheResolution,
        float alpha)
    {
        if (material == null || cacheTex == null || !global.IsValid)
            return;
        if (camera == null || (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.VR))
            return;
        if (_pointsBuffer == null || _argsBuffer == null)
            return;

        material.SetBuffer("_PointsWS", _pointsBuffer);
        material.SetFloat("_PointSizePx", pointSizePx);
        material.SetMatrix("_WorkspaceToWorld", workspaceToWorld);
        material.SetTexture("_GlobalTsdf3D", global.Tsdf);
        material.SetVector("_GlobalCorner", global.Corner);
        material.SetVector("_GlobalSize", global.Size);
        material.SetFloat("_GlobalMu", global.Mu);
        material.SetFloat("_Alpha", alpha);
        material.SetTexture("_CacheTex", cacheTex);
        material.SetInt("_CacheResolution", cacheResolution);

        Bounds bounds = new Bounds(camera.transform.position, Vector3.one * 2000f);
        Graphics.DrawProceduralIndirect(
            material,
            bounds,
            MeshTopology.Triangles,
            _argsBuffer,
            0,
            camera);
    }

    public void Release()
    {
        _pointsBuffer?.Release();
        _argsBuffer?.Release();
        _pointsBuffer = null;
        _argsBuffer = null;
        _capacity = 0;
        _cs = null;
        _kExtract = -1;
        _kBuildArgs = -1;
        _dirty = true;
    }

    private void EnsureKernel()
    {
        if (_cs != null)
            return;
        _cs = Resources.Load<ComputeShader>("SDF/Compute/Visualization/SdfSculptGuideCacheExtractPoints");
        if (_cs == null)
        {
            Debug.LogWarning("[SdfSculptGuideCachePointsRenderer] Missing compute shader: SDF/Compute/Visualization/SdfSculptGuideCacheExtractPoints");
            return;
        }
        _kExtract = _cs.FindKernel("CSExtract");
        _kBuildArgs = _cs.FindKernel("CSBuildArgs");
    }

    private void EnsureBuffers(int resolution)
    {
        int capacity = resolution * resolution * resolution;
        if (_pointsBuffer == null || _capacity != capacity)
        {
            _pointsBuffer?.Release();
            _pointsBuffer = new ComputeBuffer(capacity, sizeof(float) * 4, ComputeBufferType.Append);
            _capacity = capacity;
        }

        if (_argsBuffer == null)
            _argsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
    }
}
