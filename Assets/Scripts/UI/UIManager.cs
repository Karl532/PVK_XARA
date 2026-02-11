using System;
using UnityEngine;
using KeyBinding;
using KeyBinding.Handlers;

public class UIManager : MonoBehaviour
{
    [Header("Key Binds")]
    [SerializeField]
    [Tooltip("Implements keybind actions (ToggleWorkspacePlacement, etc.). Add to this GameObject and drag here.")]
    private KeyBindActions keyBindActions;

    [Header("Theme")]
    [SerializeField]
    private UIThemeConfig themeConfig;

    private GameObject canvasObject;

    private void Start()
    {
        BuildUI();
    }

    public void RebuildUI()
    {
        int tabToRestore = -1;
        Vector3 positionToRestore = Vector3.zero;
        Quaternion rotationToRestore = Quaternion.identity;
        Vector3 scaleToRestore = Vector3.one;
        bool hasTransformToRestore = false;

        if (canvasObject != null)
        {
            tabToRestore = GetActiveTabIndex(canvasObject);
            hasTransformToRestore = TrySaveCanvasTransform(
                canvasObject.transform,
                out positionToRestore,
                out rotationToRestore,
                out scaleToRestore);

            Destroy(canvasObject);
        }

        BuildUI();

        if (canvasObject != null)
        {
            if (hasTransformToRestore)
                RestoreCanvasTransform(canvasObject.transform, positionToRestore, rotationToRestore, scaleToRestore);

            if (tabToRestore >= 0)
                RestoreActiveTab(canvasObject, tabToRestore);
        }
    }

    private static int GetActiveTabIndex(GameObject canvas)
    {
        var tabSystem = canvas.GetComponentInChildren<UITabSystem>();
        return tabSystem != null ? tabSystem.GetActiveTabIndex() : -1;
    }

    private static bool TrySaveCanvasTransform(
        Transform canvasTransform,
        out Vector3 position,
        out Quaternion rotation,
        out Vector3 scale)
    {
        if (canvasTransform == null)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            scale = Vector3.one;
            return false;
        }

        position = canvasTransform.position;
        rotation = canvasTransform.rotation;
        scale = canvasTransform.localScale;
        return true;
    }

    private static void RestoreCanvasTransform(
        Transform canvasTransform,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale)
    {
        if (canvasTransform == null)
            return;

        canvasTransform.position = position;
        canvasTransform.rotation = rotation;
        canvasTransform.localScale = scale;
    }

    private static void RestoreActiveTab(GameObject canvas, int tabIndex)
    {
        var tabSystem = canvas.GetComponentInChildren<UITabSystem>();
        if (tabSystem == null)
            return;

        tabSystem.SelectTab(tabIndex);
    }

    private void BuildUI()
    {
        EnsureAuxiliaryHandlers();

        if (themeConfig == null)
        {
            Debug.LogWarning("UIManager: No UIThemeConfig assigned. Creating a temporary in-memory config with defaults.");
            themeConfig = ScriptableObject.CreateInstance<UIThemeConfig>();
        }

        bool useLightMode = GetUseLightModeFromSettings();

        canvasObject = UICanvasBuilder.BuildCanvas(transform, themeConfig);
        GameObject contentPanel = UIContentBuilder.BuildContent(canvasObject, themeConfig, useLightMode);

        UITabController.BuildTabs(transform, contentPanel.transform, themeConfig, useLightMode, keyBindActions);
    }

    private void EnsureAuxiliaryHandlers()
    {
        // Ensure keybind handlers and workspace bounds controller exist
        if (GetComponent<ToggleSettingsPanelHandler>() == null)
            gameObject.AddComponent<ToggleSettingsPanelHandler>();
        if (GetComponent<ToggleBlockPlacementHandler>() == null)
            gameObject.AddComponent<ToggleBlockPlacementHandler>();
        if (GetComponent<WorkspacePlacementController>() == null)
            gameObject.AddComponent<WorkspacePlacementController>();
    }

    private bool GetUseLightModeFromSettings()
    {
        var settingsManager = SettingsManager.Instance;
        if (settingsManager == null || settingsManager.settings == null)
            return false;

        return settingsManager.settings.uiLightMode;
    }
}
