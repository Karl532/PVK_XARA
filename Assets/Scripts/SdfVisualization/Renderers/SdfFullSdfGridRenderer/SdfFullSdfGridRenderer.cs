using UnityEngine;
using Assets.Scripts.Debug;
using UnityEngine.Rendering;

/// <summary>
/// Renders a world-space grid of points sampled from the GLOBAL TSDF.
/// Intended for debug visualization (not fullscreen overlay).
/// </summary>
public class SdfFullSdfGridRenderer : MonoBehaviour, ISdfRenderer
{
    [Header("Rendering")]
    [SerializeField] private Shader gridPointShader;

    [Header("Visual Settings")]
    [Range(1, 12)]
    [SerializeField] private float pointSizePx = 3f;
    [Range(0f, 1f)]
    [SerializeField] private float alpha = 0.35f;
    [Range(4, 128)]
    [SerializeField] private int gridResolution = 12;
    [SerializeField] private float distanceScale = 1f;

    private Material _material;
    private ComputeBuffer _pointsBuffer;
    private ComputeBuffer _indirectArgsBuffer;
    private readonly uint[] _indirectArgs = new uint[5] { 0, 1, 0, 0, 0 };

    private int _pointCount;
    private bool _initialized;
    private bool _gridDirty = true;

    private SdfVolumeData _global;
    private Matrix4x4 _workspaceToWorld = Matrix4x4.identity;
    private Vector3 _lastCorner;
    private Vector3 _lastSize;
    private int _lastGridResolution;

    private static readonly int ID_PointsWS = Shader.PropertyToID("_PointsWS");
    private static readonly int ID_PointSizePx = Shader.PropertyToID("_PointSizePx");
    private static readonly int ID_GlobalTsdf3D = Shader.PropertyToID("_GlobalTsdf3D");
    private static readonly int ID_GlobalCorner = Shader.PropertyToID("_GlobalCorner");
    private static readonly int ID_GlobalSize = Shader.PropertyToID("_GlobalSize");
    private static readonly int ID_GlobalMu = Shader.PropertyToID("_GlobalMu");
    private static readonly int ID_Alpha = Shader.PropertyToID("_Alpha");
    private static readonly int ID_DistanceScale = Shader.PropertyToID("_DistanceScale");
    private static readonly int ID_WorkspaceToWorld = Shader.PropertyToID("_WorkspaceToWorld");
    private static readonly int ID_MaxDistance = Shader.PropertyToID("_MaxDistance");

    private void Awake()
    {
        EnsureInitialized();
        CalibrationOriginUtility.AttachToOrigin(transform, worldPositionStays: true);
    }

