# UI Folder UML

This document summarizes the `Assets/Scripts/UI` folder structure and the main relationships between UI classes. Diagrams are Mermaid-based so they render in IDEs that support Mermaid.

**Folder Map**
```mermaid
flowchart TB
    UI[Assets/Scripts/UI]
    UI --> Core[Core: UIManager, UICanvasBuilder, UIContentBuilder, UITabController, UIThemeConfig]
    UI --> Elements[Elements: controls + styling]
    UI --> Tabsystem[Tabsystem: UITabSystem, Tabs/*]
    UI --> Utils[Utils: UIComponentHelper, UILayoutFactory, UIPrimitives]
    UI --> Scripts[Scripts: UIFollowCamera, UIKeepUpright, SpatialKeyboardPlacement]
    Elements --> Checkbox[UICheckbox/*]
    Elements --> Dropdown[UIDropdown/*]
    Elements --> FolderViewer[UIFolderViewer/*]
    Elements --> InputField[UIInputField/*]
    Elements --> Rebindable[UIRebindableKeyBinding/*]
    Elements --> Slider[UISlider/*]
    Elements --> TabButton[UITabButton.cs]
    Elements --> Rounded[RoundedImage.cs]
```

**UI Build Pipeline**
```mermaid
flowchart LR
    UIManager --> UICanvasBuilder
    UIManager --> UIContentBuilder
    UIManager --> UITabController
    UICanvasBuilder --> UIThemeConfig
    UIContentBuilder --> UIThemeConfig
    UITabController --> UIThemeConfig
    UITabController --> UITabSystem
```

**Tab System**
```mermaid
classDiagram
    class UITabSystem {
        +Build(parent, style, tabs)
        +AddTab(button, content)
        +SelectTab(index)
        +GetActiveTabIndex()
    }
    class UITabButton {
        +CreateTabButton(label, active, inactive, size, fontSize, textColor)
        +SetActive(active)
        +GetButton()
    }
    class UIStyle {
        +Color accentColor
        +Color inactiveColor
        +Color textColor
        +float cornerRadius
        +bool useShadows
        +KeyBindActions keyBindActions
    }
    class TabDefinition {
        +string label
        +Func createContent
    }

    class UITabController {
        +BuildTabs(uiRoot, contentRoot, themeConfig, useLightMode, keyBindActions)
    }

    class WorkspaceSettingsTab
    class ModelSettingsTab
    class TrackingTab
    class SculptTab
    class UICustomizationTab
    class FilesTab
    class DebugTab

    UITabController --> UITabSystem : Build()
    UITabController --> TabDefinition : builds list
    UITabSystem o--> UITabButton
    UITabSystem o--> TabDefinition
    TabDefinition --> WorkspaceSettingsTab : createContent
    TabDefinition --> ModelSettingsTab : createContent
    TabDefinition --> TrackingTab : createContent
    TabDefinition --> SculptTab : createContent
    TabDefinition --> UICustomizationTab : createContent
    TabDefinition --> FilesTab : createContent
    TabDefinition --> DebugTab : createContent (debug only)
```

**Elements and Styling**
```mermaid
classDiagram
    class UICheckbox
    class UICheckboxStyling
    class UIDropdown
    class UIDropdownStyling
    class UIInputField
    class UIInputFieldStyling
    class UIInputFieldKeyboard
    class UIInputFieldKeyboardLayoutSwitcher
    class UIRebindableKeyBinding
    class UIRebindableKeyBindingStyling
    class UISlider
    class UISliderStyling
    class UIFolderViewer
    class UIFolderViewerStyling
    class UIFolderViewerItemData
    class UIFolderViewerItemSelectionAnim
    class UIFolderViewerItemAccentBar
    class RoundedImage
    class UITabButton
    class UIPrimitives
    class UIComponentHelper

    UICheckbox --> UICheckboxStyling
    UIDropdown --> UIDropdownStyling
    UIInputField --> UIInputFieldStyling
    UIInputField --> UIInputFieldKeyboard
    UIInputFieldKeyboardLayoutSwitcher --> UIInputFieldKeyboard : optional add-on
    UIRebindableKeyBinding --> UIRebindableKeyBindingStyling
    UISlider --> UISliderStyling
    UIFolderViewer --> UIFolderViewerStyling
    UIFolderViewerStyling --> UIFolderViewerItemData
    UIFolderViewerStyling --> UIFolderViewerItemSelectionAnim
    UIFolderViewerItemAccentBar ..> UIFolderViewer : unused helper

    UICheckboxStyling --> UIPrimitives
    UIDropdownStyling --> UIPrimitives
    UIInputFieldStyling --> UIPrimitives
    UIRebindableKeyBindingStyling --> UIPrimitives
    UISliderStyling --> UIPrimitives
    UIFolderViewerStyling --> RoundedImage

    UICheckbox --> UIComponentHelper
    UIDropdown --> UIComponentHelper
    UIInputField --> UIComponentHelper
    UISlider --> UIComponentHelper
```

**Core Classes**
```mermaid
classDiagram
    class UIManager {
        -KeyBindActions keyBindActions
        -UIThemeConfig themeConfig
        +RebuildUI()
    }
    class UICanvasBuilder {
        +BuildCanvas(parent, themeConfig)
    }
    class UIContentBuilder {
        +BuildContent(canvasObject, themeConfig, useLightMode)
    }
    class UIThemeConfig {
        +ThemeVariant darkTheme
        +ThemeVariant lightTheme
        +GetTheme(useLightTheme)
    }
    class UITabController {
        +BuildTabs(uiRoot, contentRoot, themeConfig, useLightMode, keyBindActions)
    }
    class UITabSystem

    UIManager --> UICanvasBuilder
    UIManager --> UIContentBuilder
    UIManager --> UITabController
    UIManager --> UIThemeConfig
    UIContentBuilder --> UIThemeConfig
    UICanvasBuilder --> UIThemeConfig
    UITabController --> UITabSystem
```

**Notes**
- `UIManager` builds the world-space canvas, content panel, and tab system, and rebuilds the UI when theme settings change.
- `UITabController` wires the tab list and debug tab inclusion using `SettingsManager`.
- `UIFolderViewerItemAccentBar` is defined but not referenced by current UI construction code.
- `UIInputFieldKeyboardLayoutSwitcher` is optional and only works when an `XRKeyboardDisplay` exists on the same object.
