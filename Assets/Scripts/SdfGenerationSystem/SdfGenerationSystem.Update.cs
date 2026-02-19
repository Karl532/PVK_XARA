using UnityEngine;
using Assets.Scripts.Debug;

public partial class SdfGenerationSystem
{
    /// <summary>
    /// Main entry point each depth frame:
    /// Feed world-space point cloud buffer in, update SDF (local/global).
    /// </summary>
    public void UpdateWithWorldPoints(ComputeBuffer worldPointsFloat4, int pointCount)
    {
        Matrix4x4 worldToWorkspace = _workspace.Root != null ? _workspace.Root.worldToLocalMatrix : Matrix4x4.identity;
        UpdateWithPoints(worldPointsFloat4, pointCount, Matrix4x4.identity, worldToWorkspace);
    }

    public void UpdateWithPoints(ComputeBuffer pointsFloat4, int pointCount, Matrix4x4 inputToWorld, Matrix4x4 inputToWorkspace)
    {
        InitializeIfNeeded();
        if (!_initialized) return;

        DebugService.Log(
            $"[SdfGenerationSystem] UpdateWithPoints enter: pointsNull={pointsFloat4 == null} pointCount={pointCount} workspaceValid={_workspace.IsValid} modelInit={_model.IsInitialized}"
        );
        if (_workspace.Root != null)
        {
            var t = _workspace.Root;
            DebugService.Log(
                $"[SdfGenerationSystem] Workspace transform: pos={t.position} rot={t.rotation.eulerAngles} scale={t.localScale}"
            );
        }

        TryInitializeModel();
        if (!_model.IsInitialized)
        {
            DebugService.Log("[SdfGenerationSystem] Model not initialized yet.");
            return;
        }

        if (!_workspace.IsValid)
        {
            DebugService.LogVerbose("[SdfGenerationSystem] workspaceRoot is null. Call SetWorkspace(...) first.");
            return;
        }

        if (pointsFloat4 == null || pointCount <= 0)
        {
            DebugService.Log("[SdfGenerationSystem] No points received this frame.");
            return;
        }

        DebugService.Log(
            $"[SdfGenerationSystem] inputToWorkspace m00={inputToWorkspace.m00:F3} m01={inputToWorkspace.m01:F3} m02={inputToWorkspace.m02:F3} m03={inputToWorkspace.m03:F3} " +
            $"m10={inputToWorkspace.m10:F3} m11={inputToWorkspace.m11:F3} m12={inputToWorkspace.m12:F3} m13={inputToWorkspace.m13:F3} " +
            $"m20={inputToWorkspace.m20:F3} m21={inputToWorkspace.m21:F3} m22={inputToWorkspace.m22:F3} m23={inputToWorkspace.m23:F3}"
        );

        _lastInputToWorld = inputToWorld;
        _lastInputToWorkspace = inputToWorkspace;

        ComputeBuffer pointsWS = _pointConverter.ConvertAndFilter(
            pointsFloat4,
            pointCount,
            inputToWorkspace,
            _workspace.Corner,
            _workspace.Size,
            out int filteredCount);

        DebugService.Log($"[SdfGenerationSystem] Filter result: pointsWSNull={pointsWS == null} filteredCount={filteredCount}");
        DebugService.Log($"[SdfGenerationSystem] Points in={pointCount} filtered={filteredCount} workspaceSize={_workspace.Size}");

        if (filteredCount <= 0)
        {
            _lastFilteredPoints = null;
            _lastFilteredCount = 0;
            _core.Update(null, 0, _workspace.Corner, _workspace.Size);
            return;
        }

        _lastFilteredPoints = pointsWS;
        _lastFilteredCount = filteredCount;

        DebugService.Log($"[SdfGenerationSystem] Cached filtered: lastPointsNull={_lastFilteredPoints == null} lastCount={_lastFilteredCount}");

        _core.Update(pointsWS, filteredCount, _workspace.Corner, _workspace.Size);

        var g = _core.Global;
        var l = _core.Local;
        DebugService.Log(
            $"[SdfGenerationSystem] Global TSDF valid={g.IsValid} res={g.Resolution} size={g.Size} mu={g.Mu} corner={g.Corner}"
        );
        DebugService.Log(
            $"[SdfGenerationSystem] Local TSDF valid={l.IsValid} res={l.Resolution} size={l.Size} mu={l.Mu} corner={l.Corner}"
        );

        EmitVisualizationDataIfReady();

    }
}





