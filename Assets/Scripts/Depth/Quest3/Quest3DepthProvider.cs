using Assets.Scripts.Depth.Quest3.OXDepth;
using System;
using UnityEngine;

/// <summary>
/// Quest 3 depth provider that creates its own API component when enabled.
/// </summary>
public class Quest3DepthProvider : IDepthProvider
{
    #region Configuration
    private readonly Transform _trackingOrigin;
    private readonly ComputeShader _buildPointCloudCS;
    private readonly int _skipPixels;
    private readonly bool _useLeftEyeSlice;
    private readonly float _minDepth01;
    private readonly float _maxDepth01;
    private readonly bool _flipY;
    private readonly int _maxPoints;
    #endregion

    #region Private Fields
    private GameObject _apiGameObject;
    private OXDepthPointCloudAPI _depthAPI;
    private bool _isEnabled;
    #endregion

    #region Public Properties
    public bool IsReady => _isEnabled && _depthAPI != null && _depthAPI.IsReady;
    public int PointCount => _depthAPI != null ? _depthAPI.PointCount : 0;
    public Vector2Int DepthResolution => _depthAPI != null ? _depthAPI.DepthResolution : Vector2Int.zero;
    public bool IsEnabled => _isEnabled;
    #endregion

    #region Events
    public event Action<PointCloudData> OnPointCloudUpdated;
    public event Action OnDepthInvalid;
    #endregion

    #region Constructor
    public Quest3DepthProvider(
        Transform trackingOrigin,
        ComputeShader buildPointCloudCS,
        int skipPixels = 2,
        bool useLeftEyeSlice = true,
        float minDepth01 = 0.001f,
        float maxDepth01 = 0.999f,
        bool flipY = false,
        int maxPoints = 250000)
    {
        _trackingOrigin = trackingOrigin;
        _buildPointCloudCS = buildPointCloudCS;
        _skipPixels = skipPixels;
        _useLeftEyeSlice = useLeftEyeSlice;
        _minDepth01 = minDepth01;
        _maxDepth01 = maxDepth01;
        _flipY = flipY;
        _maxPoints = maxPoints;
    }
    #endregion

    #region IDepthProvider Implementation
    public void Enable(Transform parent)
    {
        if (_isEnabled)
        {
            Debug.LogWarning("[Quest3DepthProvider] Already enabled");
            return;
        }

        Debug.Log("[Quest3DepthProvider] Enabling...");

        // Create GameObject
        _apiGameObject = new GameObject("Quest3DepthAPI");
        _apiGameObject.transform.SetParent(parent);

        // Add and configure API component
        _depthAPI = _apiGameObject.AddComponent<OXDepthPointCloudAPI>();

        // Configure via reflection or public method
        ConfigureDepthAPI();

        // Subscribe to events
        _depthAPI.OnPointCloudUpdated += HandlePointCloudUpdated;
        _depthAPI.OnDepthInvalid += HandleDepthInvalid;

        // Enable the API
        _depthAPI.EnableAPI();

        _isEnabled = true;
        Debug.Log("[Quest3DepthProvider] Enabled successfully");
    }

    public void Disable()
    {
        if (!_isEnabled)
        {
            return;
        }

        Debug.Log("[Quest3DepthProvider] Disabling...");

        if (_depthAPI != null)
        {
            _depthAPI.OnPointCloudUpdated -= HandlePointCloudUpdated;
            _depthAPI.OnDepthInvalid -= HandleDepthInvalid;
            _depthAPI.DisableAPI();
        }

        if (_apiGameObject != null)
        {
            UnityEngine.Object.Destroy(_apiGameObject);
            _apiGameObject = null;
        }

        _depthAPI = null;
        _isEnabled = false;

        Debug.Log("[Quest3DepthProvider] Disabled");
    }

    public ComputeBuffer GetPointBuffer()
    {
        return _isEnabled && _depthAPI != null ? _depthAPI.PointBuffer : null;
    }

    public GpuStatistics GetStatistics()
    {
        return _isEnabled && _depthAPI != null ? _depthAPI.LastStatistics : default;
    }

    public GameObject GetGameObject()
    {
        return _apiGameObject;
    }
    #endregion

    #region Private Methods
    private void ConfigureDepthAPI()
    {
        // Access serialized fields via reflection
        var type = typeof(OXDepthPointCloudAPI);

        SetField(type, "trackingOrigin", _trackingOrigin);
        SetField(type, "buildPointCloudCS", _buildPointCloudCS);
        SetField(type, "skipPixels", _skipPixels);
        SetField(type, "useLeftEyeSlice", _useLeftEyeSlice);
        SetField(type, "minDepth01", _minDepth01);
        SetField(type, "maxDepth01", _maxDepth01);
        SetField(type, "flipY", _flipY);
        SetField(type, "maxPoints", _maxPoints);
    }

    private void SetField(System.Type type, string fieldName, object value)
    {
        var field = type.GetField(fieldName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);

        if (field != null)
        {
            field.SetValue(_depthAPI, value);
        }
        else
        {
            Debug.LogWarning($"[Quest3DepthProvider] Could not find field: {fieldName}");
        }
    }

    private void HandlePointCloudUpdated(PointCloudData data)
    {
        OnPointCloudUpdated?.Invoke(data);
    }

    private void HandleDepthInvalid()
    {
        OnDepthInvalid?.Invoke();
    }
    #endregion
}