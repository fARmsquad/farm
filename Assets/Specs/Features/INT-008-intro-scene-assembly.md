# Feature Spec: Intro Scene Assembly — INT-008

## Summary
The complete intro scene (IntroScene.unity) that wires all cinematic systems together into a playable 4-act intro sequence. It contains greybox town and farm environments, placed NPCs, lighting presets, camera waypoints, dialogue assets, and the master CinematicSequence that orchestrates the full experience.

## User Story
As a player, I want to experience a scripted intro that introduces the farm world, key characters, and first mission so that I understand the setting and feel motivated to play.

## Acceptance Criteria
- [ ] IntroScene.unity contains two distinct areas: Town (Acts I-II) and Farm/Pen (Acts III-IV)
- [ ] Town area has 5-6 house primitives (cubes with different scales/colors), lamp post cylinders, ground-plane paths
- [ ] Town includes a player house with a simple bed interior (cube bed, door trigger)
- [ ] Farm area reuses existing AnimalPen layout and adds a stump (cylinder) for El Pollo, plus a barn backdrop (scaled cube)
- [ ] Night lighting preset active during Acts I-II (low ambient, point lights on lamp posts, blue-tinted directional)
- [ ] Dawn lighting preset active during Acts III-IV (warm directional, orange ambient, increased intensity)
- [ ] Lighting transitions are triggered by the CinematicSequencer SetLighting step
- [ ] NPC Tenpenny: blue capsule placed by a lamp post in town, with DialogueData for street dialogue and farm dialogue
- [ ] NPC Chop: small brown cube placed outside the pen area, with DialogueData for subtitles
- [ ] El Pollo: enhanced chicken GameObject with ElPolloController — 3 chase phases (normal speed, lateral dodge, tired/slow)
- [ ] ChaosMeter: screen-space UI bar that fills during the El Pollo chase, triggering phase transitions at thresholds
- [ ] DialogueData assets created for: Big Smoke V.O. (Act I narration), Niko V.O. (Act II narration), Tenpenny street dialogue, Tenpenny farm dialogue, Chop subtitles
- [ ] CameraPath assets created with waypoints for each Act's camera moves
- [ ] Master CinematicSequence asset wires Acts I through IV in order, using all systems
- [ ] Editor menu item "FarmSimVR > Setup Intro Scene" creates the scene from scratch or resets it
- [ ] Scene loads and plays through all 4 acts without errors when Play is pressed in Editor

## Edge Cases
- Scene entered without pressing Play (editor navigation): all systems remain idle until CinematicSequencer.Play is called
- Player walks out of town bounds during Acts I-II: invisible wall colliders prevent this
- El Pollo chase: if player catches El Pollo during dodge phase, skip to tired phase
- El Pollo chase: if player takes longer than 60 seconds, El Pollo automatically enters tired phase
- Lighting transition during active dialogue: dialogue remains visible (UI canvas renders on top)
- Missing audio clips: SimpleAudioManager logs warnings, sequence continues without audio

## Performance Impact
- Greybox primitives only: under 100 draw calls for the entire scene
- Two lighting presets use a single directional light with property changes (no baked lighting swap)
- El Pollo chase uses simple transform movement, no physics rigidbody
- ChaosMeter UI is a single Image fill — negligible overhead
- Target: 60+ FPS on any hardware capable of running Unity Editor

## Dependencies
- **Existing:** AnimalPen layout (from hunting feature), AnimalWander.cs, AnimalFleeBehavior.cs, CatchZone.cs, PlayerMovement.cs, ThirdPersonCamera.cs, GameManager.cs
- **New:** IntroScene.unity, ElPolloController.cs, ChaosMeter.cs, LightingPreset.cs (or inline on sequencer), all DialogueData assets, all CameraPath assets, master CinematicSequence asset, editor setup script
- **System refs:** INT-001 (ScreenEffects), INT-002 (SimpleAudioManager), INT-003 (DialogueManager), INT-004 (CinematicCamera), INT-005 (CinematicSequencer), INT-006 (NPCController), INT-007 (MissionManager)

