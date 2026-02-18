using UnityEngine;

public sealed class SdfSculptGuideDepthMeshRenderer
{
    private ComputeShader _cs;
    private int _kSmooth;
    private bool _ready;

    private RenderTexture _history;
    private RenderTexture _smoothed;
    private Vector2Int _lastResolution;
    private Quaternion _lastCameraRotation = Quaternion.identity;
    private Vector3 _lastCameraPosition = Vector3.zero;
    private bool _hasLastCamera;

    private static readonly int ID_DepthTex = Shader.PropertyToID("_DepthTex");
    private static readonly int ID_DepthHistory = Shader.PropertyToID("_DepthHistory");
    private static readonly int ID_DepthSmooth = Shader.PropertyToID("_DepthSmooth");
    private static readonly int ID_DepthSize = Shader.PropertyToID("_DepthSize");
    private static readonly int ID_EyeSlice = Shader.PropertyToID("_EyeSlice");
    private static readonly int ID_FlipY = Shader.PropertyToID("_FlipY");
    private static readonly int ID_SmoothAlpha = Shader.PropertyToID("_SmoothAlpha");

    private static readonly int ID_DepthSmoothTex = Shader.PropertyToID("_DepthSmooth");
    private static readonly int ID_MeshStep = Shader.PropertyToID("_MeshStep");
    private static readonly int ID_InvDepthViewProj = Shader.PropertyToID("_InvDepthViewProj");
    private static readonly int ID_TrackingToWorld = Shader.PropertyToID("_TrackingToWorld");

    public void Reset()
    {
        _lastResolution = Vector2Int.zero;
        _hasLastCamera = false;
    }

    public void Release()
    {
        if (_history != null)
        {
            _history.Release();
            _history = null;
        }
        if (_smoothed != null)
        {
            _smoothed.Release();
            _smoothed = null;
        }
        _lastResolution = Vector2Int.zero;
        _ready = false;
        _hasLastCamera = false;
    }

    public void Render(
        Camera camera,
        DepthFrameData depthFrame,
        SculptGuideSettings settings,
        SdfVolumeData global,
        Material meshMaterial,
        Matrix4x4 worldToWorkspace)
    {
        if (camera == null || depthFrame.DepthTexture == null || !global.IsValid || meshMaterial == null)
            return;

        EnsureResources();
        if (!_ready)
            return;

        EnsureTextures(depthFrame.DepthResolution);
        SmoothDepth(depthFrame, settings, camera);

        RenderLiveMesh(camera, depthFrame, settings, global, meshMaterial, worldToWorkspace);
    }

    private void EnsureResources()
    {
        if (_ready)
            return;

        _cs = Resources.Load<ComputeShader>("SDF/Compute/Visualization/SdfSculptGuideDepthMesh");
        if (_cs == null)
        {
            Debug.LogWarning("[SdfSculptGuideDepthMeshRenderer] Missing compute shader: SDF/Compute/Visualization/SdfSculptGuideDepthMesh");
            return;
        }

        _kSmooth = _cs.FindKernel("CSSmoothDepth");
        if (_kSmooth < 0)
        {
            Debug.LogWarning("[SdfSculptGuideDepthMeshRenderer] Missing kernel CSSmoothDepth.");
            return;
        }

        _ready = true;
    }

    private void EnsureTextures(Vector2Int resolution)
    {
        if (resolution.x <= 0 || resolution.y <= 0)
            return;

        if (_history != null && _smoothed != null && _lastResolution == resolution)
            return;

        ReleaseTextures();
        _history = CreateDepthTex(resolution, "SdfSculptGuideDepthHistory");
        _smoothed = CreateDepthTex(resolution, "SdfSculptGuideDepthSmoothed");
        _lastResolution = resolution;
    }

