using UnityEngine;

public partial class SdfGenerator
{
    // ===== Dispatch helpers =====
    private void DispatchClear(VolumeInternals v)
    {
        SdfDebug.Log($"[SdfGenerator] DispatchClear res={v.Resolution}");
        int k = _csClear.FindKernel("CSClear");
        _csClear.SetInt("_Resolution", v.Resolution);
        _csClear.SetTexture(k, "_SeedTex", v.SeedA);
        _csClear.SetTexture(k, "_TsdfTex", v.Tsdf);

        int g = Mathf.CeilToInt(v.Resolution / 8f);
        _csClear.Dispatch(k, g, g, g);
    }

    private void DispatchVoxelizeSeeds(VolumeInternals v)
    {
        SdfDebug.Log($"[SdfGenerator] DispatchVoxelizeSeeds tris={_triUploader.TriangleCount} corner={v.Corner} size={v.Size}");
        int k = _csVoxelizeSeeds.FindKernel("CSVoxelizeSeeds");

        _csVoxelizeSeeds.SetInt("_Resolution", v.Resolution);
        _csVoxelizeSeeds.SetVector("_Corner", v.Corner);
        _csVoxelizeSeeds.SetVector("_Size", v.Size);

        _csVoxelizeSeeds.SetInt("_TriangleCount", _triUploader.TriangleCount);
        _csVoxelizeSeeds.SetBuffer(k, "_Triangles", _triUploader.TriangleBuffer);

        _csVoxelizeSeeds.SetTexture(k, "_SeedTex", v.SeedA);

        // One thread per triangle
        int threads = 64;
        int groups = Mathf.CeilToInt((_triUploader.TriangleCount) / (float)threads);
        _csVoxelizeSeeds.Dispatch(k, groups, 1, 1);
    }

    private void DispatchJumpFlood(VolumeInternals v)
    {
        SdfDebug.Log($"[SdfGenerator] DispatchJumpFlood res={v.Resolution}");
        int k = _csJumpFlood.FindKernel("CSJumpFlood");

        int res = v.Resolution;
        int g = Mathf.CeilToInt(res / 8f);

        // Ping-pong between SeedA and SeedB
        bool ping = true;
        for (int step = HighestPowerOfTwoAtMost(res / 2); step >= 1; step /= 2)
        {
            DispatchJumpFloodStep(v, k, g, res, step, ref ping);
        }

        // Ensure final seeds end up in SeedA for finalize
        if (!ping)
        {
            // last write went to SeedB, so swap references
            (v.SeedA, v.SeedB) = (v.SeedB, v.SeedA);
        }
    }

    private void DispatchFinalize(VolumeInternals v)
    {
        SdfDebug.Log($"[SdfGenerator] DispatchFinalize res={v.Resolution} mu={v.Mu}");
        int k = _csFinalize.FindKernel("CSFinalize");
        int res = v.Resolution;
        int g = Mathf.CeilToInt(res / 8f);

        _csFinalize.SetInt("_Resolution", res);
        _csFinalize.SetFloat("_Mu", v.Mu);
        _csFinalize.SetVector("_Corner", v.Corner);
        _csFinalize.SetVector("_Size", v.Size);

        _csFinalize.SetTexture(k, "_SeedTex", v.SeedA);
        _csFinalize.SetTexture(k, "_TsdfTex", v.Tsdf);

        _csFinalize.Dispatch(k, g, g, g);
    }

    private static int HighestPowerOfTwoAtMost(int x)
    {
        int p = 1;
        while (p * 2 <= x) p *= 2;
        return p;
    }
}