## Out of Scope
- Final art assets or textured models
- Voice acting recordings (placeholder text only)
- Particle effects or VFX
- Save/load of intro progress
- Skippable intro (Skip on sequencer exists but no UI button for it yet)
- Multiplayer or network sync

---

## Technical Plan

### Architecture
```
Assets/_Project/Scenes/IntroScene.unity
Assets/_Project/Scripts/MonoBehaviours/Cinematics/ElPolloController.cs
Assets/_Project/Scripts/MonoBehaviours/Cinematics/ChaosMeter.cs
Assets/_Project/Scripts/Editor/IntroSceneSetup.cs
Assets/_Project/ScriptableObjects/Cinematics/DialogueData/BigSmokeVO.asset
Assets/_Project/ScriptableObjects/Cinematics/DialogueData/NikoVO.asset
Assets/_Project/ScriptableObjects/Cinematics/DialogueData/TenpennyStreet.asset
Assets/_Project/ScriptableObjects/Cinematics/DialogueData/TenpennyFarm.asset
Assets/_Project/ScriptableObjects/Cinematics/DialogueData/ChopSubtitles.asset
Assets/_Project/ScriptableObjects/Cinematics/CameraPaths/ActI_CameraPath.asset
Assets/_Project/ScriptableObjects/Cinematics/CameraPaths/ActII_CameraPath.asset
Assets/_Project/ScriptableObjects/Cinematics/CameraPaths/ActIII_CameraPath.asset
Assets/_Project/ScriptableObjects/Cinematics/CameraPaths/ActIV_CameraPath.asset
Assets/_Project/ScriptableObjects/Cinematics/MasterIntroSequence.asset
```

**ElPolloController.cs**: MonoBehaviour on the El Pollo chicken. Three-phase state machine:
- Phase 1 (Normal): moves away from player at base speed, standard flee behavior
- Phase 2 (Dodge): at ChaosMeter 40%, starts lateral dodges (random left/right offset when player gets close)
- Phase 3 (Tired): at ChaosMeter 80% or after 60s timeout, speed drops to 50%, no dodging, catchable

**ChaosMeter.cs**: MonoBehaviour on a screen-space Canvas. Tracks a fill value (0-1) driven by proximity to El Pollo and chase duration. UI is a horizontal Image with fillAmount. Fires OnPhaseChange(int phase) at thresholds.

**LightingPreset**: handled inline — CinematicSequencer SetLighting step changes RenderSettings.ambientLight, directional light color/intensity, and enables/disables point lights.

**IntroSceneSetup.cs**: Editor script with [MenuItem("FarmSimVR/Setup Intro Scene")] that programmatically creates the scene hierarchy, places primitives, assigns materials, and wires all component references.

### Build Approach
1. Create IntroScene.unity with ground plane (scaled cube, green-grey), skybox material (simple gradient)
2. Build Town area: 5-6 house cubes (varied scale, different flat colors), 3 lamp post cylinders with point lights, path planes (lighter color strips on ground)
3. Build player house interior: room cube with door trigger (BoxCollider, isTrigger), bed cube
4. Build Farm/Pen area: reuse AnimalPen prefab or layout, add stump cylinder for El Pollo spawn, barn backdrop (large scaled cube, red-brown)
5. Set up Night lighting preset: low ambient (dark blue, 0.1 intensity), blue-tinted directional light (0.3 intensity), lamp post point lights enabled (warm yellow, range 8)
6. Set up Dawn lighting preset: warm ambient (orange-pink, 0.4 intensity), warm directional light (1.0 intensity, slight angle for sunrise feel), lamp post point lights disabled
7. Place NPCs: Tenpenny (blue capsule via NPCController, by lamp post), Chop (small brown cube, outside pen)
8. Create ElPolloController.cs with 3-phase chase state machine (Normal, Dodge, Tired), speed parameters, lateral dodge logic, CatchZone integration
9. Create ChaosMeter.cs with UI bar (Image fillAmount), threshold events at 0.4 and 0.8, 60s timeout fallback
10. Create all DialogueData assets with placeholder dialogue lines (Big Smoke VO, Niko VO, Tenpenny x2, Chop subtitles)
11. Create CameraPath assets with waypoints for each Act (Act I: bedroom to street, Act II: street overview, Act III: farm approach, Act IV: pen chase angles)
12. Create MasterIntroSequence CinematicSequence asset wiring all Acts: fade in, VO, camera moves, NPC activation, dialogue, lighting transitions, mission start/complete, player control toggles
13. Create IntroSceneSetup.cs editor script with menu item for automated scene creation/reset
14. Playtest full sequence: verify all acts play in order, no null references, timing feels right, adjust durations

