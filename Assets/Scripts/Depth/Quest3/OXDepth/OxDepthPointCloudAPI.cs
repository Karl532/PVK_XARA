using Assets.Scripts.Debug;
using Assets.Scripts.Depth.Quest3.OXDepth.OxUtils;
using System;
using Unity.XR.Oculus;
using UnityEngine;
using UnityEngine.XR;

namespace Assets.Scripts.Depth.Quest3.OXDepth
{
    /// <summary>
    /// Core API for managing and generating 3D point cloud data from environment depth.
    /// Use this to get point positions without rendering concerns.
    /// Can be enabled/disabled to control processing and resource usage.
    /// </summary>
    public class OXDepthPointCloudAPI : MonoBehaviour
    {
        #region Events
        /// <summary>
        /// Called when new point cloud data is available.
        /// </summary>
        public event Action<PointCloudData> OnPointCloudUpdated;

        /// <summary>
        /// Called when depth frame becomes invalid.
        /// </summary>
        public event Action OnDepthInvalid;
        #endregion

        #region Configuration
        [Header("XR Origin (REQUIRED)")]
        [Tooltip("The XR Origin transform for correct world space positioning")]
        [SerializeField] private Transform trackingOrigin;

        [Header("Compute Shader (REQUIRED)")]
        [SerializeField] private ComputeShader buildPointCloudCS;

        [Header("Point Cloud Settings")]
        [Range(1, 32)]
        [Tooltip("Pixel stride - higher values = fewer points but better performance")]
        public int skipPixels = 2;

        [Tooltip("Use left eye (true) or right eye (false) depth slice")]
        public bool useLeftEyeSlice = true;

        [Header("Depth Filtering")]
        [Range(0f, 1f)]
        [Tooltip("Minimum normalized depth (0-1 range)")]
        public float minDepth01 = 0.001f;

        [Range(0f, 1f)]
        [Tooltip("Maximum normalized depth (0-1 range)")]
        public float maxDepth01 = 0.999f;

        [Header("Conventions")]
        [Tooltip("Flip Y coordinate if needed")]
        public bool flipY = false;

        [Header("Capacity")]
        [Tooltip("Maximum number of points to generate")]
        public int maxPoints = 250_000;

        [Header("Debug")]
        [Tooltip("Log tracking origin info every N seconds (0 = every frame)")]
        public float logTrackingOriginIntervalSeconds = 1f;
        #endregion

        #region Public Properties
        /// <summary>
        /// Is the API currently ready to provide point cloud data?
        /// </summary>
        public bool IsReady => _isInitialized && _depthReady && enabled;

        /// <summary>
        /// Current point count from last update.
        /// </summary>
        public int PointCount => _lastPointCount;

        /// <summary>
        /// Current depth texture dimensions.
        /// </summary>
        public Vector2Int DepthResolution => new Vector2Int(_depthWidth, _depthHeight);

        /// <summary>
        /// Get the raw point buffer (read-only access).
        /// </summary>
        public ComputeBuffer PointBuffer => _buffers?.Points;

        /// <summary>
        /// Get the latest depth texture.
        /// </summary>
        public RenderTexture DepthTexture => _depthRT;

        /// <summary>
        /// Last inverse depth view-projection matrix used to build points.
        /// </summary>
        public Matrix4x4 LastInvDepthViewProj { get; private set; } = Matrix4x4.identity;

        /// <summary>
        /// Last tracking-to-world matrix used to build points.
        /// </summary>
        public Matrix4x4 LastTrackingToWorld { get; private set; } = Matrix4x4.identity;

        public int EyeSlice => useLeftEyeSlice ? 0 : 1;
        public bool FlipY => flipY;

        /// <summary>
        /// Get the latest statistics from GPU.
        /// </summary>
        public GpuStatistics LastStatistics => _lastStats;
        #endregion

        #region Private Fields
        private XRDisplaySubsystem _display;
        private RenderTexture _depthRT;
        private OXDepthBuffers _buffers;

        private int _kernelBuild;
        private int _idPoints, _idCounter, _idDepthTex, _idGpuStats;

        private bool _isInitialized;
        private bool _depthReady;
        private int _depthWidth, _depthHeight;
        private int _lastPointCount;

        private readonly uint[] _countReadback = new uint[1];
        private readonly uint[] _statsReadback = new uint[8];
        private GpuStatistics _lastStats;
        private float _lastTrackingOriginLogTime = -999f;