    private void SmoothDepth(DepthFrameData depthFrame, SculptGuideSettings settings, Camera camera)
    {
        if (_history == null || _smoothed == null)
            return;

        float alpha = Mathf.Clamp01(settings.MeshSmoothingAlpha);
        if (_hasLastCamera)
        {
            float angle = Quaternion.Angle(_lastCameraRotation, camera.transform.rotation);
            float posDelta = Vector3.Distance(_lastCameraPosition, camera.transform.position);
            if (angle > 1.5f || posDelta > 0.05f)
                alpha = 0f;
        }

        _cs.SetTexture(_kSmooth, ID_DepthTex, depthFrame.DepthTexture);
        _cs.SetTexture(_kSmooth, ID_DepthHistory, _history);
        _cs.SetTexture(_kSmooth, ID_DepthSmooth, _smoothed);
        _cs.SetInts(ID_DepthSize, depthFrame.DepthResolution.x, depthFrame.DepthResolution.y);
        _cs.SetInt(ID_EyeSlice, depthFrame.EyeSlice);
        _cs.SetInt(ID_FlipY, depthFrame.FlipY ? 1 : 0);
        _cs.SetFloat(ID_SmoothAlpha, alpha);

        int gx = Mathf.CeilToInt(depthFrame.DepthResolution.x / 8f);
        int gy = Mathf.CeilToInt(depthFrame.DepthResolution.y / 8f);
        _cs.Dispatch(_kSmooth, gx, gy, 1);

        SwapDepthTextures();

        _lastCameraRotation = camera.transform.rotation;
        _lastCameraPosition = camera.transform.position;
        _hasLastCamera = true;
    }

    private void RenderLiveMesh(
        Camera camera,
        DepthFrameData depthFrame,
        SculptGuideSettings settings,
        SdfVolumeData global,
        Material meshMaterial,
        Matrix4x4 worldToWorkspace)
    {
        // After SmoothDepth(), _history holds the latest smoothed texture (swap applied).
        meshMaterial.SetTexture(ID_DepthSmoothTex, _history);
        meshMaterial.SetInt(ID_MeshStep, Mathf.Max(1, settings.MeshStep));
        meshMaterial.SetMatrix(ID_InvDepthViewProj, depthFrame.InvDepthViewProj);
        meshMaterial.SetMatrix(ID_TrackingToWorld, depthFrame.TrackingToWorld);
        meshMaterial.SetMatrix(SdfSculptGuideShaderIds.WorldToWorkspace, worldToWorkspace);
        meshMaterial.SetVector(ID_DepthSize, new Vector4(depthFrame.DepthResolution.x, depthFrame.DepthResolution.y, 0f, 0f));
        meshMaterial.SetFloat(SdfSculptGuideShaderIds.Alpha, settings.Alpha);
        meshMaterial.SetTexture(SdfSculptGuideShaderIds.GlobalTsdf3D, global.Tsdf);
        meshMaterial.SetVector(SdfSculptGuideShaderIds.GlobalCorner, global.Corner);
        meshMaterial.SetVector(SdfSculptGuideShaderIds.GlobalSize, global.Size);
        meshMaterial.SetFloat(SdfSculptGuideShaderIds.GlobalMu, global.Mu);

        int step = Mathf.Max(1, settings.MeshStep);
        int gridW = Mathf.Max(2, depthFrame.DepthResolution.x / step);
        int gridH = Mathf.Max(2, depthFrame.DepthResolution.y / step);
        int cellCount = (gridW - 1) * (gridH - 1);
        int vertexCount = cellCount * 6;
        SdfDebug.LogEvery(
            "SdfSculptGuideDepthMeshRenderer.Render",
            $"[SG_DEBUG] [SdfSculptGuideDepthMeshRenderer] Render res={depthFrame.DepthResolution} step={step} verts={vertexCount} alpha={settings.MeshSmoothingAlpha}",
            1f);
        if (vertexCount <= 0)
            return;

        Bounds bounds = new Bounds(camera.transform.position, Vector3.one * 2000f);
        Graphics.DrawProcedural(
            meshMaterial,
            bounds,
            MeshTopology.Triangles,
            vertexCount,
            1,
            camera);
    }


    private void SwapDepthTextures()
    {
        var temp = _history;
        _history = _smoothed;
        _smoothed = temp;
    }

    private void ReleaseTextures()
    {
        if (_history != null)
        {
            _history.Release();
            _history = null;
        }
        if (_smoothed != null)
        {
            _smoothed.Release();
            _smoothed = null;
        }
        _lastResolution = Vector2Int.zero;
    }

    private static RenderTexture CreateDepthTex(Vector2Int resolution, string name)
    {
        var desc = new RenderTextureDescriptor(resolution.x, resolution.y, RenderTextureFormat.RFloat, 0)
        {
            enableRandomWrite = true,
            msaaSamples = 1
        };
        var rt = new RenderTexture(desc)
        {
            name = name,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        rt.Create();
        return rt;
    }
}
