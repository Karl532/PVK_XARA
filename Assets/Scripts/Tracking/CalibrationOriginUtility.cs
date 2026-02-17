using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helper methods for working with the calibrated world origin.
/// </summary>
public static class CalibrationOriginUtility
{
    private static readonly HashSet<int> LoggedTargets = new HashSet<int>();

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

        int id = target.GetInstanceID();
        if (!LoggedTargets.Contains(id))
        {
            LoggedTargets.Add(id);
            string originParent = origin.parent != null ? origin.parent.name : "null";
            string targetParent = target.parent != null ? target.parent.name : "null";
            Debug.Log($"[CalibrationOrigin] Attached '{target.name}' under origin '{origin.name}' (origin parent='{originParent}', target parent now='{targetParent}').");
        }
    }
}