        // Shader property IDs
        private static readonly int ID_DepthSize = Shader.PropertyToID("_DepthSize");
        private static readonly int ID_Skip = Shader.PropertyToID("_Skip");
        private static readonly int ID_EyeSlice = Shader.PropertyToID("_EyeSlice");
        private static readonly int ID_MaxPoints = Shader.PropertyToID("_MaxPoints");
        private static readonly int ID_FlipY = Shader.PropertyToID("_FlipY");
        private static readonly int ID_MinDepth01 = Shader.PropertyToID("_MinDepth01");
        private static readonly int ID_MaxDepth01 = Shader.PropertyToID("_MaxDepth01");
        private static readonly int ID_InvDepthViewProj = Shader.PropertyToID("_InvDepthViewProj");
        private static readonly int ID_TrackingToWorld = Shader.PropertyToID("_TrackingToWorld");
        private static readonly int ID_GlobalReproj = Shader.PropertyToID("_EnvironmentDepthReprojectionMatrices");
        #endregion

        private const string TAG_CORE = "[OX_DEPTH_CORE]";

        #region Unity Lifecycle
        private void Awake()
        {
            // Start disabled by default - let the factory/provider enable it
            enabled = false;
        }

        private void OnEnable()
        {
            DebugService.Log("[OXDepthAPI] OnEnable called", tag: TAG_CORE);

            if (!Initialize())
            {
                DebugService.Error("[OXDepthAPI] Initialization failed, disabling component", tag: TAG_CORE);
                enabled = false;
                return;
            }
        }

        private void OnDisable()
        {
            DebugService.Log("[OXDepthAPI] OnDisable called", tag: TAG_CORE);
            Shutdown();
        }

        private void Update()
        {
            if (!_isInitialized) return;

            UpdatePointCloud();
        }

        private void OnDestroy()
        {
            // Ensure cleanup happens even if disabled
            if (_isInitialized)
            {
                Shutdown();
            }
        }
        #endregion

        #region Initialization
        private bool Initialize()
        {
            // Prevent re-initialization if already initialized
            if (_isInitialized)
            {
                DebugService.Warn("[OXDepthAPI] Already initialized", tag: TAG_CORE);
                return true;
            }

            DebugService.Log("[OXDepthAPI] Initializing...", tag: TAG_CORE);

            if (!ValidateConfiguration())
            {
                return false;
            }

            if (!InitializeXRSubsystem())
            {
                return false;
            }

            if (!InitializeDepthSystem())
            {
                return false;
            }

            InitializeComputeShader();
            AllocateBuffers();

            _isInitialized = true;

            DebugService.Log("[OXDepthAPI] Initialization complete", tag: TAG_CORE);

            return true;
        }

    private bool ValidateConfiguration()
    {
        if (!trackingOrigin)
        {
            trackingOrigin = OXDepthXR.FindXROrigin();
            if (!trackingOrigin)
            {
                    DebugService.Error("[OXDepthAPI] CRITICAL: trackingOrigin not assigned! Assign XR Origin transform.", tag: TAG_CORE);
                return false;
            }
        }

        string parentName = trackingOrigin.parent != null ? trackingOrigin.parent.name : "null";
        DebugService.Log($"[OXDepthAPI] trackingOrigin='{trackingOrigin.name}' parent='{parentName}' pos={trackingOrigin.position} rot={trackingOrigin.rotation.eulerAngles}", tag: TAG_CORE);

        if (!buildPointCloudCS)
        {
            buildPointCloudCS = LoadBuildPointCloudShader();
            if (!buildPointCloudCS)
            {
                    DebugService.Error("[OXDepthAPI] buildPointCloudCS not assigned!", tag: TAG_CORE);
                    return false;
                }
            }

            return true;
        }

        private static ComputeShader LoadBuildPointCloudShader()
        {
#if UNITY_EDITOR
            const string editorPath = "Assets/Resources/OXDepth/BuildPointCloud.compute";
            var editorCs = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(editorPath);
            if (editorCs != null) return editorCs;
#endif
            var cs = Resources.Load<ComputeShader>("OXDepth/BuildPointCloud");
            if (cs == null)
            {
                DebugService.Warn("[OXDepthAPI] Could not load BuildPointCloud.compute from Resources/OXDepth.", tag: TAG_CORE);
            }
            return cs;
        }

        private bool InitializeXRSubsystem()
        {
            _display = OXDepthXR.GetXRDisplaySubsystem();
            if (_display == null)
            {
                DebugService.Error("[OXDepthAPI] No XRDisplaySubsystem found. Must run on XR device.", tag: TAG_CORE);
                return false;
            }

            return true;
        }

