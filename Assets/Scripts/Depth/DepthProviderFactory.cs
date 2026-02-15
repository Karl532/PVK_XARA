using Assets.Scripts.Depth.Quest3.OXDepth;
using Assets.Scripts.Depth.Quest3.OXDepth.OxUtils;
using UnityEngine;

/// <summary>
/// Factory that creates and manages depth providers based on device type.
/// Now creates providers programmatically instead of requiring prefabs.
/// </summary>
public class DepthProviderFactory : MonoBehaviour
{
    #region Configuration
    [Header("Depth Provider Configuration")]
    [SerializeField] private DeviceType deviceType = DeviceType.Quest3;

    [Header("Required Assets")]
    [Tooltip("XR Origin transform (auto-find if not assigned)")]
    [SerializeField] private Transform trackingOrigin;

    [Tooltip("Compute shader for building point cloud")]
    [SerializeField] private ComputeShader buildPointCloudCS;

    [Header("Quest3 Settings")]
    [Range(1, 32)]
    [SerializeField] private int skipPixels = 2;
    [SerializeField] private bool useLeftEyeSlice = true;
    [Range(0f, 1f)]
    [SerializeField] private float minDepth01 = 0.001f;
    [Range(0f, 1f)]
    [SerializeField] private float maxDepth01 = 0.999f;
    [SerializeField] private bool flipY = false;
    [SerializeField] private int maxPoints = 250000;
    #endregion

    #region Public Types
    public enum DeviceType
    {
        Quest3,
        // Future: AzureKinect, RealSense, etc.
    }
    #endregion

    #region Private Fields
    private IDepthProvider _activeProvider;
    #endregion

    #region Public Methods
    /// <summary>
    /// Get or create the depth provider for the current device type.
    /// </summary>
    public IDepthProvider GetProvider()
    {
        if (_activeProvider != null && _activeProvider.IsEnabled)
        {
            return _activeProvider;
        }

        // Validate configuration
        if (!ValidateConfiguration())
        {
            Debug.LogError("[DepthProviderFactory] Configuration validation failed");
            return null;
        }

        // Create provider based on device type
        _activeProvider = CreateProvider(deviceType);

        if (_activeProvider == null)
        {
            Debug.LogError($"[DepthProviderFactory] Failed to create provider for {deviceType}");
            return null;
        }

        // Enable the provider (this instantiates its components)
        _activeProvider.Enable(transform);

        Debug.Log($"[DepthProviderFactory] Created and enabled {deviceType} provider");

        return _activeProvider;
    }

    /// <summary>
    /// Switch to a different provider type.
    /// </summary>
    public void SwitchProvider(DeviceType newDeviceType)
    {
        // Disable current provider
        if (_activeProvider != null)
        {
            _activeProvider.Disable();
            _activeProvider = null;
        }

        deviceType = newDeviceType;

        // Get new provider (will create and enable)
        GetProvider();
    }
    #endregion

    #region Private Methods
    private bool ValidateConfiguration()
    {
        // Auto-find tracking origin if not assigned
        if (trackingOrigin == null)
        {
            trackingOrigin = OXDepthXR.FindXROrigin();
            if (trackingOrigin == null)
            {
                Debug.LogError("[DepthProviderFactory] Could not find XR Origin transform");
                return false;
            }
        }

        if (buildPointCloudCS == null)
        {
            Debug.LogError("[DepthProviderFactory] Build point cloud compute shader not assigned");
            return false;
        }

        return true;
    }

    private IDepthProvider CreateProvider(DeviceType type)
    {
        switch (type)
        {
            case DeviceType.Quest3:
                return new Quest3DepthProvider(
                    trackingOrigin: trackingOrigin,
                    buildPointCloudCS: buildPointCloudCS,
                    skipPixels: skipPixels,
                    useLeftEyeSlice: useLeftEyeSlice,
                    minDepth01: minDepth01,
                    maxDepth01: maxDepth01,
                    flipY: flipY,
                    maxPoints: maxPoints
                );

            // Future providers can be added here
            default:
                Debug.LogError($"[DepthProviderFactory] Unknown device type: {type}");
                return null;
        }
    }
    #endregion

    #region Unity Lifecycle
    private void OnDestroy()
    {
        // Clean up provider
        if (_activeProvider != null)
        {
            _activeProvider.Disable();
            _activeProvider = null;
        }
    }
    #endregion
}