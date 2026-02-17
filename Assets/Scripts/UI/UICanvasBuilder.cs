using System;
using UnityEngine;
using UnityEngine.UI;

public static class UICanvasBuilder
{
    public static GameObject BuildCanvas(Transform parent, UIThemeConfig themeConfig)
    {
        if (parent == null) throw new ArgumentNullException(nameof(parent));
        if (themeConfig == null) throw new ArgumentNullException(nameof(themeConfig));

        GameObject canvasObject = new GameObject("VR_UI_Canvas");
        canvasObject.transform.SetParent(parent);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = themeConfig.dynamicPixelsPerUnit;

        CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        TryAddTrackedDeviceGraphicRaycaster(canvasObject);

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = themeConfig.canvasSize;
        canvasRect.position = themeConfig.canvasPosition;
        canvasRect.localScale = Vector3.one * themeConfig.canvasScale;

        BoxCollider rootCollider = canvasObject.AddComponent<BoxCollider>();
        rootCollider.center = Vector3.zero;
        rootCollider.size = new Vector3(canvasRect.sizeDelta.x, canvasRect.sizeDelta.y, themeConfig.canvasDepth);

        TryAddXRGrabInteractable(canvasObject);
        TryAddUIKeepUpright(canvasObject);

        return canvasObject;
    }

    private static void TryAddTrackedDeviceGraphicRaycaster(GameObject target)
    {
        // XR Interaction Toolkit 2.x namespace
        var type = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (type == null) return;
        if (target.GetComponent(type) != null) return;
        target.AddComponent(type);
    }

    private static void TryAddXRGrabInteractable(GameObject target)
    {
        // Try both pre-3.0 and 3.x namespaces for XRGrabInteractable.
        var type = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
        if (type == null)
        {
            type = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
        }

        if (type == null) return;
        if (target.GetComponent(type) != null) return;

        // Ensure we have a Rigidbody configured for kinematic, no-gravity motion
        // so the UI doesn't fall due to physics or flip uncontrollably.
        var rb = target.GetComponent<Rigidbody>();
        if (rb == null)
            rb = target.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;
        // Only allow yaw rotation so the panel can't be grabbed and turned upside-down.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        var grab = target.AddComponent(type);

        // Best-effort: set movementType to VelocityTracking if available, so it follows controllers naturally.
        try
        {
            var baseType = type.BaseType;
            while (baseType != null && baseType.FullName != null && !baseType.FullName.Contains("XRBaseInteractable"))
            {
                baseType = baseType.BaseType;
            }

            if (baseType != null)
            {
                var movementTypeProp = baseType.GetProperty("movementType");
                if (movementTypeProp != null && movementTypeProp.CanWrite)
                {
                    var movementEnumType = movementTypeProp.PropertyType;
                    var velocityValue = Enum.Parse(movementEnumType, "VelocityTracking", ignoreCase: true);
                    movementTypeProp.SetValue(grab, velocityValue);
                }
            }
        }
        catch
        {
            // Ignore – defaults are usually fine; this just tweaks feel when available.
        }
    }

    private static void TryAddUIKeepUpright(GameObject target)
    {
        // Use reflection so this compiles even if UIKeepUpright lives in a different assembly/namespace.
        var type = Type.GetType("UIKeepUpright");
        if (type == null)
        {
            type = Type.GetType("UI.Utils.UIKeepUpright");
        }

        if (type == null) return;
        if (target.GetComponent(type) != null) return;

        target.AddComponent(type);
    }
}