        private bool InitializeDepthSystem()
        {
            if (!OXDepthXR.EnsureDepthEnabled())
            {
                DebugService.Error("[OXDepthAPI] Environment Depth not supported on this device.", tag: TAG_CORE);
                return false;
            }

            return true;
        }

        private void InitializeComputeShader()
        {
            _kernelBuild = buildPointCloudCS.FindKernel("CSMain");

            _idPoints = Shader.PropertyToID("_Points");
            _idCounter = Shader.PropertyToID("_Counter");
            _idDepthTex = Shader.PropertyToID("_DepthTex");
            _idGpuStats = Shader.PropertyToID("_GpuStats");
        }

        private void AllocateBuffers()
        {
            _buffers = new OXDepthBuffers();
            _buffers.Allocate(maxPoints, 1);
        }

        private void Shutdown()
        {
            if (!_isInitialized)
            {
                return;
            }

            DebugService.Log("[OXDepthAPI] Shutting down...", tag: TAG_CORE);

            // Clear events to prevent callbacks after shutdown
            OnPointCloudUpdated = null;
            OnDepthInvalid = null;

            // Disable depth system
            OXDepthXR.DisableDepth();
            OXDepthXR.ShutdownDepth();

            // Release GPU resources
            _buffers?.Release();
            _buffers = null;

            // Release render texture reference
            _depthRT = null;

            // Clear state
            _isInitialized = false;
            _depthReady = false;
            _lastPointCount = 0;

            DebugService.Log("[OXDepthAPI] Shutdown complete", tag: TAG_CORE);
        }
        #endregion

        #region Point Cloud Generation
        private void UpdatePointCloud()
        {
            LogTrackingOrigin();

            if (!AcquireDepthTexture())
            {
                HandleDepthInvalid();
                return;
            }

            Matrix4x4 invDepthViewProj = GetInverseDepthViewProjection();
            if (invDepthViewProj == Matrix4x4.zero)
            {
                HandleDepthInvalid();
                return;
            }

            Matrix4x4 trackingToWorld = GetTrackingToWorldMatrix();
            LastInvDepthViewProj = invDepthViewProj;
            LastTrackingToWorld = trackingToWorld;

            GeneratePoints(invDepthViewProj, trackingToWorld);
            ReadbackResults();
            NotifyUpdate();
        }

        private bool AcquireDepthTexture()
        {
            int eye = useLeftEyeSlice ? 0 : 1;

            Utils.EnvironmentDepthFrameDesc frame;
            uint texId;

            if (!OxUtils.OXDepthXR.TryGetDepthFrameAndTexture(_display, eye, out frame, out _depthRT, out texId))
            {
                return false;
            }

            _depthReady = true;
            _depthWidth = _depthRT.width;
            _depthHeight = _depthRT.height;

            return true;
        }

        private Matrix4x4 GetInverseDepthViewProjection()
        {
            int eye = useLeftEyeSlice ? 0 : 1;
            Matrix4x4 invMatrix;

            if (!OXDepthXR.TryGetReprojectionMatrix(eye, out invMatrix))
            {
                DebugService.Warn("[OXDepthAPI] Reprojection matrices not available. Add EnvironmentDepthManager to scene.", tag: TAG_CORE);
                return Matrix4x4.zero;
            }

            return invMatrix;
        }

        private Matrix4x4 GetTrackingToWorldMatrix()
        {
            Matrix4x4 matrix = OXDepthXR.GetTrackingToWorldMatrix(trackingOrigin);

            if (trackingOrigin == null)
            {
            DebugService.Warn("[OXDepthAPI] trackingOrigin is NULL! Points may be misaligned.", tag: TAG_CORE);
            }

            return matrix;
        }

        private void GeneratePoints(Matrix4x4 invDepthViewProj, Matrix4x4 trackingToWorld)
        {
            _buffers.ResetForFrame();

            SetComputeBuffers();
            SetComputeParameters(invDepthViewProj, trackingToWorld);
            DispatchCompute();
        }

        private void SetComputeBuffers()
        {
            buildPointCloudCS.SetBuffer(_kernelBuild, _idPoints, _buffers.Points);
            buildPointCloudCS.SetBuffer(_kernelBuild, _idCounter, _buffers.Counter);
            buildPointCloudCS.SetBuffer(_kernelBuild, _idGpuStats, _buffers.Stats);
            buildPointCloudCS.SetTexture(_kernelBuild, _idDepthTex, _depthRT);
        }

