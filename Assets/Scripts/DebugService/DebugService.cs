using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Debug
{
    public sealed class DebugService : MonoBehaviour
    {
        public static DebugService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindOrCreate();
                return _instance;
            }
        }

        [Header("Debug Settings")]
        [SerializeField] private bool debugEnabled = true;
        [SerializeField] private float defaultIntervalSeconds = 1.0f;

        private static DebugService _instance;
        private readonly Dictionary<int, float> _lastLogTimeByKey = new Dictionary<int, float>(128);

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void Configure(bool enabled, bool verbose, bool timing, float defaultInterval)
        {
            var svc = Instance;
            svc.debugEnabled = enabled;
            svc.defaultIntervalSeconds = Mathf.Max(0f, defaultInterval);
        }

        public static bool DebugEnabled => Instance.debugEnabled;

        public static void Log(string message, UnityEngine.Object context = null, string tag = null)
        {
            if (!Instance.debugEnabled)
                return;
            UnityEngine.Debug.Log(FormatMessage(message, tag), context);
        }

        public static void LogVerbose(string message, UnityEngine.Object context = null, string tag = null)
        {
            Log(message, context, tag);
        }

        public static void Warn(string message, UnityEngine.Object context = null, string tag = null)
        {
            if (!Instance.debugEnabled)
                return;
            UnityEngine.Debug.LogWarning(FormatMessage(message, tag), context);
        }

        public static void Error(string message, UnityEngine.Object context = null, string tag = null)
        {
            UnityEngine.Debug.LogError(FormatMessage(message, tag), context);
        }

        public static void LogEvery(string key, string message, float intervalSeconds = -1f, UnityEngine.Object context = null)
        {
            Instance.LogEveryInternal(key, message, intervalSeconds, context, isWarning: false);
        }

        public static void WarnEvery(string key, string message, float intervalSeconds = -1f, UnityEngine.Object context = null)
        {
            Instance.LogEveryInternal(key, message, intervalSeconds, context, isWarning: true);
        }

        public static Scope Sample(string name, UnityEngine.Object context = null)
        {
            if (!Instance.debugEnabled)
                return default;
            return new Scope(name, context);
        }

        private void LogEveryInternal(string key, string message, float intervalSeconds, UnityEngine.Object context, bool isWarning)
        {
            if (!debugEnabled)
                return;

            float interval = intervalSeconds >= 0f ? intervalSeconds : defaultIntervalSeconds;
            int hash = key != null ? key.GetHashCode() : 0;
            float now = Time.unscaledTime;

            if (_lastLogTimeByKey.TryGetValue(hash, out float last) && now - last < interval)
                return;

            _lastLogTimeByKey[hash] = now;

            if (isWarning)
                UnityEngine.Debug.LogWarning(message, context);
            else
                UnityEngine.Debug.Log(message, context);
        }

        private static string FormatMessage(string message, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return message;
            if (string.IsNullOrWhiteSpace(message))
                return tag;
            return $"{tag} {message}";
        }

        private static DebugService FindOrCreate()
        {
            var existing = FindObjectOfType<DebugService>(true);
            if (existing != null)
                return existing;

            var go = new GameObject("DebugService");
            return go.AddComponent<DebugService>();
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
            if (!_active)
                return;
            long endTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            double elapsedMs = (endTicks - _startTicks) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            LogEvery($"timing:{_name}", $"[Timing] {_name} = {elapsedMs:F2} ms", 0.5f, _context);
        }
        }
    }
}


