using System.Collections.Generic;
using Unity.XR.Oculus;
using UnityEngine;
using UnityEngine.XR;

namespace Assets.Scripts.Depth.Quest3.OXDepth.OxUtils
{
    /// <summary>
    /// Static helper class for XR depth operations.
    /// Encapsulates all XR subsystem and depth texture handling.
    /// </summary>
    public static class OXDepthXR
    {
        /// <summary>
        /// Get the active XR display subsystem.
        /// </summary>
        public static XRDisplaySubsystem GetXRDisplaySubsystem()
        {
            var displays = new List<XRDisplaySubsystem>();
#if UNITY_2020_2_OR_NEWER
            SubsystemManager.GetSubsystems(displays);
#else
            SubsystemManager.GetInstances(displays);
#endif
            foreach (var d in displays)
            {
                if (d != null && d.running)
                    return d;
            }
            return displays.Count > 0 ? displays[0] : null;
        }

        /// <summary>
        /// Check if environment depth is supported and enable it.
        /// </summary>
        /// <returns>True if depth is supported and enabled successfully</returns>
        public static bool EnsureDepthEnabled()
        {
            if (!Utils.GetEnvironmentDepthSupported())
                return false;

            Utils.SetupEnvironmentDepth(new Utils.EnvironmentDepthCreateParams { removeHands = false });
            Utils.SetEnvironmentDepthRendering(true);
            return true;
        }

        /// <summary>
        /// Disable environment depth rendering.
        /// </summary>
        public static void DisableDepth()
        {
            Utils.SetEnvironmentDepthRendering(false);
        }

        /// <summary>
        /// Shutdown environment depth system completely.
        /// </summary>
        public static void ShutdownDepth()
        {
            Utils.ShutdownEnvironmentDepth();
        }

        /// <summary>
        /// Try to acquire depth frame and texture for the specified eye.
        /// </summary>
        /// <param name="display">XR display subsystem</param>
        /// <param name="eye">Eye index (0 = left, 1 = right)</param>
        /// <param name="frame">Output frame descriptor</param>
        /// <param name="depthRT">Output depth render texture</param>
        /// <param name="texId">Output texture ID</param>
        /// <returns>True if all data is valid and acquired</returns>
        public static bool TryGetDepthFrameAndTexture(
            XRDisplaySubsystem display,
            int eye,
            out Utils.EnvironmentDepthFrameDesc frame,
            out RenderTexture depthRT,
            out uint texId)
        {
            frame = Utils.GetEnvironmentDepthFrameDesc(eye);
            depthRT = null;
            texId = 0;

            if (!frame.isValid)
                return false;

            if (!Utils.GetEnvironmentDepthTextureId(ref texId) || texId == 0)
                return false;

            depthRT = display.GetRenderTexture(texId);
            return depthRT != null;
        }

        /// <summary>
        /// Get reprojection matrix for the specified eye.
        /// </summary>
        /// <param name="eye">Eye index (0 = left, 1 = right)</param>
        /// <param name="invMatrix">Output inverse reprojection matrix</param>
        /// <returns>True if matrix is valid</returns>
        public static bool TryGetReprojectionMatrix(int eye, out Matrix4x4 invMatrix)
        {
            invMatrix = Matrix4x4.zero;

            Matrix4x4[] reproj = Shader.GetGlobalMatrixArray(
                Shader.PropertyToID("_EnvironmentDepthReprojectionMatrices")
            );

            if (reproj == null || reproj.Length <= eye)
            {
                return false;
            }

            invMatrix = reproj[eye].inverse;
            return true;
        }

        /// <summary>
        /// Find XR Origin transform in the scene.
        /// </summary>
        /// <returns>XR Origin transform or null if not found</returns>
        public static Transform FindXROrigin()
        {
            var xrOrigin = GameObject.Find("XR Origin");
            return xrOrigin != null ? xrOrigin.transform : null;
        }

        /// <summary>
        /// Get tracking to world space matrix from XR Origin.
        /// </summary>
        /// <param name="trackingOrigin">XR Origin transform</param>
        /// <returns>Transformation matrix, or identity if origin is null</returns>
        public static Matrix4x4 GetTrackingToWorldMatrix(Transform trackingOrigin)
        {
            return trackingOrigin != null
                ? trackingOrigin.localToWorldMatrix
                : Matrix4x4.identity;
        }
    }
}