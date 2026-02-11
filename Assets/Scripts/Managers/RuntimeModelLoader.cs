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

        // Optionally override visuals so we ignore original textures and use a wireframe effect.
        if (overrideMaterial != null)
        {
            ApplyWireframeEffect(root.transform, overrideMaterial);
        }

        _currentRoot = root;

        Debug.Log($"[RuntimeModelLoader] Loaded model from '{path}' and instantiated under calibration origin.");
    }

    /// <summary>
    /// Applies the Azerilo wireframe effect by adding a WireframeShader component
    /// to each renderer in the loaded model hierarchy and disabling the original renderer,
    /// so only the wireframe is visible.
    /// </summary>
    private void ApplyWireframeEffect(Transform root, Material wireframeMat)
    {
        if (root == null || wireframeMat == null)
        {
            Debug.LogWarning("[RuntimeModelLoader] ApplyWireframeEffect called with null root or material.");
            return;
        }

        var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
        int wiredCount = 0;

        foreach (var renderer in renderers)
        {
            if (renderer == null)
                continue;

            // WireframeShader expects a MeshFilter or SkinnedMeshRenderer on the same GameObject.
            var go = renderer.gameObject;

            // Avoid duplicates if something already added one.
            var existing = go.GetComponent<WireframeShader>();
            if (existing == null)
            {
                var wf = go.AddComponent<WireframeShader>();
                wf.wireframeMaterial = wireframeMat;
            }

            // Hide the original shaded mesh so we don't see textures underneath.
            renderer.enabled = false;
            wiredCount++;
        }

        Debug.Log($"[RuntimeModelLoader] Applied wireframe effect '{wireframeMat.name}' to {wiredCount} renderers.");
    }
}
