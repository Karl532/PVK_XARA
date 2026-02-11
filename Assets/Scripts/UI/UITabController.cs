using System;
using UnityEngine;
using KeyBinding;
using KeyBinding.Handlers;

public static class UITabController
{
    public static void BuildTabs(Transform uiRoot, Transform contentRoot, UIThemeConfig themeConfig, bool useLightMode, KeyBindActions keyBindActions)
    {
        if (uiRoot == null) throw new ArgumentNullException(nameof(uiRoot));
        if (contentRoot == null) throw new ArgumentNullException(nameof(contentRoot));
        if (themeConfig == null) throw new ArgumentNullException(nameof(themeConfig));

        var theme = themeConfig.GetTheme(useLightMode);

        // Keybind registry root (parented to uiRoot so it stays active; bindings work even when UI is hidden)
        GameObject keyBindRoot = new GameObject("KeyBindRegistry");
        keyBindRoot.transform.SetParent(uiRoot, false);
        KeyBindRegistry.SetRoot(keyBindRoot.transform);

        var actions = keyBindActions != null ? keyBindActions : uiRoot.GetComponent<KeyBindActions>();
        if (actions == null) actions = uiRoot.gameObject.AddComponent<KeyBindActions>();

        UIStyle style = new UIStyle
        {
            accentColor = theme.accentColor,
            inactiveColor = theme.inactiveColor,
            textColor = theme.textColor,
            cornerRadius = themeConfig.cornerRadius,
            useShadows = themeConfig.useShadows,
            keyBindActions = actions
        };

        UITabSystem.Build(contentRoot, style,
            new TabDefinition { label = "Workspace", createContent = WorkspaceSettingsTab.Create },
            new TabDefinition { label = "Model", createContent = ModelSettingsTab.Create },
            new TabDefinition { label = "Tracking", createContent = TrackingTab.Create },
            new TabDefinition { label = "UI", createContent = UICustomizationTab.Create },
            new TabDefinition { label = "Load model", createContent = FilesTab.Create }
        );
    }
}

