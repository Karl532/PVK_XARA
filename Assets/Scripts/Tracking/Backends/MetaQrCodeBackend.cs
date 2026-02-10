using System;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;

/// <summary>
/// Fiducial backend that uses Meta's built-in QR code tracking (MR Utility Kit / Trackables).
///
/// Notes:
/// - This is not ArUco. The "marker" is a QR code, and the "marker ID" comes from the QR payload.
/// - Runtime update rate can be relatively low (often around ~1 Hz), so this is best for
///   calibration / world origin alignment, not high-speed continuous tracking.
/// </summary>
public sealed class MetaQrCodeBackend : IFiducialBackend
{
    private TrackingBridge _bridge;
    private FiducialTrackingManager _trackingManager;
    private TrackingBridge.TrackingBridgeConfig _config;

    private MRUK _mruk;
    private readonly List<MRUKTrackable> _trackables = new List<MRUKTrackable>(32);

    public void Initialize(TrackingBridge bridge, FiducialTrackingManager trackingManager)
    {
        _bridge = bridge;
        _trackingManager = trackingManager;
        _config = bridge != null ? bridge.Config : default;

        _mruk = MRUK.Instance;
        if (_mruk == null)
        {
            Debug.LogError("[MetaQrCodeBackend] MRUK.Instance is null. Add an MRUK GameObject/prefab to the scene to use QR code tracking.");
            return;
        }

        EnableQrTrackingIfPossible();
        Debug.Log("[MetaQrCodeBackend] Initialized.");
    }

    public void Configure(TrackingBridge.TrackingBridgeConfig config)
    {
        _config = config;

        // Ensure QR tracking stays enabled when this backend is selected.
        EnableQrTrackingIfPossible();
    }

    public void ProcessFrame()
    {
        if (_mruk == null || _trackingManager == null)
            return;

        _mruk.GetTrackables(_trackables);

        for (int i = 0; i < _trackables.Count; i++)
        {
            var t = _trackables[i];
            if (t == null)
                continue;

            if (t.TrackableType != OVRAnchor.TrackableType.QRCode)
                continue;

            if (!t.IsTracked)
                continue;

            string payload = t.MarkerPayloadString;
            if (string.IsNullOrEmpty(payload))
                continue;

            int id = GetStableMarkerId(payload);

            if (_config.markerIdWhitelist != null && _config.markerIdWhitelist.Length > 0)
            {
                if (Array.IndexOf(_config.markerIdWhitelist, id) < 0)
                    continue;
            }

            _trackingManager.UpdateMarkerPose(id, t.transform.position, t.transform.rotation);
        }
    }

    public void Shutdown()
    {
        _mruk = null;
        _bridge = null;
        _trackingManager = null;
        _trackables.Clear();
        Debug.Log("[MetaQrCodeBackend] Shutdown.");
    }

    private void EnableQrTrackingIfPossible()
    {
        if (_mruk == null || _mruk.SceneSettings == null)
            return;

        // MRUK expects configuration changes through SceneSettings.TrackerConfiguration.
        var desired = _mruk.SceneSettings.TrackerConfiguration;
        desired.QRCodeTrackingEnabled = true;
        _mruk.SceneSettings.TrackerConfiguration = desired;
    }

    /// <summary>
    /// Converts a QR payload string into a stable int marker ID for our tracking manager.
    ///
    /// Recommended payload format for your project:
    /// - A plain integer like "12" (best), or
    /// - A string containing an integer like "block:12" or "id=12".
    ///
    /// If no integer can be parsed, we fall back to a stable 32-bit hash of the payload.
    /// </summary>
    private static int GetStableMarkerId(string payload)
    {
        if (TryParseFirstInt(payload, out int parsed))
            return parsed;

        // FNV-1a 32-bit hash (stable across sessions)
        unchecked
        {
            const int fnvOffset = (int)2166136261;
            const int fnvPrime = 16777619;

            int hash = fnvOffset;
            for (int i = 0; i < payload.Length; i++)
            {
                hash ^= payload[i];
                hash *= fnvPrime;
            }

            // Avoid returning 0 if possible (0 is a common default marker id)
            if (hash == 0) hash = 1;
            return hash;
        }
    }

    private static bool TryParseFirstInt(string s, out int value)
    {
        value = 0;
        if (string.IsNullOrEmpty(s))
            return false;

        int i = 0;
        while (i < s.Length)
        {
            // Find start of a number (optionally preceded by '-')
            bool negative = false;
            if (s[i] == '-')
            {
                negative = true;
                i++;
            }

            if (i < s.Length && char.IsDigit(s[i]))
            {
                long acc = 0;
                while (i < s.Length && char.IsDigit(s[i]))
                {
                    acc = acc * 10 + (s[i] - '0');
                    if (acc > int.MaxValue) break;
                    i++;
                }

                int v = (int)Mathf.Clamp((float)acc, 0, int.MaxValue);
                value = negative ? -v : v;
                return true;
            }

            i++;
        }

        return false;
    }
}

