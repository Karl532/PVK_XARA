using System;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

/// <summary>
/// Loads glTF/glb models from a local file path at runtime and
/// instantiates them under the calibration origin.
/// </summary>
public class RuntimeModelLoader : MonoBehaviour
{
    public static RuntimeModelLoader Instance { get; private set; }

    private GameObject _currentRoot;
    private Transform _currentModelRoot;
    private Transform _currentWorkspace;

    [Header("Placement")]
    [Tooltip("The workspace bounds the model should appear inside. Assign the workspace object in your scene if not using runtime placement.")]
    [SerializeField] private Transform workspaceTransform;

    [Header("Wireframe Override")]
    [Tooltip("If assigned, all loaded meshes will be rendered with this wireframe effect instead of their original materials (expects an Azerilo wireframe material).")]
    public Material overrideMaterial;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Fire-and-forget wrapper so you can call from UI without dealing with Tasks.
    /// </summary>
    public async void LoadFromPath(string path)
    {
        await LoadFromPathAsync(path);
    }

    /// <summary>
    /// Main entry point: load a model from a local file path.
    /// </summary>
    public async Task LoadFromPathAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Debug.LogWarning("[RuntimeModelLoader] LoadFromPathAsync called with empty path.");
            return;
        }

        await LoadFromFileAsync(path);
    }

    /// <summary>
    /// Unloads the currently instantiated runtime model, if any.
    /// </summary>
    public void UnloadCurrentModel()
    {
        if (_currentRoot != null)
        {
            Destroy(_currentRoot);
            _currentRoot = null;
        }
    }

    private async Task LoadFromFileAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("[RuntimeModelLoader] LoadFromFileAsync called with null/empty path.");
            return;
        }

        if (!File.Exists(path))
        {
            Debug.LogError($"[RuntimeModelLoader] File does not exist: {path}");
            return;
        }

        UnloadCurrentModel();

        var gltf = new GltfImport();

        // glTFast expects a URI; prefix local files with file://
        string uri = new Uri(path).AbsoluteUri;

        bool loaded = await gltf.Load(uri);
        if (!loaded)
        {
            Debug.LogError($"[RuntimeModelLoader] Failed to load glTF from '{uri}'.");
            return;
        }

        var root = new GameObject("RuntimeModel");

        bool instantiated = await gltf.InstantiateMainSceneAsync(root.transform);
        if (!instantiated)
        {
            Debug.LogError("[RuntimeModelLoader] Failed to instantiate main scene from glTF.");
            Destroy(root);
            return;
        }

        // Attach to calibration origin so model lives in the calibrated world space.
        CalibrationOriginUtility.AttachToOrigin(root.transform, worldPositionStays: true);

        // Place the model inside the workspace bounds (if one exists) using the offset from Settings.
        // If we have no valid reference workspace, do NOT keep the model loaded.
        _currentModelRoot = root.transform;
        if (!PositionModelInsideWorkspace(root.transform))
        {
            Debug.LogWarning("[RuntimeModelLoader] No reference point for model to load.");
            Destroy(root);
            return;
        }

        // Optionally override visuals so we ignore original textures and use a wireframe effect.
        if (overrideMaterial != null)
        {
            RuntimeModelVisualsUtility.ApplyWireframeEffect(root.transform, overrideMaterial);
        }

        _currentRoot = root;

        Debug.Log($"[RuntimeModelLoader] Loaded model from '{path}', positioned inside workspace, and instantiated under calibration origin.");
    }

    private void LateUpdate()
    {
        // Continuously keep the model aligned with the workspace and offset/scale
        // from Settings so changes take effect in real time, even after placement.
        if (_currentModelRoot != null && _currentWorkspace != null)
        {
            var settings = SettingsManager.Instance != null ? SettingsManager.Instance.settings : null;
            RuntimeModelPositionUtility.RepositionModelRelativeToWorkspace(_currentModelRoot, _currentWorkspace, settings);
        }
    }

    /// <summary>
    /// Positions the loaded model so it sits inside the workspace bounds.
    /// Delegates the actual math to RuntimeModelPositionUtility.
    /// </summary>
    private bool PositionModelInsideWorkspace(Transform modelRoot)
    {
        if (modelRoot == null)
            return false;

        var settings = SettingsManager.Instance != null ? SettingsManager.Instance.settings : null;
        if (settings == null)
            return false;

        if (!RuntimeModelPositionUtility.TryPositionModelInsideWorkspace(modelRoot, workspaceTransform, out var resolvedWorkspace, settings))
            return false;

        _currentWorkspace = resolvedWorkspace;
        return true;
    }
}