    private void OnEnable()
    {
        EnsureInitialized();
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    private void OnDestroy()
    {
        if (_material != null)
        {
            Destroy(_material);
            _material = null;
        }

        if (_pointsBuffer != null)
        {
            _pointsBuffer.Release();
            _pointsBuffer = null;
        }

        if (_indirectArgsBuffer != null)
        {
            _indirectArgsBuffer.Release();
            _indirectArgsBuffer = null;
        }
    }

    public void Configure(int resolution, float alpha01, float pointSizePxValue, float distanceScaleValue)
    {
        gridResolution = Mathf.Clamp(resolution, 4, 256);
        alpha = Mathf.Clamp01(alpha01);
        pointSizePx = Mathf.Clamp(pointSizePxValue, 1f, 12f);
        distanceScale = Mathf.Max(0.1f, distanceScaleValue);

        if (_lastGridResolution != gridResolution)
            _gridDirty = true;
    }

    public void UpdateGrid(SdfVolumeData global, Transform workspaceRoot)
    {
        _global = global;
        if (!_global.IsValid || workspaceRoot == null)
            return;

        _workspaceToWorld = workspaceRoot.localToWorldMatrix;

        if (_lastCorner != _global.Corner || _lastSize != _global.Size)
            _gridDirty = true;

        if (_gridDirty)
        {
            RebuildGridPoints();
            _gridDirty = false;
        }
    }

    public void UpdateRenderer(in SdfRendererContext context)
    {
        var settings = Settings.GetActive();
        var config = settings != null ? settings.sdfVisualizationConfig : null;
        if (settings == null || config == null)
        {
            enabled = false;
            return;
        }

        enabled = settings.sdfRenderFullSdfGrid;
        if (!enabled)
            return;

        var data = context.Data;
        float distanceScaleValue = Mathf.Max(0.1f, data.WorkspaceSize.magnitude);
        var volume = data.Global;
        if (!volume.IsValid || data.WorkspaceRoot == null)
        {
            enabled = false;
            return;
        }

        Configure(
            config.overlayFullSdfGridResolution,
            config.overlayFullSdfAlpha,
            config.overlayGridPointSizePx,
            distanceScaleValue);
        UpdateGrid(volume, data.WorkspaceRoot);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        if (!gridPointShader)
            gridPointShader = Shader.Find("Hidden/SdfGridPoints");
        if (!gridPointShader)
            gridPointShader = Resources.Load<Shader>("SDF/Shaders/SdfGridPoints");

        if (!gridPointShader)
        {
            DebugService.Error("[SdfFullSdfGridRenderer] Missing SdfGridPoints shader.");
            enabled = false;
            return;
        }

        _material = new Material(gridPointShader)
        {
            name = "SdfGridPointsMaterial"
        };

        _indirectArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        _initialized = true;
    }

    private void RebuildGridPoints()
    {
        _lastCorner = _global.Corner;
        _lastSize = _global.Size;
        _lastGridResolution = gridResolution;

        int res = gridResolution;
        int count = res * res * res;
        _pointCount = count;

        if (_pointsBuffer == null || _pointsBuffer.count != count)
        {
            if (_pointsBuffer != null)
                _pointsBuffer.Release();
            _pointsBuffer = new ComputeBuffer(count, sizeof(float) * 4);
        }

        Vector4[] points = new Vector4[count];
        int i = 0;
        for (int z = 0; z < res; z++)
        {
            float fz = (z + 0.5f) / res;
            for (int y = 0; y < res; y++)
            {
                float fy = (y + 0.5f) / res;
                for (int x = 0; x < res; x++)
                {
                    float fx = (x + 0.5f) / res;
                    Vector3 uvw = new Vector3(fx, fy, fz);
                    Vector3 posWS = _global.Corner + Vector3.Scale(_global.Size, uvw);
                    points[i++] = new Vector4(posWS.x, posWS.y, posWS.z, 1f);
                }
            }
        }

        _pointsBuffer.SetData(points);
        UpdateIndirectArgs(_pointCount);
        DebugService.Log($"[SdfFullSdfGridRenderer] Rebuilt grid points: res={res} count={count}", this);
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!_initialized || _pointsBuffer == null || _pointCount <= 0 || !_global.IsValid)
            return;

        if (camera == null || (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.VR))
            return;

        _material.SetBuffer(ID_PointsWS, _pointsBuffer);
        _material.SetFloat(ID_PointSizePx, pointSizePx);
        _material.SetTexture(ID_GlobalTsdf3D, _global.Tsdf);
        _material.SetVector(ID_GlobalCorner, _global.Corner);
        _material.SetVector(ID_GlobalSize, _global.Size);
        _material.SetFloat(ID_GlobalMu, _global.Mu);
        _material.SetFloat(ID_Alpha, alpha);
        _material.SetFloat(ID_DistanceScale, distanceScale);
        _material.SetMatrix(ID_WorkspaceToWorld, _workspaceToWorld);
        _material.SetFloat(ID_MaxDistance, 0f);

        Bounds bounds = new Bounds(camera.transform.position, Vector3.one * 2000f);
        Graphics.DrawProceduralIndirect(
            _material,
            bounds,
            MeshTopology.Triangles,
            _indirectArgsBuffer,
            0,
            camera);
    }

    private void UpdateIndirectArgs(int pointCount)
    {
        if (_indirectArgsBuffer == null)
            return;

        _indirectArgs[0] = (uint)(6 * pointCount);
        _indirectArgs[1] = 1;
        _indirectArgs[2] = 0;
        _indirectArgs[3] = 0;
        _indirectArgs[4] = 0;
        _indirectArgsBuffer.SetData(_indirectArgs);
    }
}






