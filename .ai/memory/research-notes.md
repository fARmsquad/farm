# Research Notes — FarmSim VR

## How This File Works
Every feature and non-trivial task gets a research brief appended here
BEFORE the technical plan or implementation begins. This is populated by
the `.ai/skills/unity-research.md` skill using WebSearch + WebFetch.

Codex agents (which have no internet) read this file to access research findings.

## Index
<!-- Append entries here as: - [Feature/Task Name](#anchor) — date -->
- [Starter Tool Discovery & Ability Unlocks](#research-starter-tool-discovery--ability-unlocks) — 2026-04-10

---

## Investigations
(will accumulate as technical questions arise)

## Experiments
(will track proof-of-concept results)

---
<!-- Research briefs are appended below this line -->

## Research: Starter Tool Discovery & Ability Unlocks
**Date**: 2026-04-10
**Queries**:
- `site:docs.unity3d.com "Character Controller component reference" Unity`
- `site:docs.unity3d.com "Physics.Raycast" Unity`
- `site:docs.unity3d.com "Input System Quickstart Guide" Unity`

### Recommended Packages
| Package | Source | Why | Status |
|---------|--------|-----|--------|
| `com.unity.inputsystem` | Repo manifest + Unity docs | Already installed and sufficient for the current keyboard/mouse playable slice | In Use |
| `com.unity.xr.interaction.toolkit` | Unity Package Docs | Not required for this feature's current 3D scope; keep as a later follow-up if tool handling moves to XR props | Deferred |
| `com.unity.xr.openxr` | Unity Manual | Also deferred until the feature graduates from the current 3D build to headset-specific interaction | Deferred |

Current repo note: `Packages/manifest.json` already includes `com.unity.inputsystem`, and the playable world already uses a `CharacterController`-based `FirstPersonExplorer` plus raycast-driven `FarmPlotInteractionController`.

### Key Patterns Found
- Keep the current playable slice on `CharacterController` movement instead of Rigidbody-based player physics. Unity's manual explicitly positions `CharacterController` as the right fit for first-person control that should move cleanly without force-driven behavior. Source: https://docs.unity3d.com/Manual/class-CharacterController.html
- Use center-screen or cursor-driven raycasts with distance and layer filtering to determine what tool or plot the player is targeting. Unity's `Physics.Raycast` API supports max distance, layer masks, and hit details, which matches the repo's existing prompt controller pattern. Source: https://docs.unity3d.com/ScriptReference/Physics.Raycast.html
- Keep the input stack on the new Input System and formalize actions over time. Unity's Input System quickstart recommends project-wide action assets and grouped action maps, which fits a future cleanup of the current direct keyboard checks without changing feature scope right now. Source: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/QuickStartGuide.html

### Recommended Approach
Model tool collection as a pure Core unlock-and-gating system that exposes farming abilities (`PrepareSoil`, `WaterPlot`, `HarvestCrop`, and so on) to higher-level callers. For the current playable slice, tool discovery should plug into the existing first-person controller, a simple tool pickup prompt, and the current plot-action prompt flow rather than introducing equipped XR props.

This keeps the unlock sequence fully testable in EditMode, preserves the repo's "Core first, wrappers second" architecture, and gives the player a readable 3D farm loop now while leaving carried XR tool interactions for a separate later spec.

### Code Reference
```csharp
if (!toolUnlockState.HasAbility(FarmAbility.WaterPlot))
{
    return ToolGateResult.Blocked(ToolId.WateringCan, "Recover the watering can from the well.");
}

var result = wateringService.Apply(plotId, amount);
return ToolGateResult.FromAbilityResult(result);
```

### Gotchas & Pitfalls
- Do not put unlock checks in scene scripts only. If gating lives only in MonoBehaviours, debug/editor flows and save/load paths will drift from the real rules. Keep unlock truth in Core.
- Limit raycasts deliberately. Unity's `Physics.Raycast` docs emphasize `maxDistance` and `layerMask`; broad scene-wide casts will make tool focus noisy and fragile.
- Tune the existing `CharacterController` rather than replacing it. Unity's manual calls out skin width, min move distance, and step offset as the first controls to fix if the player gets stuck or jitters.
- Avoid input drift. The repo already uses direct `Keyboard.current` checks in places; if this feature adds more keys, document them centrally or move toward an action-map-backed layer instead of scattering bindings further.

### Current 3D Considerations
- Prefer focused raycasts from the camera center and a small interaction radius to keep prompts readable.
- Avoid large always-on HUD overlays; reuse the current prompt strip and lightweight world highlights.
- Keep tool feedback event-driven. No per-frame scene scans for rack hints or unlock effects.

### Sources
1. [Unity Manual: Character Controller component reference](https://docs.unity3d.com/Manual/class-CharacterController.html) — Supports the current first-person exploration model already present in the repo.
2. [Unity Scripting API: Physics.Raycast](https://docs.unity3d.com/ScriptReference/Physics.Raycast.html) — Matches the current camera-to-plot targeting pattern and informs tool pickup focus.
3. [Unity Input System Quickstart Guide](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/QuickStartGuide.html) — Reinforces staying on the new Input System for the current 3D playable slice.
