using System;
using UnityEngine;

    /// <summary>
    /// Orchestrates:
    /// - converting world-space points -> workspace space
    /// - converting model mesh -> triangle space in workspace
    /// - building local/global TSDF volumes via Core (black box)
    /// - optional visualization (slice + overlay)
    ///
    /// ASSUMPTIONS (current project):
    /// 1) Point cloud buffer positions are in Unity WORLD space (OXDepth uses _TrackingToWorld).
    /// 2) Workspace space is PlacementBlock.localSpace (workspaceRoot).
    /// 3) PlacementBlock pivot is centered (Unity cube), so workspaceCornerWS = -size/2.
    /// 4) Model instance is placed in the same anchored world (ideally parented under CalibrationOrigin).
    /// </summary>
    public class SdfSystem : MonoBehaviour
    {
        [Header("Workspace (PlacementBlock)")]
        [Tooltip("Transform whose LOCAL space defines workspace space (usually your PlacementBlock transform).")]
        [SerializeField] private Transform workspaceRoot;

        [Tooltip("Workspace size in meters (Settings.stoneBlockDimensions).")]
        [SerializeField] private Vector3 workspaceSizeWS = Vector3.one;

        [Header("Model")]
        [Tooltip("Instantiated target model root GameObject (from settings.targetSculpturePrefab).")]
        [SerializeField] private GameObject modelInstance;

        [Header("Converters")]
        [Tooltip("Compute shader: PointsToWorkspace.compute")]
        [SerializeField] private ComputeShader pointsToWorkspaceCS;

        [Header("Core SDF Compute Shaders")]
        [SerializeField] private ComputeShader pointAabbCS;
        [SerializeField] private ComputeShader sdfClearCS;
        [SerializeField] private ComputeShader sdfVoxelizeSeedsCS;
        [SerializeField] private ComputeShader sdfJumpFloodCS;
        [SerializeField] private ComputeShader sdfFinalizeCS;

        [Header("Visualization (optional)")]
        [Tooltip("Compute shader: SdfSlice.compute (optional debug).")]
        [SerializeField] private ComputeShader sdfSliceCS;

        [Tooltip("Compute shader: SdfOverlay.compute (optional; depth-based fullscreen overlay).")]
        [SerializeField] private ComputeShader sdfOverlayCS;

        [Tooltip("Camera used for overlay sizing (optional).")]
        [SerializeField] private Camera overlayCamera;

        [SerializeField] private bool enableSliceDebug = false;
        [SerializeField] private bool enableOverlay = false;

        // Workspace derived
        private Vector3 _workspaceCornerWS; // min corner in workspace space

        // Modules
        private WorkspacePointConverter _pointConverter;
        private SdfGenerator _core;
        private SdfSliceDebugger _sliceDbg;
        private SdfOverlayRenderer _overlay; // unified fullscreen overlay renderer

        private bool _initialized;
        private bool _modelInitialized;

        // Public outputs
        public ComputeBuffer PointsWS => _pointConverter?.OutputBuffer;

        public SdfVolumeData Global => _core != null ? _core.Global : default;
        public SdfVolumeData Local => _core != null ? _core.Local : default;

        public RenderTexture OverlayRT => _overlay != null ? _overlay.OverlayRT : null;

        /// <summary>
        /// Call this when your PlacementBlock is created or moved/changed.
        /// </summary>
        public void SetWorkspace(Transform newWorkspaceRoot, Vector3 newWorkspaceSizeWS)
        {
            workspaceRoot = newWorkspaceRoot;
            workspaceSizeWS = newWorkspaceSizeWS;

            // Unity cube pivot is centered -> min corner is -size/2 in local space
            _workspaceCornerWS = -0.5f * workspaceSizeWS;

            // Also update workspace info for any consumers
            TryInitializeModel(); // modelLocalToWorkspace depends on worldToWorkspace (workspaceRoot)
        }

        /// <summary>
        /// Call this after instantiating the target sculpture model.
        /// </summary>
        public void SetModelInstance(GameObject newModelInstance)
        {
            modelInstance = newModelInstance;
            _modelInitialized = false;
            TryInitializeModel();
        }

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void OnDestroy()
        {
            DisposeAll();
        }

        private void InitializeIfNeeded()
        {
            if (_initialized) return;

            if (!pointsToWorkspaceCS)
                Debug.LogError("[SdfSystem] pointsToWorkspaceCS missing (PointsToWorkspace.compute).");

            _pointConverter = new WorkspacePointConverter(pointsToWorkspaceCS);

            _core = new SdfGenerator(
                pointAabbCS,
                sdfClearCS,
                sdfVoxelizeSeedsCS,
                sdfJumpFloodCS,
                sdfFinalizeCS
            );

            if (enableSliceDebug && sdfSliceCS != null)
                _sliceDbg = new SdfSliceDebugger(sdfSliceCS);

            if (enableOverlay && sdfOverlayCS != null)
            {
                if (!overlayCamera) overlayCamera = Camera.main;

                _overlay = gameObject.AddComponent<SdfOverlayRenderer>();
                _overlay.SetResources(sdfOverlayCS, overlayCamera);
            }

            // If workspace is already assigned in inspector, compute corner
            if (workspaceRoot != null)
                _workspaceCornerWS = -0.5f * workspaceSizeWS;

            _initialized = true;

            // If model is already assigned, init
            TryInitializeModel();
        }

        private void DisposeAll()
        {
            _pointConverter?.Dispose();
            _pointConverter = null;

            _core?.Dispose();
            _core = null;

            _sliceDbg?.Dispose();
            _sliceDbg = null;

            // OverlayRT is owned by SdfOverlayRenderer component; Unity will destroy it with the GameObject,
            // but we can let it clean up in its OnDestroy.
        }

        /// <summary>
        /// Convert and upload the model mesh into workspace space, then initialize Core.
        /// This must be called whenever workspaceRoot changes OR modelInstance changes.
        /// </summary>
        private void TryInitializeModel()
        {
            if (!_initialized) return;
            if (_core == null) return;
            if (workspaceRoot == null) return;
            if (modelInstance == null) return;

            var ws = new WorkspaceInfo(workspaceRoot, _workspaceCornerWS, workspaceSizeWS);

            if (!WorkspaceMeshConverter.TryGetModelMesh(
                    modelInstance,
                    ws,
                    out Mesh mesh,
                    out Matrix4x4 modelLocalToWorkspace,
                    out _))
            {
                Debug.LogWarning("[SdfSystem] Could not find MeshFilter/Mesh on modelInstance.");
                return;
            }

            _core.Initialize(mesh, modelLocalToWorkspace);
            _modelInitialized = true;
        }

        /// <summary>
        /// Main entry point each depth frame:
        /// Feed world-space point cloud buffer in, update SDF (local/global).
        /// </summary>
        public void UpdateWithWorldPoints(ComputeBuffer worldPointsFloat4, int pointCount)
        {
            InitializeIfNeeded();

            if (!_modelInitialized)
                TryInitializeModel();

            if (!_modelInitialized)
                return;

            if (workspaceRoot == null)
            {
                Debug.LogWarning("[SdfSystem] workspaceRoot is null. Call SetWorkspace(...) first.");
                return;
            }

            if (worldPointsFloat4 == null || pointCount <= 0)
                return;

            // World -> Workspace
            Matrix4x4 worldToWorkspace = workspaceRoot.worldToLocalMatrix;

            // Convert points to workspace space on GPU
            ComputeBuffer pointsWS = _pointConverter.Convert(worldPointsFloat4, pointCount, worldToWorkspace);

            // Update core volumes using workspace-space points and workspace bounds
            _core.Update(pointsWS, pointCount, _workspaceCornerWS, workspaceSizeWS);
        }

        /// <summary>
        /// Optional debug: build a slice from the GLOBAL TSDF.
        /// axis: 0=X, 1=Y, 2=Z. slice01: 0..1.
        /// </summary>
        public RenderTexture BuildGlobalSlice(int axis, float slice01)
        {
            if (_sliceDbg == null || _core == null) return null;

            var g = _core.Global;
            if (!g.IsValid) return null;

            return _sliceDbg.BuildSlice(g.Tsdf, g.Resolution, axis, slice01, g.Mu);
        }

        /// <summary>
        /// Optional: render fullscreen overlay from depth.
        /// You must supply depth texture and invViewProj for the camera that produced it.
        /// </summary>
        public void RenderOverlay(Texture depthTex, Matrix4x4 invViewProj)
        {
            if (!enableOverlay || _overlay == null) return;
            if (_core == null) return;

            var g = _core.Global;
            if (!g.IsValid) return;

            Matrix4x4 worldToWorkspace = workspaceRoot.worldToLocalMatrix;

            var l = _core.Local;

            _overlay.RenderOverlay(
                depthTex,
                invViewProj,
                worldToWorkspace,

                l.IsValid ? l.Tsdf : null,
                l.Corner, l.Size, l.Mu,

                g.Tsdf,
                g.Corner, g.Size, g.Mu
            );
        }
    }

