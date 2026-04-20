using UnityEngine;
using UnityEngine.Rendering;

public partial class SdfGenerationSystem
{
    private void OnCoreBuildStarted(bool isGlobal)
    {
        if (isGlobal)
            _suppressSculptGuide = true;
    }

    private void OnCoreBuildCompleted(bool isGlobal)
    {
        if (isGlobal)
        {
            _suppressSculptGuide = false;
            CacheVolume(Global, ref _cachedGlobalTsdf, ref _cachedGlobal);
            EmitVisualizationDataIfReady();
        }
        else
        {
            CacheVolume(Local, ref _cachedLocalTsdf, ref _cachedLocal);
            EmitVisualizationDataIfReady();
        }
    }

    public bool SuppressSculptGuide => _suppressSculptGuide;

    private void CacheVolume(SdfVolumeData source, ref RenderTexture cacheTex, ref SdfVolumeData cacheData)
    {
        if (!source.IsValid)
            return;

        EnsureCacheTexture(source, ref cacheTex);
        _isCaching = true;
        Graphics.CopyTexture(source.Tsdf, cacheTex);
        _isCaching = false;
        cacheData = new SdfVolumeData(cacheTex, source.Corner, source.Size, source.Resolution, source.Mu);
    }

    private static void EnsureCacheTexture(SdfVolumeData source, ref RenderTexture cacheTex)
    {
        if (cacheTex != null &&
            cacheTex.IsCreated() &&
            cacheTex.width == source.Resolution &&
            cacheTex.height == source.Resolution &&
            cacheTex.volumeDepth == source.Resolution)
        {
            return;
        }

        if (cacheTex != null)
            cacheTex.Release();

        var desc = new RenderTextureDescriptor(source.Resolution, source.Resolution, RenderTextureFormat.RHalf, 0)
        {
            dimension = TextureDimension.Tex3D,
            volumeDepth = source.Resolution,
            enableRandomWrite = false
        };
        cacheTex = new RenderTexture(desc)
        {
            name = "SdfTsdfCache",
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        cacheTex.Create();
    }

    private static void ReleaseCachedVolume(ref RenderTexture cacheTex, ref SdfVolumeData cacheData)
    {
        if (cacheTex != null)
        {
            cacheTex.Release();
            cacheTex = null;
        }
        cacheData = default;
    }

    private void EmitVisualizationDataIfReady()
    {
        if (_isCaching)
            return;

        var global = _cachedGlobal;
        var local = _cachedLocal;
        if (!global.IsValid)
            return;

        var data = new SdfVisualizationData(
            global,
            local,
            _workspace.Root,
            Vector3.one / -2.0f/*_workspace.Corner*/,
            Vector3.one,
            //_workspace.Size,
            _lastFilteredPoints,
            _lastFilteredCount);
        VisualizationDataUpdated?.Invoke(data);
    }
}
