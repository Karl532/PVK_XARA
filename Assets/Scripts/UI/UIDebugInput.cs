using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.XR.CoreUtils;

public class UIDebugInput : MonoBehaviour
{
    private float logInterval = 2f;
    private float nextLogTime;

    void Update()
    {
        if (Time.time < nextLogTime) return;
        nextLogTime = Time.time + logInterval;

        // Check EventSystem
        var es = EventSystem.current;
        if (es == null)
        {
            Debug.LogError("[UIDebug] No EventSystem found!");
            return;
        }

        // Log all input modules
        var allModules = es.GetComponents<BaseInputModule>();
        foreach (var m in allModules)
            Debug.Log($"[UIDebug] Input module: {m.GetType().Name}, active: {m.isActiveAndEnabled}");

        // Check InputSystemUIInputModule
        var inputModule = es.GetComponent<InputSystemUIInputModule>();
        if (inputModule != null)
        {
            Debug.Log($"[UIDebug] InputSystemUIInputModule active: {inputModule.isActiveAndEnabled}");
            Debug.Log($"[UIDebug] XR Tracking Origin: {(inputModule.xrTrackingOrigin != null ? inputModule.xrTrackingOrigin.name : "NULL")}");

            var actionsAsset = inputModule.actionsAsset;
            if (actionsAsset != null)
            {
                var trackedPosAction = actionsAsset.FindAction("UI/TrackedDevicePosition");
                var trackedRotAction = actionsAsset.FindAction("UI/TrackedDeviceOrientation");
                var clickAction = actionsAsset.FindAction("UI/Click");

                if (trackedPosAction != null)
                    Debug.Log($"[UIDebug] TrackedDevicePosition: {trackedPosAction.ReadValue<Vector3>()}");
                if (trackedRotAction != null)
                    Debug.Log($"[UIDebug] TrackedDeviceOrientation: {trackedRotAction.ReadValue<Quaternion>().eulerAngles}");
                if (clickAction != null)
                    Debug.Log($"[UIDebug] Click phase: {clickAction.phase}");
            }
        }

        // Check Canvas setup
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.WorldSpace)
            {
                Debug.Log($"[UIDebug] Canvas '{c.name}': worldCamera={(c.worldCamera != null ? c.worldCamera.name : "NULL")}, pos={c.transform.position}");

                var gr = c.GetComponent<GraphicRaycaster>();
                var tdgr = c.GetComponent<TrackedDeviceGraphicRaycaster>();
                Debug.Log($"[UIDebug]   GraphicRaycaster: {(gr != null ? "YES" : "no")}, TrackedDeviceGraphicRaycaster: {(tdgr != null ? "YES" : "no")}");
            }
        }

        // Check CanvasGroup
        var canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            Debug.Log($"[UIDebug] CanvasGroup: alpha={canvasGroup.alpha}, interactable={canvasGroup.interactable}, blocksRaycasts={canvasGroup.blocksRaycasts}");

        // EventSystem state
        Debug.Log($"[UIDebug] CurrentSelected: {(es.currentSelectedGameObject != null ? es.currentSelectedGameObject.name : "none")}");

        // Manual 3D raycast from controller toward UI
        var actionsAsset2 = inputModule?.actionsAsset;
        if (actionsAsset2 != null)
        {
            var posAction = actionsAsset2.FindAction("UI/TrackedDevicePosition");
            var rotAction = actionsAsset2.FindAction("UI/TrackedDeviceOrientation");
            if (posAction != null && rotAction != null)
            {
                Vector3 pos = posAction.ReadValue<Vector3>();
                Quaternion rot = rotAction.ReadValue<Quaternion>();

                // Transform from tracking space to world space
                var xrOrigin = FindFirstObjectByType<XROrigin>();
                if (xrOrigin != null)
                {
                    pos = xrOrigin.transform.TransformPoint(pos);
                    rot = xrOrigin.transform.rotation * rot;
                }

                Vector3 forward = rot * Vector3.forward;
                Ray ray = new Ray(pos, forward);

                // Check if ray hits the canvas plane
                foreach (var c in canvases)
                {
                    if (c.renderMode != RenderMode.WorldSpace) continue;
                    Plane canvasPlane = new Plane(c.transform.forward, c.transform.position);
                    if (canvasPlane.Raycast(ray, out float dist))
                    {
                        Vector3 hitPoint = ray.GetPoint(dist);
                        Debug.Log($"[UIDebug] Controller ray hits canvas plane at {hitPoint} (dist={dist:F2}m)");
                        Debug.DrawRay(pos, forward * dist, Color.green, logInterval);
                    }
                    else
                    {
                        Debug.Log($"[UIDebug] Controller ray does NOT hit canvas plane. Ray origin={pos}, dir={forward}");
                        Debug.DrawRay(pos, forward * 5f, Color.red, logInterval);
                    }
                }
            }
        }

        // EventSystem raycast test using tracked device pointer
        var pointerData = new PointerEventData(es);
        pointerData.position = new Vector2(Screen.width / 2f, Screen.height / 2f);
        var results = new List<RaycastResult>();
        es.RaycastAll(pointerData, results);
        Debug.Log($"[UIDebug] EventSystem.RaycastAll from screen center: {results.Count} hits");
        for (int i = 0; i < Mathf.Min(results.Count, 3); i++)
            Debug.Log($"[UIDebug]   Hit: {results[i].gameObject.name}");
    }
}
