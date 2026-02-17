using UnityEngine;

[DisallowMultipleComponent]
public class SdfOverlayApi : MonoBehaviour
{
    private SdfDepthOrchestrator _orchestrator;

    private void Awake()
    {
        _orchestrator = GetComponent<SdfDepthOrchestrator>();
        if (_orchestrator == null)
            Debug.LogWarning("[SdfOverlayApi] Missing SdfDepthOrchestrator on same GameObject.");
    }

    public bool IsFullSdfDebugEnabled()
    {
        return _orchestrator != null && _orchestrator.OverlayFullSdfDebugEnabled;
    }

    public bool IsSculptGuideEnabled()
    {
        return _orchestrator != null && _orchestrator.SculptGuideEnabled;
    }

    public void SetSculptGuideEnabled(bool enabled)
    {
        if (_orchestrator == null)
            return;
        _orchestrator.SetSculptGuideEnabled(enabled);
    }

    public void SetFullSdfDebug(bool enabled)
    {
        if (_orchestrator == null)
            return;
        _orchestrator.SetOverlayFullSdfDebug(enabled);
    }
}
