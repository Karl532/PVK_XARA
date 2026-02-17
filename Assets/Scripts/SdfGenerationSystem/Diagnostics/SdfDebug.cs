using System;
using System.Collections.Generic;
using UnityEngine;

public static class SdfDebug
{
    private const string Tag = "[SDF_DEBUG]";
    public static bool Enabled = true;
    public static bool Verbose = true;
    public static bool Timing = false;
    public static float DefaultIntervalSeconds = 1.0f;

    private static readonly Dictionary<int, float> LastLogTimeByKey = new Dictionary<int, float>(128);

    private static string TagMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return Tag;
        if (message.StartsWith(Tag, StringComparison.Ordinal)) return message;
        return $"{Tag} {message}";
    }

    public static void Configure(bool enabled, bool verbose, bool timing, float defaultIntervalSeconds)
    {
        Enabled = enabled;
        Verbose = verbose;
        Timing = timing;
        DefaultIntervalSeconds = Mathf.Max(0f, defaultIntervalSeconds);
    }

    public static void Log(string message, UnityEngine.Object context = null)
    {
        if (!Enabled) return;
        Debug.Log(TagMessage(message), context);
    }

    public static void LogVerbose(string message, UnityEngine.Object context = null)
    {
        if (!Enabled || !Verbose) return;
        Debug.Log(TagMessage(message), context);
    }

    public static void Warn(string message, UnityEngine.Object context = null)
    {
        if (!Enabled) return;
        Debug.LogWarning(TagMessage(message), context);
    }

    public static void Error(string message, UnityEngine.Object context = null)
    {
        Debug.LogError(TagMessage(message), context);
    }

    public static void LogEvery(string key, string message, float intervalSeconds = -1f, UnityEngine.Object context = null)
    {
        if (!Enabled) return;
        Debug.Log(TagMessage(message), context);
    }

    public static void WarnEvery(string key, string message, float intervalSeconds = -1f, UnityEngine.Object context = null)
    {
        if (!Enabled) return;
        Debug.LogWarning(TagMessage(message), context);
    }

    public static Scope Sample(string name, UnityEngine.Object context = null)
    {
        if (!Enabled || !Timing) return default;
        return new Scope(name, context);
    }

    public readonly struct Scope : IDisposable
    {
        private readonly string _name;
        private readonly UnityEngine.Object _context;
        private readonly long _startTicks;
        private readonly bool _active;

        public Scope(string name, UnityEngine.Object context)
        {
            _name = name;
            _context = context;
            _startTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            _active = true;
        }

        public void Dispose()
        {
            if (!_active) return;
            long endTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            double elapsedMs = (endTicks - _startTicks) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            LogEvery($"timing:{_name}", $"[Timing] {_name} = {elapsedMs:F2} ms", 0.5f, _context);
        }
    }
}
