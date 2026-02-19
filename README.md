# PVK_XARA

AR sculpting on Quest 3 using environment depth to build and visualize SDF volumes.

**Platform**
- Quest 3 only (XR runtime).

**High-Level Pipeline**
1. **Depth capture** via `IDepthProvider` (Quest 3 implementation: `Quest3DepthProvider`).
2. **Orchestration** in `SdfDepthOrchestrator`:
   - Resolves workspace + model.
   - Subscribes to depth provider point clouds.
   - Feeds point clouds into `SdfGenerationSystem`.
   - Receives `SdfVisualizationData` from `SdfGenerationSystem` (needed because local SDF content varies over time) and forwards it to `SdfVisualizationController`.
3. **SDF build** in `SdfGenerationSystem`:
   - Converts points to workspace space.
   - Builds global/local TSDF volumes (core pipeline).
4. **Visualization** via `SdfDepthOrchestrator` → `SdfVisualizationController`:
   - `SdfDepthOrchestrator` subscribes to `SdfGenerationSystem.VisualizationDataUpdated`.
   - It forwards those updates to `SdfVisualizationController.OnVisualizationData(...)`.
   - The controller uses renderers registered in `SdfRendererRegistry`.
   - Each renderer implements `ISdfRenderer.UpdateRenderer(...)`.
5. **UI** toggles update values in `Settings`, which drive runtime behavior.

**Required Scene Objects (runtime expectations)**
These must exist in a typical scene for the system to function:
- XR Origin
- XR Interaction Manager
- OVR Passthrough Layer
- UI Manager
- Calibration Origin
- Calibration Origin Controller
- Tracking Bridge
- Tracking Manager
- MRUK (Meta Room/Scene understanding)
- RuntimeModelLoader
- SdfDepthOrchestrator
- DepthManager
- SdfGenerationSystem

If any are missing, initialization may stall (see `SdfDepthOrchestrator.InitializeWhenReady` and workspace/model readiness checks).

**Settings & Configuration**
- `Settings` is the primary configuration asset.
- `Settings.GetActive()` is the singleton entry point for runtime settings.
- `SettingsManager` holds the live `Settings` asset in the scene.
- `SettingsPersistence` handles load/save of runtime settings.
- `SdfVisualizationConfig` is referenced from `Settings` via `Settings.sdfVisualizationConfig`.

**Debug & Logging**
- Centralized under `Assets/Scripts/DebugService/`:
  - `DebugService` (singleton) is the core logger (namespace `Assets.Scripts.Debug`).
- Visual debug renderers remain in their original folders (e.g., SDF visualization renderers).

**Depth System**
- `IDepthProvider` defines the depth provider interface.
- `DepthProviderFactory` creates the active provider based on `Settings.depthDevice`.
- Quest 3 implementation: `Quest3DepthProvider` + `OXDepthPointCloudAPI`.
- `DepthOverlayController` + `PointCloudOverlayRenderer` provide optional point cloud overlay rendering.

**SDF Generation**
- `SdfGenerationSystem` is the core pipeline:
  - Converts point clouds into workspace space.
  - Builds global/local TSDF volumes.
  - Exposes `SdfVisualizationData` for visualization.
  - Has debug/perf configuration via `SdfOrchestratorConfig`.
- `SdfDepthOrchestrator` is the runtime entry point that wires:
  - `DepthProviderFactory`
  - `SdfGenerationSystem`
  - `SdfVisualizationController`

**Visualization**
- `SdfDepthOrchestrator` listens to `SdfGenerationSystem.VisualizationDataUpdated` and forwards to `SdfVisualizationController`.
- Renderers are singletons created via `SdfRendererRegistry`:
  - `SdfBoundsRenderer`
  - `SdfFullSdfGridRenderer`
  - `SdfSculptGuideRenderer`
  - `SdfDepthErrorRenderer`
  - `SdfMatchOverlayRenderer`
- Each renderer pulls config from `Settings.GetActive().sdfVisualizationConfig`.

**UI**
- Tabs are created in `UITabController`.
- Debug-related toggles live in `DebugTab` and `SculptTab`.
- UI reads and writes values directly on `Settings` and `SdfVisualizationConfig`.

**Tracking / Calibration**
- `TrackingBridge` and `TrackingManager` manage tracking integration.
- `CallibrationOriginController` (note spelling) keeps origin aligned to settings.
- The SDF pipeline assumes workspace and model are aligned through the calibration origin.

**Workspace & Model**
- Workspace:
  - `WorkspaceBoundsUtility` and `WorkspacePlacementController` manage workspace placement and sizing.
  - `SdfDepthOrchestrator` uses `WorkspaceMovementState` to skip processing while moving.
- Model:
  - `RuntimeModelLoader` loads test and runtime models.
  - `RuntimeModelPositionUtility` applies settings-based transforms.

**Script Folder Tour**
- `Assets/Scripts/Debug`
  - Central debug/logging singleton + wrappers.
- `Assets/Scripts/Depth`
  - Depth providers, point cloud overlay, and Quest 3 depth API.
- `Assets/Scripts/KeyBinding`
  - Hotkey actions (e.g., toggling settings panel).
- `Assets/Scripts/RuntimeModel`
  - Model loading and positioning utilities.
- `Assets/Scripts/SdfDepthOrchestrator`
  - Entry point wiring depth → SDF → visualization.
- `Assets/Scripts/SdfGenerationSystem`
  - SDF build pipeline and core TSDF logic.
- `Assets/Scripts/SdfVisualization`
  - Visualization controller, renderers, and debug overlays.
- `Assets/Scripts/Settings`
  - Settings asset, manager, persistence, and config.
- `Assets/Scripts/Tracking`
  - Tracking integration and calibration control.
- `Assets/Scripts/UI`
  - UI system, tabs, and UI helpers.
- `Assets/Scripts/Utils`
  - General helpers (e.g., `ComponentUtility`).
- `Assets/Scripts/WorkspacePlacement`
  - Workspace placement tools and bounds utilities.

**/Resources**
- Runtime shaders and compute shaders live under `Assets/Resources/` and are loaded via `Resources.Load(...)`.
- Key paths used in Scripts:
  - `SDF/Compute/*` (SDF pipeline compute shaders)
  - `SDF/Shaders/*` (SDF visualization shaders)
  - `OXDepth/*` (Quest depth compute/shader resources)
