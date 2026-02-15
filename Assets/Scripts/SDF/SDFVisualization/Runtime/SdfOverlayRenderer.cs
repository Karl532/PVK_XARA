using UnityEngine;

    public class SdfOverlayRenderer : MonoBehaviour
    {
        [Header("Compute")]
        [SerializeField] private ComputeShader overlayCS;

        [Header("Camera")]
        [SerializeField] private Camera cam;
        [SerializeField] private bool halfResolution = true;

        private int _kernel;
        private RenderTexture _overlayRT;

        public RenderTexture OverlayRT => _overlayRT;

        private void Awake()
        {
            if (!cam) cam = Camera.main;
            _kernel = overlayCS.FindKernel("CSOverlay");
        }

        public void SetResources(ComputeShader cs, Camera targetCamera)
        {
            overlayCS = cs;
            cam = targetCamera;
            _kernel = overlayCS.FindKernel("CSOverlay");
        }


    private void EnsureRT(int width, int height)
        {
            if (_overlayRT != null &&
                _overlayRT.width == width &&
                _overlayRT.height == height &&
                _overlayRT.IsCreated())
                return;

            if (_overlayRT != null)
                _overlayRT.Release();

            _overlayRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "SDF_OverlayRT"
            };

            _overlayRT.Create();
        }

        public void RenderOverlay(
            Texture depthTexture,
            Matrix4x4 invViewProj,
            Matrix4x4 worldToWorkspace,

            Texture localTsdf,
            Vector3 localCorner,
            Vector3 localSize,
            float localMu,

            Texture globalTsdf,
            Vector3 globalCorner,
            Vector3 globalSize,
            float globalMu)
        {
            if (!overlayCS || !depthTexture || !globalTsdf)
                return;

            int width = cam.pixelWidth;
            int height = cam.pixelHeight;

            if (halfResolution)
            {
                width /= 2;
                height /= 2;
            }

            EnsureRT(width, height);

            overlayCS.SetInts("_OutputSize", width, height);
            overlayCS.SetMatrix("_InvViewProj", invViewProj);
            overlayCS.SetMatrix("_WorldToWorkspace", worldToWorkspace);

            overlayCS.SetVector("_LocalCorner", localCorner);
            overlayCS.SetVector("_LocalSize", localSize);
            overlayCS.SetFloat("_LocalMu", localMu);

            overlayCS.SetVector("_GlobalCorner", globalCorner);
            overlayCS.SetVector("_GlobalSize", globalSize);
            overlayCS.SetFloat("_GlobalMu", globalMu);

            overlayCS.SetInt("_HasLocal", localTsdf ? 1 : 0);

            overlayCS.SetTexture(_kernel, "_DepthTex", depthTexture);

            if (localTsdf)
                overlayCS.SetTexture(_kernel, "_LocalTsdf3D", localTsdf);

            overlayCS.SetTexture(_kernel, "_GlobalTsdf3D", globalTsdf);
            overlayCS.SetTexture(_kernel, "_OverlayOut", _overlayRT);

            int gx = Mathf.CeilToInt(width / 8f);
            int gy = Mathf.CeilToInt(height / 8f);

            overlayCS.Dispatch(_kernel, gx, gy, 1);
        }

        private void OnDestroy()
        {
            if (_overlayRT != null)
                _overlayRT.Release();
        }
    }

