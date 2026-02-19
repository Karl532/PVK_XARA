using UnityEngine;
using Assets.Scripts.Debug;

public partial class SdfGenerator
{
    // ===== AABB buffers =====
    // Each AABB is 2 float3s: min, max
    private ComputeBuffer _aabbGroups;
    private ComputeBuffer _aabbScratch;
    private Vector3[] _aabbReadback;

    // ===== Point AABB reduction =====
    private (Vector3 min, Vector3 max) ComputePointAabb(ComputeBuffer pointBuffer, int pointCount)
    {
        int kernelInit = _csPointAabb.FindKernel("CSInit");
        int kernelReduce = _csPointAabb.FindKernel("CSReduce");

        // First pass outputs one AABB per threadgroup into _aabbGroups
        int threads = 256;
        int groupCount = Mathf.CeilToInt(pointCount / (float)threads);

        EnsureAabbBuffers(groupCount);

        _csPointAabb.SetInt("_PointCount", pointCount);
        _csPointAabb.SetBuffer(kernelInit, "_Points", pointBuffer);
        _csPointAabb.SetBuffer(kernelInit, "_GroupAabbs", _aabbGroups);
        _csPointAabb.Dispatch(kernelInit, groupCount, 1, 1);

        // Reduce group AABBs down to one
        int currentCount = groupCount;
        while (currentCount > 1)
        {
            int outCount = Mathf.CeilToInt(currentCount / (float)threads);
            EnsureAabbScratch(outCount);

            _csPointAabb.SetInt("_AabbCount", currentCount);
            _csPointAabb.SetBuffer(kernelReduce, "_GroupAabbs", _aabbGroups);
            _csPointAabb.SetBuffer(kernelReduce, "_OutAabbs", _aabbScratch);
            _csPointAabb.Dispatch(kernelReduce, outCount, 1, 1);

            // swap
            (_aabbGroups, _aabbScratch) = (_aabbScratch, _aabbGroups);
            currentCount = outCount;
        }

        // Read back the final AABB (2 float3s)
        _aabbReadback ??= new Vector3[2];
        _aabbGroups.GetData(_aabbReadback, 0, 0, 2);

        DebugService.Log($"[SdfGenerator] AABB min={_aabbReadback[0]} max={_aabbReadback[1]} groups={groupCount}");

        return (_aabbReadback[0], _aabbReadback[1]);
    }

    private void EnsureAabbBuffers(int groupCount)
    {
        // Needs at least groupCount entries; each entry stores min+max => 2 Vector3
        int entries = groupCount * 2;
        if (_aabbGroups == null || _aabbGroups.count < entries)
        {
            _aabbGroups?.Release();
            _aabbGroups = new ComputeBuffer(entries, sizeof(float) * 3, ComputeBufferType.Structured);
        }
    }

    private void EnsureAabbScratch(int outCount)
    {
        int entries = outCount * 2;
        if (_aabbScratch == null || _aabbScratch.count < entries)
        {
            _aabbScratch?.Release();
            _aabbScratch = new ComputeBuffer(entries, sizeof(float) * 3, ComputeBufferType.Structured);
        }
    }
}