        private void SetComputeParameters(Matrix4x4 invDepthViewProj, Matrix4x4 trackingToWorld)
        {
            int stride = Mathf.Max(1, skipPixels);
            int eye = useLeftEyeSlice ? 0 : 1;

            buildPointCloudCS.SetInts(ID_DepthSize, _depthWidth, _depthHeight);
            buildPointCloudCS.SetInt(ID_Skip, stride);
            buildPointCloudCS.SetInt(ID_EyeSlice, eye);
            buildPointCloudCS.SetInt(ID_FlipY, flipY ? 1 : 0);
            buildPointCloudCS.SetInt(ID_MaxPoints, maxPoints);
            buildPointCloudCS.SetFloat(ID_MinDepth01, minDepth01);
            buildPointCloudCS.SetFloat(ID_MaxDepth01, maxDepth01);
            buildPointCloudCS.SetMatrix(ID_InvDepthViewProj, invDepthViewProj);
            buildPointCloudCS.SetMatrix(ID_TrackingToWorld, trackingToWorld);
        }

        private void LogTrackingOrigin()
        {
            if (trackingOrigin == null)
                return;

            float now = Time.unscaledTime;
            if (logTrackingOriginIntervalSeconds > 0f &&
                now - _lastTrackingOriginLogTime < logTrackingOriginIntervalSeconds)
                return;

            _lastTrackingOriginLogTime = now;

            string parentName = trackingOrigin.parent != null ? trackingOrigin.parent.name : "null";
            DebugService.Log($"[OXDepthAPI] trackingOrigin='{trackingOrigin.name}' parent='{parentName}' pos={trackingOrigin.position} rot={trackingOrigin.rotation.eulerAngles} scale={trackingOrigin.localScale}", tag: TAG_CORE);
        }

        private void DispatchCompute()
        {
            int groupsX = Mathf.CeilToInt(_depthWidth / 8f);
            int groupsY = Mathf.CeilToInt(_depthHeight / 8f);
            buildPointCloudCS.Dispatch(_kernelBuild, groupsX, groupsY, 1);
        }

        private void ReadbackResults()
        {
            _buffers.Counter.GetData(_countReadback);
            _lastPointCount = (int)_countReadback[0];

            _buffers.Stats.GetData(_statsReadback);
            _lastStats = new GpuStatistics
            {
                threadsExecuted = _statsReadback[0],
                pixelsSkipped = _statsReadback[1],
                outOfBounds = _statsReadback[2],
                depthInvalid = _statsReadback[3],
                depthRangeRejected = _statsReadback[4],
                pointsAppended = _statsReadback[6],
                nanWorldPositions = _statsReadback[7]
            };
        }

        private void HandleDepthInvalid()
        {
            _depthReady = false;
            _lastPointCount = 0;
            OnDepthInvalid?.Invoke();
        }

        private void NotifyUpdate()
        {
            if (OnPointCloudUpdated != null && _lastPointCount > 0)
            {
                var data = new PointCloudData
                {
                    pointBuffer = _buffers.Points,
                    pointCount = _lastPointCount,
                    resolution = new Vector2Int(_depthWidth, _depthHeight),
                    statistics = _lastStats
                };

                OnPointCloudUpdated.Invoke(data);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Manually enable the API. Usually called by factory/provider.
        /// </summary>
        public void EnableAPI()
        {
            if (!enabled)
            {
                enabled = true;
            }
        }

        /// <summary>
        /// Manually disable the API. Usually called by factory/provider.
        /// </summary>
        public void DisableAPI()
        {
            if (enabled)
            {
                enabled = false;
            }
        }

        #endregion
    }

    #region Data Structures
    /// <summary>
    /// Contains point cloud data from a single frame.
    /// </summary>
    public struct PointCloudData
    {
        public ComputeBuffer pointBuffer;
        public int pointCount;
        public Vector2Int resolution;
        public GpuStatistics statistics;
    }

    /// <summary>
    /// Statistics from GPU point generation.
    /// </summary>
    public struct GpuStatistics
    {
        public uint threadsExecuted;
        public uint pixelsSkipped;
        public uint outOfBounds;
        public uint depthInvalid;
        public uint depthRangeRejected;
        public uint pointsAppended;
        public uint nanWorldPositions;

        public override string ToString()
        {
            return $"GpuStats[threads={threadsExecuted}, skipped={pixelsSkipped}, " +
                   $"oob={outOfBounds}, invalid={depthInvalid}, rangeReject={depthRangeRejected}, " +
                   $"appended={pointsAppended}, nan={nanWorldPositions}]";
        }
    }
    #endregion
}


