using UnityEngine;

/// <summary>
/// Helper methods for working with the calibrated world origin.
/// </summary>
public static class CalibrationOriginUtility
{
    /// <summary>
    /// Parents the given transform under the current calibration origin, if one exists.
    /// Does nothing if either the target is null or no CalibrationOriginController is present.
    /// </summary>
    /// <param name="target">Transform to parent under the origin.</param>
    /// <param name="worldPositionStays">
    /// Whether to keep the current world position when reparenting (default: true).
    /// </param>
    public static void AttachToOrigin(Transform target, bool worldPositionStays = true)
    {
        if (target == null)
            return;

        var origin = CalibrationOriginController.OriginTransform;
        if (origin == null)
            return;

        target.SetParent(origin, worldPositionStays);
    }
}

