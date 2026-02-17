using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Renders a continuous depth surface mesh and colors it by model TSDF distance.
/// Mesh is built in screen space using a grid.
/// </summary>
public class SdfSculptGuideRenderer : MonoBehaviour
{
    [Header("Rendering")]
    [SerializeField] private Shader surfaceMeshShader;
    [SerializeField] private int meshStep = 4;

    private Material _material;
    private bool _initialized;

    private RenderTexture _depthTex;
    private Vector2Int _depthSize;
    private Matrix4x4 _invDepthViewProj = Matrix4x4.identity;
    private Matrix4x4 _trackingToWorld = Matrix4x4.identity;
    private Matrix4x4 _worldToWorkspace = Matrix4x4.identity;
    private int _eyeSlice;
    private bool _flipY;

    private SdfVolumeData _global;
    private float _alpha = 0.6f;

    private Mesh _mesh;
    private int _meshW;
    private int _meshH;

    private static readonly int ID_DepthTex = Shader.PropertyToID("_DepthTex");
    private static readonly int ID_DepthSize = Shader.PropertyToID("_DepthSize");
    private static readonly int ID_EyeSlice = Shader.PropertyToID("_EyeSlice");
    private static readonly int ID_FlipY = Shader.PropertyToID("_FlipY");
    private static readonly int ID_InvDepthViewProj = Shader.PropertyToID("_InvDepthViewProj");
    private static readonly int ID_TrackingToWorld = Shader.PropertyToID("_TrackingToWorld");
    private static readonly int ID_WorldToWorkspace = Shader.PropertyToID("_WorldToWorkspace");

    private static readonly int ID_GlobalTsdf3D = Shader.PropertyToID("_GlobalTsdf3D");
    private static readonly int ID_GlobalCorner = Shader.PropertyToID("_GlobalCorner");
    private static readonly int ID_GlobalSize = Shader.PropertyToID("_GlobalSize");
    private static readonly int ID_GlobalMu = Shader.PropertyToID("_GlobalMu");

    private static readonly int ID_Alpha = Shader.PropertyToID("_Alpha");
    private static readonly int ID_InsideColor = Shader.PropertyToID("_InsideColor");
    private static readonly int ID_OutsideColor = Shader.PropertyToID("_OutsideColor");
    private static readonly int ID_SurfaceColor = Shader.PropertyToID("_SurfaceColor");

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
    }

    public void UpdateDepthFrame(DepthFrameData depthFrame, SdfVisualizationData data, SdfVisualizationConfig config)
    {
        _depthTex = depthFrame.DepthTexture;
        _depthSize = depthFrame.Resolution;
        _invDepthViewProj = depthFrame.InvDepthViewProj;
        _trackingToWorld = depthFrame.TrackingToWorld;
        _eyeSlice = depthFrame.EyeSlice;
        _flipY = depthFrame.FlipY;

        _global = data.Global;
        _worldToWorkspace = data.WorkspaceRoot != null ? data.WorkspaceRoot.worldToLocalMatrix : Matrix4x4.identity;

        if (config != null)
            _alpha = Mathf.Clamp01(config.sculptGuideAlpha);

        EnsureMesh(_depthSize, meshStep);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        if (!surfaceMeshShader)
            surfaceMeshShader = Shader.Find("Hidden/SdfSculptGuideSurfaceMesh");
        if (!surfaceMeshShader)
            surfaceMeshShader = Resources.Load<Shader>("SDF/Shaders/SdfSculptGuideSurfaceMesh");

        if (!surfaceMeshShader)
        {
            Debug.LogError("[SdfSculptGuideRenderer] Missing SdfSculptGuideSurfaceMesh shader.");
            enabled = false;
            return;
        }

        _material = new Material(surfaceMeshShader)
        {
            name = "SdfSculptGuideSurfaceMaterial"
        };

        _initialized = true;
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!_initialized || _depthTex == null || !_global.IsValid || _mesh == null)
        {
            SdfDebug.LogEvery(
                "SdfSculptGuideRenderer.MissingData",
                "[SdfSculptGuideRenderer] Missing data for rendering.",
                1f,
                this);
            return;
        }

        if (camera == null || (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.VR))
            return;

        _material.SetTexture(ID_DepthTex, _depthTex);
        _material.SetVector(ID_DepthSize, new Vector4(_depthSize.x, _depthSize.y, 0, 0));
        _material.SetInt(ID_EyeSlice, _eyeSlice);
        _material.SetInt(ID_FlipY, _flipY ? 1 : 0);
        _material.SetMatrix(ID_InvDepthViewProj, _invDepthViewProj);
        _material.SetMatrix(ID_TrackingToWorld, _trackingToWorld);
        _material.SetMatrix(ID_WorldToWorkspace, _worldToWorkspace);

        _material.SetTexture(ID_GlobalTsdf3D, _global.Tsdf);
        _material.SetVector(ID_GlobalCorner, _global.Corner);
        _material.SetVector(ID_GlobalSize, _global.Size);
        _material.SetFloat(ID_GlobalMu, _global.Mu);

        _material.SetFloat(ID_Alpha, _alpha);
        _material.SetColor(ID_InsideColor, new Color(0f, 0.4f, 1f, 1f));
        _material.SetColor(ID_OutsideColor, new Color(1f, 0.2f, 0.2f, 1f));
        _material.SetColor(ID_SurfaceColor, new Color(0f, 1f, 0f, 1f));

        Graphics.DrawMesh(
            _mesh,
            Matrix4x4.identity,
            _material,
            0,
            camera,
            0,
            null,
            ShadowCastingMode.Off,
            false);
    }

    private void EnsureMesh(Vector2Int depthSize, int step)
    {
        int w = Mathf.Max(2, depthSize.x / Mathf.Max(1, step));
        int h = Mathf.Max(2, depthSize.y / Mathf.Max(1, step));
        if (_mesh != null && _meshW == w && _meshH == h)
            return;

        _meshW = w;
        _meshH = h;

        int vertCount = (w + 1) * (h + 1);
        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];

        int idx = 0;
        for (int y = 0; y <= h; y++)
        {
            float v = y / (float)h;
            float posY = v * 2f - 1f;
            for (int x = 0; x <= w; x++)
            {
                float u = x / (float)w;
                float posX = u * 2f - 1f;
                verts[idx] = new Vector3(posX, posY, 0f);
                uvs[idx] = new Vector2(u, v);
                idx++;
            }
        }

        int quadCount = w * h;
        int[] tris = new int[quadCount * 6];
        int ti = 0;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i0 = y * (w + 1) + x;
                int i1 = i0 + 1;
                int i2 = i0 + (w + 1);
                int i3 = i2 + 1;
                tris[ti++] = i0;
                tris[ti++] = i2;
                tris[ti++] = i1;
                tris[ti++] = i1;
                tris[ti++] = i2;
                tris[ti++] = i3;
            }
        }

        if (_mesh == null)
            _mesh = new Mesh { name = "SdfSculptGuideSurfaceMesh" };
        _mesh.Clear();
        _mesh.vertices = verts;
        _mesh.uv = uvs;
        _mesh.triangles = tris;
        _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
    }
}
