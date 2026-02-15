using Assets.Scripts.Depth.Quest3.OXDepth;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Depth.Quest3.OXDepth.Diagnostics
{
    /// <summary>
    /// Renders point cloud data as screen-space dots.
    /// Works with OXDepthPointCloudAPI to visualize point data.
    /// </summary>
    [RequireComponent(typeof(OXDepthPointCloudAPI))]
    public class OXDepthPointCloudRenderer : MonoBehaviour
    {
        #region Configuration
        [Header("Rendering")]
        [SerializeField] private Shader screenDotsShader;

        [Header("Visual Settings")]
        [Range(1, 20)]
        [Tooltip("Size of each dot in pixels")]
        public float dotSizePx = 2f;

        [Range(0.1f, 100f)]
        [Tooltip("Maximum distance to render points (meters)")]
        public float maxDistanceMeters = 6.0f;

        [Header("Debug")]
        [Range(10, 600)]
        [Tooltip("Log rendering info every N frames")]
        public int debugLogEveryNFrames = 120;
        #endregion

        #region Private Fields
        private OXDepthPointCloudAPI _pointCloudAPI;
        private Material _dotMaterial;
        private ComputeBuffer _indirectArgsBuffer;
        private Camera _referenceCamera;

        private readonly uint[] _indirectArgs = new uint[5] { 0, 1, 0, 0, 0 };

        private bool _isInitialized;
        private int _lastRenderedCount;

        // Shader property IDs
        private static readonly int ID_DotSizePx = Shader.PropertyToID("_DotSizePx");
        private static readonly int ID_Points = Shader.PropertyToID("_Points");
        private static readonly int ID_DistFromPosWS = Shader.PropertyToID("_DistFromPosWS");
        private static readonly int ID_MaxDistMeters = Shader.PropertyToID("_MaxDistMeters");
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _pointCloudAPI = GetComponent<OXDepthPointCloudAPI>();
        }

        private void OnEnable()
        {
            if (!Initialize())
            {
                enabled = false;
                return;
            }

            SubscribeToAPI();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable()
        {
            UnsubscribeFromAPI();
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            Cleanup();
        }
        #endregion

        #region Initialization
        private bool Initialize()
        {
            if (!ValidateConfiguration())
            {
                return false;
            }

            CreateMaterial();
            CreateIndirectArgsBuffer();
            FindReferenceCamera();

            _isInitialized = true;
            Debug.Log("[OXDepthRenderer] Initialized successfully");
            return true;
        }

        private bool ValidateConfiguration()
        {
            if (!screenDotsShader)
            {
                Debug.LogError("[OXDepthRenderer] screenDotsShader not assigned!");
                return false;
            }

            if (!_pointCloudAPI)
            {
                Debug.LogError("[OXDepthRenderer] OXDepthPointCloudAPI component required!");
                return false;
            }

            return true;
        }

        private void CreateMaterial()
        {
            _dotMaterial = new Material(screenDotsShader)
            {
                name = "PointCloudDotsMaterial"
            };
        }

        private void CreateIndirectArgsBuffer()
        {
            _indirectArgsBuffer = new ComputeBuffer(
                1,
                sizeof(uint) * 5,
                ComputeBufferType.IndirectArguments
            );
        }

        private void FindReferenceCamera()
        {
            _referenceCamera = Camera.main;
            if (!_referenceCamera)
            {
                _referenceCamera = FindObjectOfType<Camera>();
            }
        }

        private void Cleanup()
        {
            if (_dotMaterial)
            {
                Destroy(_dotMaterial);
                _dotMaterial = null;
            }

            if (_indirectArgsBuffer != null)
            {
                _indirectArgsBuffer.Release();
                _indirectArgsBuffer = null;
            }

            _isInitialized = false;
        }
        #endregion

        #region API Integration
        private void SubscribeToAPI()
        {
            _pointCloudAPI.OnPointCloudUpdated += OnPointCloudUpdated;
            _pointCloudAPI.OnDepthInvalid += OnDepthInvalid;
        }

        private void UnsubscribeFromAPI()
        {
            if (_pointCloudAPI != null)
            {
                _pointCloudAPI.OnPointCloudUpdated -= OnPointCloudUpdated;
                _pointCloudAPI.OnDepthInvalid -= OnDepthInvalid;
            }
        }

        private void OnPointCloudUpdated(PointCloudData data)
        {
            UpdateIndirectArgs(data.pointCount);
            _lastRenderedCount = data.pointCount;
        }

        private void OnDepthInvalid()
        {
            _lastRenderedCount = 0;
        }
        #endregion

        #region Rendering
        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!ShouldRender(camera))
            {
                return;
            }

            RenderPointCloud(camera);
            LogRenderingStats();
        }

        private bool ShouldRender(Camera camera)
        {
            if (!_isInitialized || !_pointCloudAPI.IsReady)
                return false;

            if (_lastRenderedCount <= 0)
                return false;

            if (!IsValidCameraType(camera))
                return false;

            return true;
        }

        private bool IsValidCameraType(Camera camera)
        {
            return camera.cameraType == CameraType.Game ||
                   camera.cameraType == CameraType.VR;
        }

        private void RenderPointCloud(Camera camera)
        {
            UpdateMaterialProperties(camera);
            DrawPoints(camera);
        }

        private void UpdateMaterialProperties(Camera camera)
        {
            _dotMaterial.SetFloat(ID_DotSizePx, dotSizePx);
            _dotMaterial.SetBuffer(ID_Points, _pointCloudAPI.PointBuffer);

            Vector3 referencePosition = GetReferencePosition(camera);
            _dotMaterial.SetVector(ID_DistFromPosWS, referencePosition);
            _dotMaterial.SetFloat(ID_MaxDistMeters, maxDistanceMeters);
        }

        private Vector3 GetReferencePosition(Camera camera)
        {
            if (_referenceCamera != null)
            {
                return _referenceCamera.transform.position;
            }
            return camera.transform.position;
        }

        private void DrawPoints(Camera camera)
        {
            Bounds bounds = CreateRenderBounds(camera);

            Graphics.DrawProceduralIndirect(
                _dotMaterial,
                bounds,
                MeshTopology.Triangles,
                _indirectArgsBuffer,
                0,
                camera
            );
        }

        private Bounds CreateRenderBounds(Camera camera)
        {
            // Large bounds to ensure points are not culled
            return new Bounds(camera.transform.position, Vector3.one * 2000f);
        }

        private void UpdateIndirectArgs(int pointCount)
        {
            // 6 vertices per point (2 triangles)
            _indirectArgs[0] = (uint)(6 * pointCount);
            _indirectArgs[1] = 1; // instance count
            _indirectArgs[2] = 0; // start vertex
            _indirectArgs[3] = 0; // start instance
            _indirectArgs[4] = 0; // unused

            _indirectArgsBuffer.SetData(_indirectArgs);
        }

        private void LogRenderingStats()
        {
            if (debugLogEveryNFrames <= 0)
                return;

            if (Time.frameCount % debugLogEveryNFrames != 0)
                return;

            Diagnostics.OXDepthLogger.Info(Diagnostics.OXDepthLogger.TAG_RENDER,$"[OXDepthRenderer] Rendering {_lastRenderedCount} points " +
                      $"({6 * _lastRenderedCount} vertices) | " +
                      $"DotSize={dotSizePx}px, MaxDist={maxDistanceMeters}m");
        }
        #endregion

        #region Public API
        /// <summary>
        /// Enable/disable rendering without disabling the component.
        /// </summary>
        public void SetRenderingEnabled(bool enabled)
        {
            this.enabled = enabled;
        }

        /// <summary>
        /// Get current rendering statistics.
        /// </summary>
        public RenderStatistics GetRenderStatistics()
        {
            return new RenderStatistics
            {
                pointsRendered = _lastRenderedCount,
                verticesRendered = 6 * _lastRenderedCount,
                dotSize = dotSizePx,
                maxDistance = maxDistanceMeters,
                isRendering = _isInitialized && _pointCloudAPI.IsReady
            };
        }
        #endregion
    }

    #region Data Structures
    /// <summary>
    /// Current rendering statistics.
    /// </summary>
    public struct RenderStatistics
    {
        public int pointsRendered;
        public int verticesRendered;
        public float dotSize;
        public float maxDistance;
        public bool isRendering;

        public override string ToString()
        {
            return $"RenderStats[points={pointsRendered}, vertices={verticesRendered}, " +
                   $"dotSize={dotSize}px, maxDist={maxDistance}m, active={isRendering}]";
        }
    }
    #endregion
}