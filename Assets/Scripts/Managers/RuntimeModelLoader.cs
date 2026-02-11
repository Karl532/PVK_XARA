using System;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Loads glTF/glb models from a local file path at runtime and
/// instantiates them under the calibration origin.
/// </summary>
public class RuntimeModelLoader : MonoBehaviour
{
    public static RuntimeModelLoader Instance { get; private set; }

    private GameObject _currentRoot;

    [Header("Placement")]
    [Tooltip("The stone block the model should appear inside. Assign the final block object in your scene (e.g. the block at the bottom).")]
    [SerializeField] private Transform blockTransform;

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

        // Place the model inside the stone block (if one is assigned) using the offset from Settings.
        // If we have no valid reference block, do NOT keep the model loaded.
        if (!PositionModelInsideBlock(root.transform))
        {
            Debug.LogWarning("[RuntimeModelLoader] No reference point for model to load.");
            Destroy(root);
            return;
        }

        // Optionally override visuals so we ignore original textures and use a wireframe effect.
        if (overrideMaterial != null)
        {
            ApplyWireframeEffect(root.transform, overrideMaterial);
        }

        _currentRoot = root;

        Debug.Log($"[RuntimeModelLoader] Loaded model from '{path}', positioned inside block, and instantiated under calibration origin.");
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

        // Configure the wireframe material so it draws clearly on top of the block
        // and other opaque geometry (bright and not occluded).
        ConfigureWireframeMaterial(wireframeMat);

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

    /// <summary>
    /// Positions the loaded model so it sits inside the stone block at the bottom.
    /// The offset in Settings.modelOffset controls the position within the block:
    /// - X/Z: offset from the block's center in local block space
    /// - Y:   offset up from the bottom of the block
    /// </summary>
    private bool PositionModelInsideBlock(Transform modelRoot)
    {
        if (modelRoot == null)
            return false;

        // Resolve the block transform:
        // 1) Prefer the explicitly assigned reference (if any).
        // 2) Otherwise, try to find the runtime-created placement block by name ("PlacementBlock").
        Transform block = blockTransform;
        if (block == null)
        {
            GameObject blockObj = GameObject.Find("PlacementBlock");
            if (blockObj != null)
            {
                block = blockObj.transform;
                Debug.Log("[RuntimeModelLoader] Using runtime-created 'PlacementBlock' as blockTransform.");
            }
        }

        if (block == null)
        {
            Debug.LogWarning("[RuntimeModelLoader] No blockTransform assigned and no 'PlacementBlock' found.");
            return false;
        }

        var settings = SettingsManager.Instance != null ? SettingsManager.Instance.settings : null;
        Vector3 offset = settings != null ? settings.modelOffset : Vector3.zero;

        // Compute world-space bounds of the block.
        var blockRenderers = block.GetComponentsInChildren<Renderer>(includeInactive: true);
        if (blockRenderers.Length == 0)
        {
            Debug.LogWarning("[RuntimeModelLoader] blockTransform has no renderers. Cannot compute block bounds.");
            return false;
        }

        Bounds blockBounds = blockRenderers[0].bounds;
        for (int i = 1; i < blockRenderers.Length; i++)
        {
            blockBounds.Encapsulate(blockRenderers[i].bounds);
        }

        // Compute world-space bounds of the model.
        var modelRenderers = modelRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
        if (modelRenderers.Length == 0)
        {
            Debug.LogWarning("[RuntimeModelLoader] Loaded model has no renderers. Cannot compute model bounds.");
            return false;
        }

        Bounds modelBounds = modelRenderers[0].bounds;
        for (int i = 1; i < modelRenderers.Length; i++)
        {
            modelBounds.Encapsulate(modelRenderers[i].bounds);
        }

        Vector3 blockCenter = blockBounds.center;
        float blockBottomY = blockBounds.min.y;

        Vector3 modelCenter = modelBounds.center;
        Vector3 modelExtents = modelBounds.extents;

        // Interpret offset:
        // - X/Z: offset from block center in local block space.
        // - Y:   extra height above the block bottom.
        Vector3 offsetXZWorld =
            block.right * offset.x +
            block.forward * offset.z;

        float targetCenterY = blockBottomY + offset.y + modelExtents.y;
        Vector3 targetCenterWorld = new Vector3(blockCenter.x, targetCenterY, blockCenter.z) + offsetXZWorld;

        Vector3 delta = targetCenterWorld - modelCenter;
        modelRoot.position += delta;

        // Parent the model under the block so it continues to follow any
        // movements/rotations of the block after placement.
        modelRoot.SetParent(block, worldPositionStays: true);

        Debug.Log($"[RuntimeModelLoader] Positioned model inside block. BlockBounds={blockBounds.size}, ModelBounds={modelBounds.size}, Offset={offset}.");
        return true;
    }

    /// <summary>
    /// Tweaks the Azerilo wireframe material so it is always visible and slightly glowing,
    /// even when inside or behind other geometry like the placement block.
    /// </summary>
    private void ConfigureWireframeMaterial(Material mat)
    {
        if (mat == null)
            return;

        // Render very late so it appears on top of most things.
        mat.renderQueue = (int)RenderQueue.Overlay;

        // If the shader supports depth properties, disable depth writes and relax depth testing.
        if (mat.HasProperty("_ZWrite"))
        {
            mat.SetInt("_ZWrite", 0);
        }
        if (mat.HasProperty("_ZTest"))
        {
            mat.SetInt("_ZTest", (int)CompareFunction.Always);
        }

        // Azerilo shader exposes _ZMode; -1 usually corresponds to a more permissive depth mode.
        if (mat.HasProperty("_ZMode"))
        {
            mat.SetFloat("_ZMode", -1f);
        }

        // Make the wireframe color fully opaque and slightly brighter for readability.
        if (mat.HasProperty("_WireColor"))
        {
            var c = mat.GetColor("_WireColor");
            c.a = 1f;
            c *= 1.3f;
            mat.SetColor("_WireColor", c);
        }
        else if (mat.HasProperty("_Color"))
        {
            var c = mat.GetColor("_Color");
            c.a = 1f;
            c *= 1.3f;
            mat.SetColor("_Color", c);
        }
    }
}
