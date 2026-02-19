using UnityEngine;
using Assets.Scripts.Debug;

namespace Assets.Scripts.Depth.Diagnostics
{
    public sealed class DepthOverlayController : MonoBehaviour
    {
        public static bool IsEnabled => _instance != null && _instance.enabled;

        [SerializeField] private DepthProviderFactory depthFactory;
        [SerializeField] private PointCloudOverlayRenderer overlayRenderer;

        private static DepthOverlayController _instance;
        private IDepthProvider _provider;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            if (overlayRenderer == null)
            {
                overlayRenderer = GetComponent<PointCloudOverlayRenderer>();
                if (overlayRenderer == null)
                    overlayRenderer = gameObject.AddComponent<PointCloudOverlayRenderer>();
            }
        }

        private void OnEnable()
        {
            ResolveProvider();
            AttachProvider();
        }

        private void OnDisable()
        {
            if (overlayRenderer != null)
                overlayRenderer.SetProvider(null);
            _provider = null;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public static void Enable()
        {
            var controller = GetOrCreate();
            if (controller == null)
                return;

            controller.enabled = true;
            controller.RefreshProvider();
        }

        public static void Disable()
        {
            if (_instance == null)
                return;
            _instance.enabled = false;
        }

        public static void Refresh()
        {
            if (_instance == null)
                return;
            _instance.RefreshProvider();
        }

        public static void SetProvider(IDepthProvider provider)
        {
            var controller = GetOrCreate();
            if (controller == null)
                return;
            controller._provider = provider;
            controller.AttachProvider();
        }

        public static void SetConfig(DepthOverlayConfig config)
        {
            var controller = GetOrCreate();
            if (controller == null)
                return;

            if (controller.overlayRenderer == null)
                return;

            controller.overlayRenderer.dotSizePx = config.dotSizePx;
            controller.overlayRenderer.maxDistanceMeters = config.maxDistanceMeters;
            controller.overlayRenderer.debugLogEveryNFrames = config.debugLogEveryNFrames;
        }

        public void RefreshProvider()
        {
            if (_provider == null)
                ResolveProvider();
            AttachProvider();
        }

        private void ResolveProvider()
        {
            if (depthFactory == null)
                depthFactory = FindObjectOfType<DepthProviderFactory>(true);

            if (depthFactory == null)
            {
                DebugService.Warn("[DepthOverlayController] DepthProviderFactory not found.");
                _provider = null;
                return;
            }

            _provider = depthFactory.GetProvider();
            if (_provider == null)
            {
                DebugService.Warn("[DepthOverlayController] Failed to resolve IDepthProvider.");
            }
        }

        private void AttachProvider()
        {
            if (overlayRenderer == null)
                return;

            overlayRenderer.SetProvider(_provider);
        }

        private static DepthOverlayController GetOrCreate()
        {
            if (_instance != null)
                return _instance;

            var controllers = Object.FindObjectsOfType<DepthOverlayController>(true);
            if (controllers != null && controllers.Length > 0 && controllers[0] != null)
            {
                _instance = controllers[0];
                return _instance;
            }

            var go = new GameObject("DepthOverlayController");
            var factories = Object.FindObjectsOfType<DepthProviderFactory>(true);
            if (factories != null && factories.Length > 0 && factories[0] != null)
                go.transform.SetParent(factories[0].transform, false);
            _instance = go.AddComponent<DepthOverlayController>();
            return _instance;
        }
    }

    public readonly struct DepthOverlayConfig
    {
        public readonly float dotSizePx;
        public readonly float maxDistanceMeters;
        public readonly int debugLogEveryNFrames;

        public DepthOverlayConfig(float dotSizePx, float maxDistanceMeters, int debugLogEveryNFrames)
        {
            this.dotSizePx = dotSizePx;
            this.maxDistanceMeters = maxDistanceMeters;
            this.debugLogEveryNFrames = debugLogEveryNFrames;
        }

        public static DepthOverlayConfig Default => new DepthOverlayConfig(
            dotSizePx: 2f,
            maxDistanceMeters: 6.0f,
            debugLogEveryNFrames: 120);
    }
}
