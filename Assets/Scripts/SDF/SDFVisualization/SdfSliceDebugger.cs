using System;
using UnityEngine;


    public sealed class SdfSliceDebugger : IDisposable
    {
        private readonly ComputeShader _cs;
        private readonly int _kernel;

        private RenderTexture _slice; // RHalf or RFloat 2D
        private int _sliceRes;

        public RenderTexture SliceTexture => _slice;

        public SdfSliceDebugger(ComputeShader sdfSliceCS)
        {
            _cs = sdfSliceCS ? sdfSliceCS : throw new ArgumentNullException(nameof(sdfSliceCS));
            _kernel = _cs.FindKernel("CSSlice");
        }

        public void EnsureSlice(int resolution)
        {
            if (resolution <= 0) resolution = 1;
            if (_slice != null && _sliceRes == resolution && _slice.IsCreated()) return;

            if (_slice != null) _slice.Release();

            _sliceRes = resolution;
            _slice = new RenderTexture(_sliceRes, _sliceRes, 0, RenderTextureFormat.RHalf)
            {
                enableRandomWrite = true,
                dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"SDF_Slice_{_sliceRes}x{_sliceRes}"
            };
            _slice.Create();
        }

        /// <summary>
        /// Create a 2D slice from a 3D TSDF texture.
        /// axis: 0=X, 1=Y, 2=Z
        /// slice01: 0..1 along that axis
        /// </summary>
        public RenderTexture BuildSlice(Texture tsdf3D, int volumeResolution, int axis, float slice01, float mu)
        {
            if (tsdf3D == null) throw new ArgumentNullException(nameof(tsdf3D));
            EnsureSlice(volumeResolution);

            _cs.SetInt("_Resolution", volumeResolution);
            _cs.SetInt("_Axis", Mathf.Clamp(axis, 0, 2));
            _cs.SetFloat("_Slice01", Mathf.Clamp01(slice01));
            _cs.SetFloat("_Mu", Mathf.Max(1e-6f, mu));

            _cs.SetTexture(_kernel, "_Tsdf3D", tsdf3D);
            _cs.SetTexture(_kernel, "_OutSlice", _slice);

            int g = Mathf.CeilToInt(volumeResolution / 8f);
            _cs.Dispatch(_kernel, g, g, 1);

            return _slice;
        }

        public void Dispose()
        {
            if (_slice != null)
            {
                _slice.Release();
                _slice = null;
            }
        }
    }

