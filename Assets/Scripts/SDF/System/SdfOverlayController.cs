using UnityEngine;

public sealed class SdfOverlayController
{
    private SdfOverlayRenderer _renderer;
    private Camera _camera;

    public RenderTexture OverlayRT => _renderer != null ? _renderer.OverlayRT : null;

    public void Ensure(ComputeShader overlayCS, Camera camera, GameObject host)
    {
        if (!overlayCS || host == null)
            return;

        if (_renderer == null)
            _renderer = host.AddComponent<SdfOverlayRenderer>();

        _camera = camera ? camera : Camera.main;
        _renderer.SetResources(overlayCS, _camera);
    }

    public void Render(
        ComputeBuffer worldPoints,
        int pointCount,
        Matrix4x4 worldToWorkspace,
        SdfVolumeData local,
        SdfVolumeData global)
    {
        if (_renderer == null || worldPoints == null || pointCount <= 0 || !global.IsValid)
            return;

        _renderer.RenderOverlay(
            worldPoints,
            pointCount,
            worldToWorkspace,

            local.IsValid ? local.Tsdf : null,
            local.Corner, local.Size, local.Mu,

            global.Tsdf,
            global.Corner, global.Size, global.Mu
        );
    }
}
