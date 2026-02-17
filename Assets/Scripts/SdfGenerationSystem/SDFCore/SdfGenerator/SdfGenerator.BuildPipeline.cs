using UnityEngine;

public partial class SdfGenerator
{
    private void DispatchJumpFloodStep(VolumeInternals v, int kernel, int groups, int res, int step, ref bool ping)
    {
        _csJumpFlood.SetInt("_Resolution", res);
        _csJumpFlood.SetInt("_Step", step);
        _csJumpFlood.SetVector("_Corner", v.Corner);
        _csJumpFlood.SetVector("_Size", v.Size);

        if (ping)
        {
            _csJumpFlood.SetTexture(kernel, "_SeedIn", v.SeedA);
            _csJumpFlood.SetTexture(kernel, "_SeedOut", v.SeedB);
        }
        else
        {
            _csJumpFlood.SetTexture(kernel, "_SeedIn", v.SeedB);
            _csJumpFlood.SetTexture(kernel, "_SeedOut", v.SeedA);
        }

        _csJumpFlood.Dispatch(kernel, groups, groups, groups);
        ping = !ping;
    }

    // ===== Spread build over frames =====
    private enum BuildStage
    {
        Clear,
        Voxelize,
        JumpFlood,
        Finalize,
        Done
    }

    private struct BuildJob
    {
        public VolumeInternals Volume;
        public bool IsGlobal;
        public BuildStage Stage;
        public int JumpStep;
        public bool Ping;
    }

    private BuildJob? _activeJob;
    public bool IsBuilding => _activeJob.HasValue;
    public event System.Action<bool> BuildCompleted;
    public event System.Action<bool> BuildStarted;

    private void EnqueueBuildJob(VolumeInternals v, bool isGlobal)
    {
        if (_activeJob.HasValue)
            return; // one job at a time

        int startStep = HighestPowerOfTwoAtMost(v.Resolution / 2);
        _activeJob = new BuildJob
        {
            Volume = v,
            IsGlobal = isGlobal,
            Stage = BuildStage.Clear,
            JumpStep = startStep,
            Ping = true
        };
        BuildStarted?.Invoke(isGlobal);
    }

    public void TickBuild()
    {
        int stages = Mathf.Max(1, MaxStagesPerFrame);
        for (int i = 0; i < stages; i++)
        {
            if (!_activeJob.HasValue)
                return;

            var job = _activeJob.Value;
            switch (job.Stage)
            {
                case BuildStage.Clear:
                    DispatchClear(job.Volume);
                    job.Stage = BuildStage.Voxelize;
                    break;
                case BuildStage.Voxelize:
                    DispatchVoxelizeSeeds(job.Volume);
                    job.Stage = BuildStage.JumpFlood;
                    break;
                case BuildStage.JumpFlood:
                    {
                        int k = _csJumpFlood.FindKernel("CSJumpFlood");
                        int res = job.Volume.Resolution;
                        int g = Mathf.CeilToInt(res / 8f);
                        DispatchJumpFloodStep(job.Volume, k, g, res, job.JumpStep, ref job.Ping);
                        job.JumpStep /= 2;
                        if (job.JumpStep < 1)
                        {
                            // ensure final seeds end up in SeedA for finalize
                            if (!job.Ping)
                            {
                                (job.Volume.SeedA, job.Volume.SeedB) = (job.Volume.SeedB, job.Volume.SeedA);
                            }
                            job.Stage = BuildStage.Finalize;
                        }
                    }
                    break;
                case BuildStage.Finalize:
                    DispatchFinalize(job.Volume);
                    job.Stage = BuildStage.Done;
                    break;
                case BuildStage.Done:
                    bool isGlobal = job.IsGlobal;
                    _activeJob = null;
                    BuildCompleted?.Invoke(isGlobal);
                    return;
            }

            _activeJob = job;
        }
    }
}
