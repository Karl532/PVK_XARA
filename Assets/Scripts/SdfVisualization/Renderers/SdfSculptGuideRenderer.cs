using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Renders a point-based sculpting guide overlay using depth points sampled against the GLOBAL TSDF.
/// Points are provided in WORKSPACE space.
/// </summary>
public class SdfSculptGuideRenderer : MonoBehaviour
{
    [Header("Rendering")]
    [SerializeField] private Shader guidePointShader;

    private Material _material;
    private ComputeBuffer _pointsBuffer;
    private ComputeBuffer _indirectArgsBuffer;
    private readonly uint[] _indirectArgs = new uint[5] { 0, 1, 0, 0, 0 };

    private int _pointCount;
    private bool _initialized;

    private SdfVolumeData _global;
    private Matrix4x4 _workspaceToWorld = Matrix4x4.identity;

    private float _pointSizePx = 3f;
    private float _alpha = 0.35f;
    private float _maxDistanceMeters = 0f;

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

        if (_indirectArgsBuffer != null)
        {
            _indirectArgsBuffer.Release();
            _indirectArgsBuffer = null;
        }
    }

    public void Configure(float pointSizePx, float alpha01, float maxDistanceMeters)
    {
        _pointSizePx = Mathf.Clamp(pointSizePx, 1f, 12f);
        _alpha = Mathf.Clamp01(alpha01);
        _maxDistanceMeters = Mathf.Max(0f, maxDistanceMeters);
    }

    public void UpdatePoints(ComputeBuffer pointsWS, int pointCount, SdfVolumeData global, Transform workspaceRoot)
    {
        _pointsBuffer = pointsWS;
        _pointCount = pointCount;
        _global = global;
        _workspaceToWorld = workspaceRoot != null ? workspaceRoot.localToWorldMatrix : Matrix4x4.identity;
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        if (!guidePointShader)
            guidePointShader = Shader.Find("Hidden/SdfGridPoints");
        if (!guidePointShader)
            guidePointShader = Resources.Load<Shader>("SDF/Shaders/SdfGridPoints");

        if (!guidePointShader)
        {
            Debug.LogError("[SdfSculptGuideRenderer] Missing SdfGridPoints shader.");
            enabled = false;
            return;
        }

        _material = new Material(guidePointShader)
        {
            name = "SdfSculptGuideMaterial"
        };

        _indirectArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        _initialized = true;
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!_initialized || _pointsBuffer == null || _pointCount <= 0 || !_global.IsValid)
            return;

        if (camera == null || (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.VR))
            return;

        _material.SetBuffer(ID_PointsWS, _pointsBuffer);
        _material.SetFloat(ID_PointSizePx, _pointSizePx);
        _material.SetTexture(ID_GlobalTsdf3D, _global.Tsdf);
        _material.SetVector(ID_GlobalCorner, _global.Corner);
        _material.SetVector(ID_GlobalSize, _global.Size);
        _material.SetFloat(ID_GlobalMu, _global.Mu);
        _material.SetFloat(ID_Alpha, _alpha);
        _material.SetFloat(ID_DistanceScale, 1f);
        _material.SetMatrix(ID_WorkspaceToWorld, _workspaceToWorld);
        _material.SetFloat(ID_MaxDistance, _maxDistanceMeters);

        UpdateIndirectArgs(_pointCount);

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
