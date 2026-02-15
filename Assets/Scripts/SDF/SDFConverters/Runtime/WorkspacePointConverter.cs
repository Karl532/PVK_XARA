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

        private ComputeBuffer _outPoints; // float4
        private int _capacity;

        public ComputeBuffer OutputBuffer => _outPoints;

        public WorkspacePointConverter(ComputeShader pointsToWorkspaceCS)
        {
            _cs = pointsToWorkspaceCS ? pointsToWorkspaceCS : throw new ArgumentNullException(nameof(pointsToWorkspaceCS));
            _kernel = _cs.FindKernel("CSTransform");
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
            _capacity = maxPoints;
            _outPoints = new ComputeBuffer(_capacity, sizeof(float) * 4, ComputeBufferType.Structured);
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

            return _outPoints;
        }

        public void Dispose()
        {
            _outPoints?.Release();
            _outPoints = null;
            _capacity = 0;
        }
    }

