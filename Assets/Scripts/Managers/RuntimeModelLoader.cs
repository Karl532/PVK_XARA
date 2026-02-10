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

        _currentRoot = root;

        Debug.Log($"[RuntimeModelLoader] Loaded model from '{path}' and instantiated under calibration origin.");
    }
}
