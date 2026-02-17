using UnityEngine;

public partial class SdfGenerator
{
    // ===== Volume internals =====
    private struct VolumeInternals
    {
        public int Resolution;
        public float Mu;
        public Vector3 Corner;
        public Vector3 Size;

        public RenderTexture SeedA; // ARGBFloat: xyz=seed pos, w=valid (1/0)
        public RenderTexture SeedB; // ping-pong
        public RenderTexture Tsdf;  // RHalf: unsigned distance clamped to Mu

        public bool IsAllocated => SeedA != null;

        public static VolumeInternals Create(int resolution, float mu)
        {
            var v = new VolumeInternals
            {
                Resolution = resolution,
                Mu = mu,
                SeedA = CreateTex3D(resolution, RenderTextureFormat.ARGBFloat, "SDF_SeedA"),
                SeedB = CreateTex3D(resolution, RenderTextureFormat.ARGBFloat, "SDF_SeedB"),
                Tsdf = CreateTex3D(resolution, RenderTextureFormat.RHalf, "SDF_TSDF"),
            };
            return v;
        }

        private static RenderTexture CreateTex3D(int res, RenderTextureFormat fmt, string name)
        {
            var rt = new RenderTexture(res, res, 0, fmt)
            {
                dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
                volumeDepth = res,
                enableRandomWrite = true,
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = name
            };
            rt.Create();
            return rt;
        }

        public void Release()
        {
            SeedA?.Release(); SeedA = null;
            SeedB?.Release(); SeedB = null;
            Tsdf?.Release(); Tsdf = null;
        }
    }
}
