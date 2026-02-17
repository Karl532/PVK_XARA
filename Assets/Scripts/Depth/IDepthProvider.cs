using Assets.Scripts.Depth.Quest3.OXDepth;
using System;
using UnityEngine;

public interface IDepthProvider
{
    /// <summary>
    /// Is the depth provider currently ready to provide point cloud data?
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Current point count from last update.
    /// </summary>
    int PointCount { get; }

    /// <summary>
    /// Current depth texture dimensions.
    /// </summary>
    Vector2Int DepthResolution { get; }

    /// <summary>
    /// Event fired when new point cloud data is available.
    /// </summary>
    event Action<PointCloudData> OnPointCloudUpdated;

    /// <summary>
    /// Event fired when depth frame becomes invalid.
    /// </summary>
    event Action OnDepthInvalid;

    /// <summary>
    /// Get the raw point buffer (read-only access).
    /// Returns null if not ready.
    /// </summary>
    ComputeBuffer GetPointBuffer();

    /// <summary>
    /// Get the current depth texture (if available).
    /// </summary>
    RenderTexture GetDepthTexture();

    /// <summary>
    /// Get the inverse depth view-projection matrix for the current frame.
    /// </summary>
    Matrix4x4 GetInvDepthViewProj();

    /// <summary>
    /// Get the tracking-to-world matrix for the current frame.
    /// </summary>
    Matrix4x4 GetTrackingToWorld();

    /// <summary>
    /// Get depth eye slice index (0=left, 1=right).
    /// </summary>
    int GetDepthEyeSlice();

    /// <summary>
    /// True if the depth texture needs Y flip.
    /// </summary>
    bool GetFlipY();

    /// <summary>
    /// Get the latest statistics from GPU.
    /// </summary>
    GpuStatistics GetStatistics();

    /// <summary>
    /// Enable this depth provider (start processing).
    /// This should instantiate and set up all required components.
    /// </summary>
    /// <param name="parent">Parent transform to instantiate under</param>
    void Enable(Transform parent);

    /// <summary>
    /// Disable this depth provider (stop processing).
    /// </summary>
    void Disable();

    /// <summary>
    /// Is this provider currently enabled?
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Get the GameObject instance (if created).
    /// </summary>
    GameObject GetGameObject();
}