### Testing Strategy
- EditMode tests: ElPolloController phase transitions (Normal -> Dodge at threshold, Dodge -> Tired at threshold or timeout)
- EditMode tests: ChaosMeter fill value clamping (0-1), threshold event firing
- PlayMode tests: IntroScene loads without errors
- PlayMode tests: CinematicSequencer.Play executes master sequence, OnSequenceComplete fires
- PlayMode tests: ElPolloController responds to player proximity, phase transitions occur
- PlayMode tests: all DialogueData assets load and contain expected number of lines
- Manual playtest: full run-through of all 4 acts, verify visual coherence, timing, and flow

---

## Task Breakdown

| # | Type | Action | Depends | Acceptance |
|---|------|--------|---------|------------|
| 1 | scene | Create IntroScene.unity with ground plane and skybox | — | Scene opens, ground visible, skybox renders |
| 2 | scene | Build town area: houses, lamp posts, paths | 1 | 5-6 houses placed, lamp posts with lights, paths visible |
| 3 | scene | Build player house interior with bed and door trigger | 2 | Interior room accessible, bed visible, door trigger works |
| 4 | scene | Build farm/pen area: reuse AnimalPen, add stump, barn backdrop | 1 | Farm area distinct from town, stump and barn placed |
| 5 | scene | Set up Night lighting preset (Acts I-II) | 1 | Dark blue ambient, directional at 0.3, lamp post lights on |
| 6 | scene | Set up Dawn lighting preset (Acts III-IV) | 1 | Warm ambient, directional at 1.0, lamp post lights off |
| 7 | scene | Place NPC Tenpenny (blue capsule) and Chop (brown cube) with NPCController | 2,4,INT-006 | NPCs visible, name tags showing, interaction range set |
| 8 | code | Create ElPolloController.cs with 3-phase chase (Normal, Dodge, Tired) | INT-006 | Phase transitions at thresholds, dodge behavior works, catchable in Tired |
| 9 | code | Create ChaosMeter.cs with UI fill bar and threshold events | — | Bar fills over time/proximity, events fire at 0.4 and 0.8 |
| 10 | asset | Create all 5 DialogueData assets with placeholder lines | INT-003 | Each asset has correct speaker names, line count, auto-advance settings |
| 11 | asset | Create 4 CameraPath assets with waypoints per Act | INT-004 | Waypoints define positions, rotations, FOV, durations for each Act |
| 12 | asset | Create MasterIntroSequence CinematicSequence asset | INT-005,10,11 | All steps wired: fades, VO, cameras, NPCs, dialogue, lighting, missions |
| 13 | code | Create IntroSceneSetup.cs editor menu item (FarmSimVR > Setup Intro Scene) | 1-12 | Menu item creates/resets scene hierarchy and wires all references |
| 14 | test | Write tests for ElPolloController and ChaosMeter | 8,9 | Phase transitions and meter thresholds tested |
| 15 | qa | Full playtest: run all 4 acts, adjust timing, fix null refs | 1-13 | Intro plays start to finish without errors, timing feels natural |
