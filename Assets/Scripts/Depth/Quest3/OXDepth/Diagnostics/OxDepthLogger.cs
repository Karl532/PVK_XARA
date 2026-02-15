using UnityEngine;

namespace Assets.Scripts.Depth.Quest3.OXDepth.Diagnostics
{
    public static class OXDepthLogger
    {
        public const string TAG_CORE = "[OX_DEPTH_CORE]";
        public const string TAG_TEX = "[OX_DEPTH_TEX]";
        public const string TAG_GPU = "[OX_DEPTH_GPU]";
        public const string TAG_PTS = "[OX_DEPTH_PTS]";
        public const string TAG_RENDER = "[OX_DEPTH_RENDER]";
        public const string TAG_DEBUG = "[OX_DEPTH_DEBUG]";

        public static void Info(string tag, string msg) => Debug.Log($"{tag}[I] {msg}");
        public static void Warn(string tag, string msg) => Debug.LogWarning($"{tag}[W] {msg}");
        public static void Err(string tag, string msg) => Debug.LogError($"{tag}[E] {msg}");

        public static void EveryN(string tag, string msg, int n)
        {
            if (n <= 0) return;
            if (Time.frameCount % n != 0) return;
            Debug.Log($"{tag}[I] {msg}");
        }
    }
}
