using UnityEngine;
using Assets.Scripts.Debug;

/// <summary>
/// Runtime bounds renderer (Quest-friendly).
/// Draws the workspace bounds via the render pipeline.
/// All corners/sizes are in WORKSPACE space; workspaceRoot defines workspace->world.
/// </summary>
public class SdfBoundsRenderer : MonoBehaviour, ISdfRenderer
{
    [Header("Workspace frame")]
    [SerializeField] private Transform workspaceRoot;

    [Header("Overlay material (Unlit)")]
    [SerializeField] private Material lineMaterial;

    [Header("Enable")]
    [SerializeField] private bool drawWorkspace = true;

    private Mesh _cubeMesh;
    private Mesh _lineMesh;
    private bool _hooked;
    private Vector3 _workspaceCornerWS;
    private Vector3 _workspaceSizeWS;
    private float _lastDebugLogTime = -999f;

    Matrix4x4 _unscaledWorkspaceToWorldMatrix;

    private void Awake()
    {
        EnsureCubeMesh();
        EnsureLineMesh();
        HookRenderPipeline();
        DebugService.LogVerbose("[SdfBoundsRenderer] Overlay renderer initialized.", this);
        DebugService.Log("[SDF_RENDER] SdfBoundsRenderer.Awake", this);
    }

    private void OnEnable()
    {
        HookRenderPipeline();
    }

    private void OnDisable()
    {
        UnhookRenderPipeline();
    }

    public void UpdateBounds(SdfVisualizationConfig config, Settings settings, SdfVisualizationData data)
    {
        if (config == null || settings == null)
            return;

        drawWorkspace = settings.sdfRenderWorkspaceBounds;
        if (!drawWorkspace)
            return;

        if (data.WorkspaceRoot == null)
            return;

        workspaceRoot = data.WorkspaceRoot;
        _workspaceCornerWS = data.WorkspaceCorner;
        _workspaceSizeWS = data.WorkspaceSize;
        _unscaledWorkspaceToWorldMatrix = data.UnscaledWorkspaceToWorldMatrix;

        EnsureMaterial(config);

        LogBasicDebug(_workspaceCornerWS, _workspaceSizeWS);
    }

    public void UpdateRenderer(in SdfRendererContext context)
    {
        var settings = Settings.GetActive();
        var config = settings != null ? settings.sdfVisualizationConfig : null;
        UpdateBounds(config, settings, context.Data);
    }

    private void EnsureMaterial(SdfVisualizationConfig config)
    {
        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                return;

            lineMaterial = new Material(shader) { name = "SDF_BoundsLineMat" };
            lineMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            if (lineMaterial.HasProperty("_Surface"))
                lineMaterial.SetFloat("_Surface", 0f); // Opaque
            if (lineMaterial.HasProperty("_ZWrite"))
                lineMaterial.SetInt("_ZWrite", 1);
        }

        lineMaterial.color = config.boundsColor;
    }

    private void EnsureCubeMesh()
    {
        if (_cubeMesh != null)
            return;

        var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var filter = temp.GetComponent<MeshFilter>();
        _cubeMesh = filter != null ? filter.sharedMesh : null;
        Destroy(temp);
    }

    private void EnsureLineMesh()
    {
        if (_lineMesh != null)
            return;

        var mesh = new Mesh();
        mesh.name = "SdfBoundsWireCube";

        Vector3[] v =
        {
            new Vector3(-0.5f, -0.5f, -0.5f), // 0
            new Vector3( 0.5f, -0.5f, -0.5f), // 1
            new Vector3( 0.5f, -0.5f,  0.5f), // 2
            new Vector3(-0.5f, -0.5f,  0.5f), // 3
            new Vector3(-0.5f,  0.5f, -0.5f), // 4
            new Vector3( 0.5f,  0.5f, -0.5f), // 5
            new Vector3( 0.5f,  0.5f,  0.5f), // 6
            new Vector3(-0.5f,  0.5f,  0.5f)  // 7
        };

        int[] idx =
        {
            0,1, 1,2, 2,3, 3,0,
            4,5, 5,6, 6,7, 7,4,
            0,4, 1,5, 2,6, 3,7
        };

        mesh.vertices = v;
        mesh.SetIndices(idx, MeshTopology.Lines, 0);
        mesh.RecalculateBounds();
        _lineMesh = mesh;
    }

    private void HookRenderPipeline()
    {
        if (_hooked)
            return;
        UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        _hooked = true;
    }

    private void UnhookRenderPipeline()
    {
        if (!_hooked)
            return;
        UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        _hooked = false;
    }

    private void LogBasicDebug(Vector3 cornerWS, Vector3 sizeWS)
    {
        float now = Time.unscaledTime;
        if (now - _lastDebugLogTime < 1f)
            return;
        _lastDebugLogTime = now;

        Vector3 min = cornerWS;
        Vector3 max = cornerWS + sizeWS;
        Vector3[] c = new Vector3[8];
        c[0] = new Vector3(min.x, min.y, min.z);
        c[1] = new Vector3(max.x, min.y, min.z);
        c[2] = new Vector3(max.x, min.y, max.z);
        c[3] = new Vector3(min.x, min.y, max.z);
        c[4] = new Vector3(min.x, max.y, min.z);
        c[5] = new Vector3(max.x, max.y, min.z);
        c[6] = new Vector3(max.x, max.y, max.z);
        c[7] = new Vector3(min.x, max.y, max.z);

        DebugService.Log(
            $"[SDF_RENDER] Bounds debug (workspace space only): corner={cornerWS} size={sizeWS} " +
            $"c0={c[0]} c1={c[1]} c2={c[2]} c3={c[3]} c4={c[4]} c5={c[5]} c6={c[6]} c7={c[7]}",
            this);
    }

    private void OnBeginCameraRendering(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
    {
        if (!drawWorkspace || lineMaterial == null || _cubeMesh == null)
            return;
        if (camera == null || (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.VR))
            return;
        if (workspaceRoot == null)
            return;

        Matrix4x4 wsMatrix = BuildWorkspaceSpaceMatrix(_workspaceCornerWS, _workspaceSizeWS);
        DrawWireCube(camera, wsMatrix);
    }

    private Matrix4x4 BuildWorkspaceSpaceMatrix(Vector3 cornerWS, Vector3 sizeWS)
    {
        Vector3 centerWS = cornerWS + sizeWS * 0.5f;
        Matrix4x4 local = Matrix4x4.TRS(centerWS, Quaternion.identity, sizeWS);
        /*Matrix4x4 rootMatrix = Matrix4x4.TRS( //FIX to make the scaling of the workspace bounds same as the workspace game object
            workspaceRoot.position,
            workspaceRoot.rotation,
            Vector3.one
        );*/

        return _unscaledWorkspaceToWorldMatrix * local;
    }

    private void DrawWireCube(Camera camera, Matrix4x4 matrix)
    {
        Graphics.DrawMesh(
            _lineMesh != null ? _lineMesh : _cubeMesh,
            matrix,
            lineMaterial,
            0,
            camera,
            0,
            null,
            UnityEngine.Rendering.ShadowCastingMode.Off,
            false);
    }
}




