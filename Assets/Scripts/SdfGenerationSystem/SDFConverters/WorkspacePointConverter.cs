using System;
using UnityEngine;

    /// <summary>
    /// Converts a point cloud ComputeBuffer<float4> into WORKSPACE space on GPU.
    /// Input/output format: float4 per point, xyz = position, w = preserved.
    /// </summary>
    public sealed class WorkspacePointConverter : IDisposable
    {
        private readonly ComputeShader _cs;
        private readonly int _kernel;
        private readonly int _kernelFilter;

        private ComputeBuffer _outPoints; // float4
        private ComputeBuffer _filteredPoints; // append float4
        private ComputeBuffer _countBuffer; // uint
        private readonly uint[] _countReadback = new uint[1];
        private int _capacity;

        public ComputeBuffer OutputBuffer => _outPoints;

        public WorkspacePointConverter(ComputeShader pointsToWorkspaceCS)
        {
            _cs = pointsToWorkspaceCS ? pointsToWorkspaceCS : throw new ArgumentNullException(nameof(pointsToWorkspaceCS));
            _kernel = _cs.FindKernel("CSTransform");
            _kernelFilter = _cs.FindKernel("CSFilter");
        }

        /// <summary>
        /// Ensure output capacity (number of points).
        /// </summary>
        public void EnsureCapacity(int maxPoints)
        {
            if (maxPoints <= 0) maxPoints = 1;

            if (_outPoints != null && _capacity >= maxPoints)
                return;

            _outPoints?.Release();
            _filteredPoints?.Release();
            _countBuffer?.Release();
            _capacity = maxPoints;
            _outPoints = new ComputeBuffer(_capacity, sizeof(float) * 4, ComputeBufferType.Structured);
            _filteredPoints = new ComputeBuffer(_capacity, sizeof(float) * 4, ComputeBufferType.Append);
            _countBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);
            SdfDebug.LogVerbose($"[WorkspacePointConverter] Allocated buffers for {_capacity} points.");
        }

        /// <summary>
        /// Convert input points into workspace space using the provided matrix.
        /// </summary>
        /// <param name="inputPoints">ComputeBuffer of float4 points</param>
        /// <param name="pointCount">Valid points count</param>
        /// <param name="inputToWorkspace">Matrix mapping input space -> workspace space</param>
        /// <returns>Output buffer in workspace space</returns>
        public ComputeBuffer Convert(ComputeBuffer inputPoints, int pointCount, Matrix4x4 inputToWorkspace)
        {
            if (inputPoints == null) throw new ArgumentNullException(nameof(inputPoints));
            if (pointCount <= 0) throw new ArgumentOutOfRangeException(nameof(pointCount), "pointCount must be > 0");

            EnsureCapacity(pointCount);

            _cs.SetInt("_PointCount", pointCount);
            _cs.SetMatrix("_InputToWorkspace", inputToWorkspace);
            _cs.SetBuffer(_kernel, "_InPoints", inputPoints);
            _cs.SetBuffer(_kernel, "_OutPoints", _outPoints);

            // 256 threads per group
            int groups = Mathf.CeilToInt(pointCount / 256f);
            _cs.Dispatch(_kernel, groups, 1, 1);

            SdfDebug.LogEvery(
                "WorkspacePointConverter.Convert",
                $"[WorkspacePointConverter] Converted points={pointCount} groups={groups}",
                1f);

            return _outPoints;
        }

        /// <summary>
        /// Convert input points into workspace space and filter to workspace bounds.
        /// Returns the filtered buffer and outputs the filtered count.
        /// </summary>
        public ComputeBuffer ConvertAndFilter(
            ComputeBuffer inputPoints,
            int pointCount,
            Matrix4x4 inputToWorkspace,
            Vector3 workspaceCorner,
            Vector3 workspaceSize,
            out int filteredCount)
        {
            if (inputPoints == null) throw new ArgumentNullException(nameof(inputPoints));
            if (pointCount <= 0) throw new ArgumentOutOfRangeException(nameof(pointCount), "pointCount must be > 0");

            EnsureCapacity(pointCount);
            _filteredPoints.SetCounterValue(0);

            _cs.SetInt("_PointCount", pointCount);
            _cs.SetMatrix("_InputToWorkspace", inputToWorkspace);
            _cs.SetVector("_WorkspaceCorner", workspaceCorner);
            _cs.SetVector("_WorkspaceSize", workspaceSize);
            _cs.SetBuffer(_kernelFilter, "_InPoints", inputPoints);
            _cs.SetBuffer(_kernelFilter, "_OutAppend", _filteredPoints);

            int groups = Mathf.CeilToInt(pointCount / 256f);
            _cs.Dispatch(_kernelFilter, groups, 1, 1);

            ComputeBuffer.CopyCount(_filteredPoints, _countBuffer, 0);
            _countBuffer.GetData(_countReadback, 0, 0, 1);
            filteredCount = (int)_countReadback[0];

            SdfDebug.LogEvery(
                "WorkspacePointConverter.Filter",
                $"[WorkspacePointConverter] Filtered points={filteredCount}/{pointCount} groups={groups} corner={workspaceCorner} size={workspaceSize}",
                1f);

            return _filteredPoints;
        }

        public void Dispose()
        {
            _outPoints?.Release();
            _outPoints = null;
            _filteredPoints?.Release();
            _filteredPoints = null;
            _countBuffer?.Release();
            _countBuffer = null;
            _capacity = 0;
            SdfDebug.LogVerbose("[WorkspacePointConverter] Disposed buffers.");
        }
    }
