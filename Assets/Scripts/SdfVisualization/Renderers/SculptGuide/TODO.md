# Sculpt Guide TODO (End Goals)

## Vision
- Provide a stable, continuous surface guide that shows **add vs remove** volume between the **live depth surface** and the **target model surface**.
- Persist scans over time so the entire workspace can be covered from multiple viewpoints.
- Remain performant on Quest-class GPUs (interactive framerate).

## Current State (Baseline)
- Screen-space depth mesh (grid) rendered in world space.
- Surface colored by signed model TSDF.
- No temporal smoothing or persistence.
- No ray carving or accumulation yet.

## End Goals (Phased)

### Phase 1: Stabilize Live Surface (No Persistence)
- Add temporal smoothing of depth (EMA) in shader or compute.
- Configurable mesh density (`meshStep`).
- Handle invalid depth pixels (hole fill or discard).

### Phase 2: Persistent Surface Cache (Accumulate Scans)
- Add a low-res voxel cache (e.g. 128^3) for the sculpt guide surface.
- Use compute shader to **splat** depth samples into cache.
- Confidence/age per voxel to smooth and merge scans.
- Decay old voxels over time.

### Phase 3: Ray Carving (Removal & Cleanup)
- For each depth sample, carve free space along the camera ray. (implemented)
- Reduce confidence or clear voxels behind the surface. (implemented: clears voxels)
- Update only every N frames to control cost. (implemented via cache carve interval)
- Support downsampled depth to keep GPU cost acceptable. (implemented via cache carve downsample)

### Phase 4: “Between Volume” Visualization
- Raymarch from depth surface toward model surface using model TSDF. (implemented)
- Render only the segment between depth surface and model surface. (implemented)
- Color code:
  - Inside model (need add) = blue. (implemented)
  - Outside model (need remove) = red. (implemented)
  - Near surface = green. (implemented)
- Strict bounds: discard outside workspace. (implemented via TSDF bounds)

### Phase 5: Quality & Debugging
- Debug modes:
  - Show raw depth mesh.
  - Show cache occupancy/confidence.
  - Show ray carving paths.
  - Show sign/zero-crossings.
- GPU profiling and adaptive quality (steps/frame).

## Data Contracts / Integration Points
- Depth input:
  - Depth texture, resolution, inv depth VP, tracking->world, eye slice, flipY.
- Model TSDF input:
  - `_GlobalTsdf3D`, `_GlobalCorner`, `_GlobalSize`, `_GlobalMu`.
- Workspace:
  - `WorkspaceRoot`, `WorkspaceCorner`, `WorkspaceSize`.

## Performance Targets
- Quest-class GPU:
  - Cache update: <2 ms per frame (with downsampling).
  - Rendering pass: <1 ms.

## Open Questions
- Final cache resolution (64^3 vs 128^3)?
- Update frequency (every frame vs every N frames)?
- How aggressive should decay be?
- Do we want a â€œlockedâ€ mode to freeze scan?


