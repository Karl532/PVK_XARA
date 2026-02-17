using UnityEngine;

    /// <summary>
    /// Runtime bounds renderer (Quest-friendly).
    /// Draws 3 cube overlays (workspace/global/local) via the render pipeline.
    /// All corners/sizes are in WORKSPACE space; workspaceRoot defines workspace->world.
    /// </summary>
    public class SdfBoundsRenderer : MonoBehaviour
    {
        [Header("Workspace frame")]
        [SerializeField] private Transform workspaceRoot;

        [Header("Overlay material (Unlit)")]
        [SerializeField] private Material lineMaterial;

        [Header("Enable")]
    [SerializeField] private bool drawWorkspace = true;
    [SerializeField] private bool drawGlobal = true;
    [SerializeField] private bool drawLocal = true;

        private Mesh _cubeMesh;
        private Mesh _lineMesh;
        private bool _hooked;
        private Vector3 _workspaceCornerWS;
        private Vector3 _workspaceSizeWS;
        private Vector3 _globalCornerWS;
        private Vector3 _globalSizeWS;
        private Vector3 _localCornerWS;
        private Vector3 _localSizeWS;
        private float _lastDebugLogTime = -999f;

        private void Awake()
        {
            EnsureCubeMesh();
            EnsureLineMesh();
            HookRenderPipeline();
            SdfDebug.LogVerbose("[SdfBoundsRenderer] Overlay renderer initialized.", this);
            SdfDebug.Log("[SDF_RENDER] SdfBoundsRenderer.Awake", this);
        }

        private void OnEnable()
        {
            HookRenderPipeline();
        }

        private void OnDisable()
        {
            UnhookRenderPipeline();
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

        /// <summary>
        /// Set all bounds (workspace-space min corner + size).
        /// Provide any you don't want to draw with drawX=false.
        /// </summary>
        public void Render(
            Transform workspaceRootTransform,
            Vector3 workspaceCornerWS, Vector3 workspaceSizeWS,
            Vector3 globalCornerWS, Vector3 globalSizeWS,
            Vector3 localCornerWS, Vector3 localSizeWS)
        {
            workspaceRoot = workspaceRootTransform;
            _workspaceCornerWS = workspaceCornerWS;
            _workspaceSizeWS = workspaceSizeWS;
            _globalCornerWS = globalCornerWS;
            _globalSizeWS = globalSizeWS;
            _localCornerWS = localCornerWS;
            _localSizeWS = localSizeWS;

            if (lineMaterial == null)
            {
                SdfDebug.WarnEvery(
                    "SdfBoundsRenderer.MissingRefs",
                    $"[SdfBoundsRenderer] Missing refs. workspaceRoot={(workspaceRoot ? workspaceRoot.name : "null")} lineMaterial={(lineMaterial ? lineMaterial.name : "null")}",
                    2f,
                    this);
                SdfDebug.Log($"[SDF_RENDER] Missing refs workspaceRoot={(workspaceRoot ? workspaceRoot.name : "null")} lineMaterial={(lineMaterial ? lineMaterial.name : "null")}", this);
                return;
            }

            SdfDebug.Log($"[SDF_RENDER] Render bounds ws={drawWorkspace} g={drawGlobal} l={drawLocal}", this);

            LogBasicDebug(workspaceCornerWS, workspaceSizeWS);
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

            SdfDebug.Log(
                $"[SDF_RENDER] Bounds debug (workspace space only): corner={cornerWS} size={sizeWS} " +
                $"c0={c[0]} c1={c[1]} c2={c[2]} c3={c[3]} c4={c[4]} c5={c[5]} c6={c[6]} c7={c[7]}",
                this);
        }


        private void OnBeginCameraRendering(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
        {
            if (lineMaterial == null || _cubeMesh == null)
                return;
            if (camera == null || (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.VR))
                return;
            if (workspaceRoot == null)
                return;

            if (drawWorkspace)
            {
                Matrix4x4 wsMatrix = workspaceRoot.localToWorldMatrix;
                DrawWireCube(camera, wsMatrix);
            }

            if (drawGlobal)
            {
                Matrix4x4 gMatrix = BuildWorkspaceSpaceMatrix(_globalCornerWS, _globalSizeWS);
                DrawWireCube(camera, gMatrix);
            }

            if (drawLocal)
            {
                Matrix4x4 lMatrix = BuildWorkspaceSpaceMatrix(_localCornerWS, _localSizeWS);
                DrawWireCube(camera, lMatrix);
            }
        }

        private Matrix4x4 BuildWorkspaceSpaceMatrix(Vector3 cornerWS, Vector3 sizeWS)
        {
            Vector3 centerWS = cornerWS + sizeWS * 0.5f;
            Matrix4x4 local = Matrix4x4.TRS(centerWS, Quaternion.identity, sizeWS);
            return workspaceRoot.localToWorldMatrix * local;
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

    public void Configure(Material material, float width, bool showWorkspace, bool showGlobalBounds, bool showLocalBounds)
    {
        lineMaterial = material;
        drawWorkspace = showWorkspace;
        drawGlobal = showGlobalBounds;
        drawLocal = showLocalBounds;
    }
    

    }
