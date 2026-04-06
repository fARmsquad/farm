# Feature Spec: Farm Layout (Greybox) — L1-001

## Summary
Block out the farm environment with placeholder geometry. Ground plane, paths,
plot positions, structure positions. Built via MCP (manage_gameobject). Testable
in editor Scene view and Play mode with free camera.

## User Story
As a developer, I want a greyboxed farm layout so that I can validate spatial
relationships and scale before adding art assets.

## Acceptance Criteria
- [ ] Ground plane (20m x 20m, grass material)
- [ ] Dirt paths connecting areas (ProBuilder or planes with dirt material)
- [ ] Perimeter fence (cube primitives, modular segments)
- [ ] Spawn/start position marker (empty GameObject at south entrance)
- [ ] Crop plot positions (6 empty plots in 2x3 grid, 1m x 1m each, center-farm)
- [ ] Tool rack position marker (east side)
- [ ] Barn position marker (north)
- [ ] Well position marker (west)
- [ ] Expansion zone boundaries (east, west, north — visible fence/fog)
- [ ] Basic directional light (sun)
- [ ] Procedural skybox (URP)

## Layout Sketch
```
  N
  |
  [Barn]
  |
  [Well] --- [2x3 Plot Grid] --- [Rack]
  |
  [Spawn Point / Entrance]
  S
```

Expansion zones: east, west, north (beyond fence, fogged)

## VR Interaction Model
Not applicable — greybox layout. Free camera (WASD + mouse) for testing.

## Edge Cases
- Plots must be reachable from tool rack in reasonable walking distance
- Well must be accessible from all plots
- Expansion zones must be visually distinct (fogged or fenced)

## Performance Impact
- Primitive geometry only — negligible draw calls
- Quest budget: trivially within bounds

## Dependencies
- None — first thing built

## Out of Scope
- Art assets (L1-003)
- Interactable components (Layer 2)
- VR rig (Layer X0)

---

## Technical Plan

### Architecture
- **No Core/ classes** — scene assembly only
- **MCP tools**: manage_gameobject (create primitives), manage_components (colliders, markers)
- **Scene**: Assets/_Project/Scenes/FarmMain.unity

### Build Approach
```
MCP manage_gameobject → primitives → parented hierarchy
Farm/Ground, Farm/Structures, Farm/Plots, Farm/Markers
```

### Testing Strategy
- **Editor**: Free camera fly-around in Play mode
- **Verify**: scale feels right, distances walkable, layout readable
- **No automated tests** (visual/spatial verification)

---

## Task Breakdown

### Task 1: Ground Plane
- **Type**: Scene primitive
- **Action**: Create 20x20 plane, apply grass material, position at origin
- **Depends on**: nothing
- **Acceptance**: Ground plane visible at (0,0,0), grass material applied

### Task 2: Path Layout
- **Type**: Scene primitives
- **Action**: Create dirt-material planes connecting barn, well, plots, rack, entrance
- **Depends on**: Task 1
- **Acceptance**: All key areas visually connected by dirt path strips

### Task 3: Perimeter Fence
- **Type**: Scene primitives
- **Action**: Modular cube fence segments around farm boundary (20x20 perimeter)
- **Depends on**: Task 1
- **Acceptance**: Continuous fence enclosing farm, no gaps at corners

### Task 4: Structure Markers
- **Type**: Empty GameObjects
- **Action**: Place markers — barn (0,0,8), well (-6,0,0), rack (6,0,0)
- **Depends on**: Task 1
- **Acceptance**: Three named markers visible in hierarchy under Farm/Markers

### Task 5: Plot Grid
- **Type**: Scene primitives + tags
- **Action**: 6 plots (2 columns x 3 rows), 1m x 1m, centered at farm, tagged "CropPlot"
- **Depends on**: Task 1
- **Acceptance**: 6 plot GameObjects in Farm/Plots, all tagged, evenly spaced

### Task 6: Spawn Point
- **Type**: Empty GameObject + tag
- **Action**: Empty GameObject at (0,0,-8), tagged "SpawnPoint"
- **Depends on**: Task 1
- **Acceptance**: SpawnPoint marker at south entrance, tagged correctly

### Task 7: Expansion Zones
- **Type**: Scene primitives + markers
- **Action**: Fenced/fogged areas east, west, north with zone boundary markers
- **Depends on**: Task 3
- **Acceptance**: Three expansion zones visually distinct, each has boundary marker

### Task 8: Lighting
- **Type**: Scene setup
- **Action**: Directional light (warm white, 45 deg elevation, soft shadows) + URP procedural skybox
- **Depends on**: nothing
- **Acceptance**: Scene lit with warm sun, no harsh shadows, skybox visible

### Task 9: Hierarchy Cleanup
- **Type**: Scene organization
- **Action**: Organize all objects under Farm/ parent — Farm/Ground, Farm/Structures, Farm/Plots, Farm/Markers
- **Depends on**: Tasks 1–8
- **Acceptance**: Hierarchy clean, all objects named clearly, no orphaned objects
