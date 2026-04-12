# Research Notes — FarmSim VR

## How This File Works
Every feature and non-trivial task gets a research brief appended here
BEFORE the technical plan or implementation begins. This is populated by
the `.ai/skills/unity-research.md` skill using WebSearch + WebFetch.

Codex agents (which have no internet) read this file to access research findings.

## Index
<!-- Append entries here as: - [Feature/Task Name](#anchor) — date -->
- [Starter Tool Discovery & Ability Unlocks](#research-starter-tool-discovery--ability-unlocks) — 2026-04-10
- [Tutorial Title Screen Slice Launcher](#research-tutorial-title-screen-slice-launcher) — 2026-04-11
- [Horse Training Title Slice](#research-horse-training-title-slice) — 2026-04-11

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

## Research: Tutorial Title Screen Slice Launcher
**Date**: 2026-04-11
**Queries**:
- `site:docs.unity3d.com SceneManager.LoadScene scene name build settings`
- `site:docs.unity3d.com EditorSceneManager playModeStartScene`
- `site:docs.unity3d.com EditorBuildSettingsScene EditorBuildSettings.scenes`

### Recommended Packages
No packages found — custom implementation required.

### Key Patterns Found
- Keep runtime scene launches on stable scene names already present in Build Settings. Unity's `SceneManager.LoadScene` accepts scene names or paths, but using one shared catalog avoids divergence between UI launchers and scene flow. Source: https://docs.unity3d.com/ru/current/ScriptReference/SceneManagement.SceneManager.LoadScene.html
- Use `EditorSceneManager.playModeStartScene` to force a predictable development entry scene when quick testing matters more than the currently open editor scene. Source: https://docs.unity3d.com/es/2021.1/ScriptReference/SceneManagement.EditorSceneManager-playModeStartScene.html
- Use `EditorBuildSettingsScene` plus `EditorBuildSettings.scenes` to script build-order updates from editor tooling instead of hand-maintaining the Build Settings list. Source: https://docs.unity3d.com/cn/2022.3/ScriptReference/EditorBuildSettingsScene.html and https://docs.unity3d.com/es/ScriptReference/EditorBuildSettings-scenes.html

### Recommended Approach
Keep the ordered tutorial scenes in one shared catalog that carries both scene
names and asset paths. Use that catalog to drive the title-screen slice
launcher, the editor play-mode start scene, and build-settings ordering so the
development launcher and the actual tutorial sequence cannot drift apart.

### Code Reference
```csharp
foreach (var slice in SceneWorkCatalog.TutorialOrderedScenes)
{
    AddButton(slice.NumberLabel, slice.SceneName);
    buildScenes.Add(slice.ScenePath);
}
```

### Gotchas & Pitfalls
- If scene names are duplicated or renamed without updating build settings, `LoadScene` can resolve the wrong scene or fail. Keep build-order updates scripted from the same catalog that feeds the launcher.
- A play-mode start scene that bypasses the title entry makes a slice launcher ineffective for editor testing. If the launcher is the intended dev entry, point `playModeStartScene` at the title scene.

### Quest/Mobile Considerations
- Keep the slice launcher editor/dev only. It is useful for iteration, but it
  should not become required runtime UI for headset players.

### Sources
1. [Unity Scripting API: SceneManager.LoadScene](https://docs.unity3d.com/ru/current/ScriptReference/SceneManagement.SceneManager.LoadScene.html) — Confirms scene-name/path loading behavior and the need for Build Settings alignment.
2. [Unity Scripting API: EditorSceneManager.playModeStartScene](https://docs.unity3d.com/es/2021.1/ScriptReference/SceneManagement.EditorSceneManager-playModeStartScene.html) — Confirms editor-time start-scene override is the supported way to standardize play-mode entry.
3. [Unity Scripting API: EditorBuildSettingsScene](https://docs.unity3d.com/cn/2022.3/ScriptReference/EditorBuildSettingsScene.html) — Confirms scripted build-scene entries are the supported automation path.
4. [Unity Scripting API: EditorBuildSettings.scenes](https://docs.unity3d.com/es/ScriptReference/EditorBuildSettings-scenes.html) — Confirms the build-scene list is writable from editor scripts.

## Research: Horse Training Title Slice
**Date**: 2026-04-11
**Queries**:
- `site:docs.unity3d.com SceneManager.LoadScene Unity 6`
- `site:docs.unity3d.com Character Controller component reference Unity 6`
- `site:docs.unity3d.com Collider.OnTriggerEnter Unity 6`

### Recommended Packages
No new packages found. The repo's current first-person + scene-loading stack is enough for a greybox horse-training slice.

### Key Patterns Found
- Keep the slice on the existing `CharacterController`-style first-person rig for grounded tutorial movement instead of introducing rigidbody locomotion for a one-scene prototype. Unity's manual explicitly positions `CharacterController` for first-person or third-person control that should not rely on rigidbody physics. Source: https://docs.unity3d.com/Manual/class-CharacterController.html
- Launch the slice by stable scene name or path that is already present in Build Settings. Unity's `SceneManager.LoadScene` resolves names from Build Settings and warns that duplicate names should use the full path to avoid ambiguity. Source: https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.LoadScene.html
- Use trigger volumes to mark treat pickups, jump rails, and slalom checkpoints. Unity's `Collider.OnTriggerEnter` flow is the built-in pattern for overlap-driven gameplay events without requiring collision impulses. Source: https://docs.unity3d.com/ScriptReference/Collider.OnTriggerEnter.html

### Recommended Approach
Model the horse-training beat as a self-contained greybox scene launched from the title screen's shared scene catalog, but keep it out of the mandatory linear tutorial order. Put the sequence logic in a pure C# service that tracks storyboard phases (`Setup`, `GuidedWalk`, `Jumping`, `Slalom`, `Success`, `Failure`) and let a thin MonoBehaviour scene controller translate trigger hits and button presses into service calls.

This preserves the repo's Core-first architecture, gives the title screen a real launch target, and keeps the storyboarded experience readable without rewriting the existing intro-to-farm sequence.

### Gotchas & Pitfalls
- Do not add the horse slice by hardcoding one more title-screen button in isolation. The launch entry, build-settings inclusion, and scene metadata should come from the same shared catalog so the title screen does not drift from editor tooling.
- Do not put the training progression rules in the MonoBehaviour only. If failure rules, treat counts, or slalom balance live only in scene code, they will be hard to test and harder to tune.
- Avoid force-driven horse or player physics for this slice. The storyboard needs readable gates and failure states, not systemic simulation complexity.

### Sources
1. [Unity Manual: Character Controller component reference](https://docs.unity3d.com/Manual/class-CharacterController.html) — Supports staying on the current grounded first-person rig for the slice.
2. [Unity Scripting API: SceneManager.LoadScene](https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.LoadScene.html) — Supports title-screen launch wiring through a shared scene catalog and Build Settings.
3. [Unity Scripting API: Collider.OnTriggerEnter](https://docs.unity3d.com/ScriptReference/Collider.OnTriggerEnter.html) — Supports trigger-driven checkpoint and pickup interactions for the training course.
