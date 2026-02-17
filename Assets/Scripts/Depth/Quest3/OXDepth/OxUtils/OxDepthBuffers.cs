using UnityEngine;

namespace Assets.Scripts.Depth.Quest3.OXDepth.OxUtils
{
    /// <summary>
    /// Manages GPU buffers for point cloud data.
    /// Points are stored as Vector4/float4 (16 bytes per point: x, y, z, w).
    /// </summary>
    public sealed class OXDepthBuffers
    {
        /// <summary>
        /// Main point buffer. Layout: Vector4 per point (x, y, z, w).
        /// </summary>
        public ComputeBuffer Points;

        /// <summary>
        /// Atomic counter for point generation (must be Structured, not Counter type).
        /// </summary>
        public ComputeBuffer Counter;

        /// <summary>
        /// Count readback buffer.
        /// </summary>
        public ComputeBuffer Count;

        /// <summary>
        /// GPU statistics buffer (uint[8]).
        /// </summary>
        public ComputeBuffer Stats;

        /// <summary>
        /// CPU-side sample data for debugging.
        /// </summary>
        public Vector4[] SampleReadback;

        /// <summary>
        /// Maximum number of points this buffer can hold.
        /// </summary>
        public int MaxPoints { get; private set; }

        /// <summary>
        /// Allocate all GPU buffers.
        /// </summary>
        /// <param name="maxPoints">Maximum number of points</param>
        /// <param name="sampleCount">Number of sample points for debugging</param>
        public void Allocate(int maxPoints, int sampleCount)
        {
            Release();
            MaxPoints = maxPoints;

            // Point buffer: Vector4 (float4) per point - 16 bytes
            Points = new ComputeBuffer(maxPoints, sizeof(float) * 4, ComputeBufferType.Structured);

            // CRITICAL: Use Structured type for manual InterlockedAdd access
            // ComputeBufferType.Counter only works with IncrementCounter()/DecrementCounter()
            Counter = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Structured);

            Count = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Structured);

            // Stats buffer: 8 unsigned integers
            Stats = new ComputeBuffer(8, sizeof(uint), ComputeBufferType.Structured);

            SampleReadback = new Vector4[Mathf.Max(1, sampleCount)];
        }

        /// <summary>
        /// Reset buffers at the start of each frame.
        /// </summary>
        public void ResetForFrame()
        {
            // Reset counter to 0 using SetData (not SetCounterValue)
            Counter.SetData(new uint[] { 0 });

            // Reset all statistics to 0
            Stats.SetData(new uint[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        }

        /// <summary>
        /// Release all GPU buffers.
        /// </summary>
        public void Release()
        {
            Points?.Release();
            Points = null;

            Counter?.Release();
            Counter = null;

            Count?.Release();
            Count = null;

            Stats?.Release();
            Stats = null;

            SampleReadback = null;
        }
    }
}