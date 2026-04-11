# Feature Spec: World Farming Playable Slice — L2-011

## Summary
Turn `WorldMain` into a focused playable farming slice by reducing the active
world to the farm plots, farmhouse, and chicken coop area. The slice must keep
the existing farming mechanics, integrate progression and atmosphere directly in
world space, expose developer shortcuts for testing, and provide clear
reference surfaces for both solo debugging and team demos.

## User Story
As a player, I want the world scene to feel like one clean farming destination
 instead of a cluttered prototype map, so I can move between the plots, the
house, and the coop and understand exactly how farming progression, weather,
and atmosphere work together.

## Acceptance Criteria
- [ ] `WorldMain` boots into a focused playable slice that keeps the farmhouse,
      crop plots, and chicken coop while deactivating unrelated clutter.
- [ ] The slice has explicit zones for `Farm Plots`, `Farm House`, and
      `Chicken Coop`.
- [ ] Existing farming actions still work in world space with the current
      first-person plot interaction flow.
- [ ] Progression is active in `WorldMain`: coins, XP, levels, watering upgrade
      tier, expansion hook state, and spendable skill points.
- [ ] Atmosphere is active in `WorldMain`: weather-aware ambience, weather
      particles, zone-specific educational or reference surfaces, and
      persistence for farming progression.
- [ ] Dev shortcuts exist for weather, save/load, selling harvests, upgrading,
      granting test resources, and spending skill points.
- [ ] The slice has an on-screen GUI reference showing current zone, farming
      status, and available shortcuts.
- [ ] Season integration is optional but automatic: if the season driver is
      present, progression and atmosphere surfaces reflect it without requiring
      code changes.

## Product Intent
This story is not about adding more map content. It is about making the current
world usable and legible. The world should feel like a believable farming
destination with three meaningful spaces:
- plots for tending crops
- house for planning, selling, saving, and upgrades
- coop for chicken-side flavor and future expansion hooks

## VR Interaction Model
- Plot interactions remain look-at + key-driven for desktop validation.
- Zone interactions use readable in-world or HUD reference text instead of
  floating debug windows everywhere.
- No new locomotion model is introduced. The existing first-person explorer
  remains the transport layer.

## Dependencies
- `L2-000` Farming Foundation & Build Sequence
- Existing world farming bootstrap in `WorldSceneBootstrap`
- Existing day, weather, and season drivers

## Out of Scope
- New terrain sculpting
- New town gameplay
- Multiplayer or social loops
- A full house interior gameplay loop
- A full chicken-management system inside `WorldMain`
- Hard dependence on seasons being complete before this slice ships

## Design Rules
- Keep the world focused. If it is not plots, house, coop, player, or required
  support systems, it should not stay active in the playable slice.
- Progression must deepen the farming loop, not replace it.
- Atmosphere must stay low-stress and readable.
- Season support must be optional and auto-detected.
- Developer testing must be first-class through shortcuts and GUI references.

## Technical Plan
- Extend the existing world farming bootstrap rather than create a second world
  bootstrap path.
- Add pure C# progression state and services under `Core/Farming/`.
- Add thin MonoBehaviour world controllers for:
  - playable-slice pruning
  - zone installation
  - progression wiring
  - atmosphere and reference overlays
  - developer shortcuts
- Keep persistence limited to farming progression and related world-farming
  state.
- Use `Resources` for any runtime-loaded farming atmosphere references that
  cannot rely on scene-assigned assets.

## Testing Strategy
- EditMode tests for pure progression logic, persistence round-trip, and world
  bootstrap expectations.
- Manual editor playtest in `WorldMain` for:
  - visible slice pruning
  - plot interaction
  - zone changes
  - weather shortcuts
  - sell/upgrade/skill shortcuts
  - persistence save/load
  - atmosphere and reference overlays

## Task Breakdown
1. Add failing tests for progression state and world playable-slice bootstrap.
2. Implement pure progression and snapshot logic.
3. Extend world bootstrap with slice pruning and zones.
4. Wire progression into world farming runtime.
5. Add atmosphere, persistence, and GUI reference systems.
6. Add developer shortcuts and documentation.
