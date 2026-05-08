using UnityEngine;

/// <summary>
/// Renders a small sphere in world space for each QR corner marker so you can
/// see exactly where the headset thinks each code is.
///
/// Green  = currently tracked live this frame.
/// Yellow = last known position (marker not in FOV right now).
/// Spheres are hidden until the marker has been seen at least once.
/// </summary>
public class QrMarkerVisualizer : MonoBehaviour
{
    [Tooltip("Diameter of each marker sphere in metres.")]
    [SerializeField] private float markerSize = 0.04f;

    private int[]        _ids;
    private GameObject[] _spheres;
    private Renderer[]   _renderers;
    private Material     _matLive;
    private Material     _matLastKnown;

    private static readonly Color ColorLive      = new Color(0.0f, 1.0f, 0.5f, 1f);
    private static readonly Color ColorLastKnown = new Color(1.0f, 0.8f, 0.0f, 1f);

    void Awake()
    {
        _matLive      = BuildMaterial(ColorLive);
        _matLastKnown = BuildMaterial(ColorLastKnown);
    }

    void Update()
    {
        EnsureSpheres();
        RefreshPositions();
    }

    // ── Internal helpers ────────────────────────────────────────────────

    private void EnsureSpheres()
    {
        var settings = SettingsManager.Instance?.settings;
        int[] ids = settings?.qrCornerMarkerIds;
        if (ids == null || ids.Length == 0) return;

        // Rebuild sphere array if IDs changed
        bool needRebuild = _ids == null || ids.Length != _ids.Length;
        if (!needRebuild)
        {
            for (int i = 0; i < ids.Length; i++)
                if (ids[i] != _ids[i]) { needRebuild = true; break; }
        }

        if (!needRebuild) return;

        DestroySpheres();
        _ids      = (int[])ids.Clone();
        _spheres  = new GameObject[ids.Length];
        _renderers= new Renderer[ids.Length];

        for (int i = 0; i < ids.Length; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"QrMarker_{ids[i]}";
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * markerSize;
            go.SetActive(false);

            var rend = go.GetComponent<Renderer>();
            rend.material = _matLastKnown;
            _spheres[i]   = go;
            _renderers[i] = rend;
        }
    }

    private void RefreshPositions()
    {
        if (_spheres == null) return;
        var tracker = FiducialTrackingManager.Instance;
        if (tracker == null)
        {
            foreach (var s in _spheres) if (s) s.SetActive(false);
            return;
        }

        for (int i = 0; i < _ids.Length; i++)
        {
            bool live = tracker.TryGetMarkerPose(_ids[i], out var liveP);
            bool seen = tracker.TryGetLastKnownMarkerPose(_ids[i], out var lastP);

            if (!seen)
            {
                _spheres[i].SetActive(false);
                continue;
            }

            _spheres[i].SetActive(true);
            _spheres[i].transform.position = lastP.position;
            _renderers[i].sharedMaterial   = live ? _matLive : _matLastKnown;
        }
    }

    private void DestroySpheres()
    {
        if (_spheres == null) return;
        foreach (var s in _spheres) if (s) Destroy(s);
        _spheres   = null;
        _renderers = null;
    }

    void OnDestroy()
    {
        DestroySpheres();
        if (_matLive)      Destroy(_matLive);
        if (_matLastKnown) Destroy(_matLastKnown);
    }

    private static Material BuildMaterial(Color color)
    {
        // Prefer URP Unlit so the marker is always fully bright regardless of lighting.
        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color")
                  ?? Shader.Find("Standard");

        var mat = new Material(shader) { name = "QrMarkerMat" };

        // Both URP Unlit and Unlit/Color expose _BaseColor / _Color
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        return mat;
    }
}
