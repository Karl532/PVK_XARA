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
/// 2) Workspace space is Workspace.localSpace (workspaceRoot).
/// 3) Workspace pivot is centered (Unity cube), so workspaceCornerWS = -size/2.
/// 4) Model instance is placed in the same anchored world (ideally parented under CalibrationOrigin).
/// </summary>
public partial class SdfGenerationSystem : MonoBehaviour
{
    [Header("Visualization (optional)")]
    [SerializeField] private bool enableSliceDebug = false;

    [Header("SDF Debug/Perf")]
    [SerializeField] private bool disableLocalVolume = false;
    [SerializeField] private float localRebuildMinIntervalSeconds = 0f;
    [SerializeField] private float globalRebuildMinIntervalSeconds = 0f;
    [SerializeField] private int globalResolution = 128;
    [SerializeField] private int localResolution = 256;
    [SerializeField] private int maxStagesPerFrame = 1;

    [Header("Debug")]
    [SerializeField] private bool enableDebug = true;
    [SerializeField] private bool verboseDebug = true;
    [SerializeField] private bool timingDebug = false;
    [SerializeField] private float debugLogIntervalSeconds = 1f;

    private readonly SdfWorkspaceState _workspace = new SdfWorkspaceState();
    private readonly SdfModelState _model = new SdfModelState();

    private WorkspacePointConverter _pointConverter;
    private SdfGenerator _core;
    private SdfSliceDebugger _sliceDbg;
    private SdfShaderSet _shaders;

    private bool _initialized;
    private ComputeBuffer _lastFilteredPoints;
    private int _lastFilteredCount;
    private Matrix4x4 _lastInputToWorld = Matrix4x4.identity;
    private Matrix4x4 _lastInputToWorkspace = Matrix4x4.identity;
    private bool _suppressSculptGuide;
    private RenderTexture _cachedGlobalTsdf;
    private RenderTexture _cachedLocalTsdf;
    private SdfVolumeData _cachedGlobal;
    private SdfVolumeData _cachedLocal;
    private bool _isCaching;

    public ComputeBuffer PointsWS => _pointConverter?.OutputBuffer;
    public ComputeBuffer LastFilteredPoints => _lastFilteredPoints;
    public int LastFilteredCount => _lastFilteredCount;
    public SdfVolumeData Global => _core != null ? _core.Global : default;
    public SdfVolumeData Local => _core != null ? _core.Local : default;
    public SdfVolumeData CachedGlobal => _cachedGlobal;
    public SdfVolumeData CachedLocal => _cachedLocal;
    public bool IsBuilding => _core != null && _core.IsBuilding;
    public Transform WorkspaceRoot => _workspace.Root;
    public Vector3 WorkspaceCorner => _workspace.Corner;
    public Vector3 WorkspaceSize => _workspace.Size;

    public event System.Action<SdfVisualizationData> VisualizationDataUpdated;
}
