using UnityEngine;

    public class SdfOverlayRenderer : MonoBehaviour
    {
        [Header("Compute")]
        [SerializeField] private ComputeShader overlayCS;

        [Header("Camera")]
        [SerializeField] private Camera cam;
        [SerializeField] private bool halfResolution = true;

        private int _kernelClear;
        private int _kernelPoints;
        private RenderTexture _overlayRT;

        public RenderTexture OverlayRT => _overlayRT;

        private void Awake()
        {
            if (!cam) cam = Camera.main;
            _kernelClear = overlayCS.FindKernel("CSClear");
            _kernelPoints = overlayCS.FindKernel("CSPoints");
        }

        public void SetResources(ComputeShader cs, Camera targetCamera)
        {
            overlayCS = cs;
            cam = targetCamera;
            _kernelClear = overlayCS.FindKernel("CSClear");
            _kernelPoints = overlayCS.FindKernel("CSPoints");
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
            ComputeBuffer worldPoints,
            int pointCount,
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
            if (!overlayCS || worldPoints == null || pointCount <= 0 || !globalTsdf)
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
            Matrix4x4 view = cam.worldToCameraMatrix;
            Matrix4x4 proj = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);
            Matrix4x4 viewProj = proj * view;
            overlayCS.SetMatrix("_ViewProj", viewProj);
            overlayCS.SetMatrix("_WorldToWorkspace", worldToWorkspace);

            overlayCS.SetVector("_LocalCorner", localCorner);
            overlayCS.SetVector("_LocalSize", localSize);
            overlayCS.SetFloat("_LocalMu", localMu);

            overlayCS.SetVector("_GlobalCorner", globalCorner);
            overlayCS.SetVector("_GlobalSize", globalSize);
            overlayCS.SetFloat("_GlobalMu", globalMu);

            overlayCS.SetInt("_HasLocal", localTsdf ? 1 : 0);

            if (localTsdf)
                overlayCS.SetTexture(_kernelPoints, "_LocalTsdf3D", localTsdf);

            overlayCS.SetInt("_PointCount", pointCount);
            overlayCS.SetBuffer(_kernelPoints, "_PointsWorld", worldPoints);
            overlayCS.SetTexture(_kernelPoints, "_GlobalTsdf3D", globalTsdf);
            overlayCS.SetTexture(_kernelPoints, "_OverlayOut", _overlayRT);

            overlayCS.SetTexture(_kernelClear, "_OverlayOut", _overlayRT);

            int gx = Mathf.CeilToInt(width / 8f);
            int gy = Mathf.CeilToInt(height / 8f);

            overlayCS.Dispatch(_kernelClear, gx, gy, 1);

            int pointsGroups = Mathf.CeilToInt(pointCount / 256f);
            overlayCS.Dispatch(_kernelPoints, pointsGroups, 1, 1);
        }

        private void OnDestroy()
        {
            if (_overlayRT != null)
                _overlayRT.Release();
        }
    }

