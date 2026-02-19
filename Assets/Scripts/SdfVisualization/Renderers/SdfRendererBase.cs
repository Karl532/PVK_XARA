using UnityEngine;
using UnityEngine.Rendering;

public abstract class SdfRendererBase : MonoBehaviour, ISdfRenderer
{
    public abstract void UpdateRenderer(in SdfRendererContext context);

    private bool _hooked;

    protected Settings Settings { get; private set; }
    protected SdfVisualizationConfig Config { get; private set; }

    protected virtual void OnEnable()
    {
        TryResolveSettings();
        HookRenderPipeline();
    }

    protected virtual void OnDisable()
    {
        UnhookRenderPipeline();
    }

    protected abstract void OnRenderCamera(ScriptableRenderContext context, Camera camera);

    protected bool TryResolveSettings()
    {
        if (Settings == null)
            Settings = Settings.GetActive();
        if (Config == null)
        {
            if (Settings != null && Settings.sdfVisualizationConfig != null)
                Config = Settings.sdfVisualizationConfig;
            else
                Config = SdfVisualizationConfig.GetActive();
        }
        return Settings != null && Config != null;
    }

    private void HookRenderPipeline()
    {
        if (_hooked)
            return;
        RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
        _hooked = true;
    }

    private void UnhookRenderPipeline()
    {
        if (!_hooked)
            return;
        RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
        _hooked = false;
    }

    private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        OnRenderCamera(context, camera);
    }
}
