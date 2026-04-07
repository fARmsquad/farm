# Feature Spec: Intro Scene Assembly — INT-008

## Summary
The complete 5-panel intro cinematic (IntroScene.unity) that wires ALL cinematic systems together into a ~90-second non-interactive sequence transitioning to gameplay. Panels: 1) Sleeping Soundly, 2) Town Sleeps, 3) The Disruption, 4) The Walk, 5) The Pen → gameplay. This is the final assembly spec — all prerequisite systems must be Done first.

## User Story
As a player, I want to experience a scripted 5-panel intro that introduces Willowbrook, El Pollo Loco's disruption, the walk to the farm, and my first mission, so that I understand the world's personality and feel motivated to play.

## Acceptance Criteria

### Scene Structure
- [ ] IntroScene.unity contains: Bedroom interior, Town exterior, Farm/Pen area
- [ ] Scene loads from TitleScreen via SceneManager.LoadScene
- [ ] CinematicSequencer auto-plays on scene Start

### Panel 1: Sleeping Soundly (8s)
- [ ] Camera: interior tight shot on player in bed, slow zoom out revealing bedroom
- [ ] Lighting: NightPreset — cool blue moonlight, window light cookie, lamp off
- [ ] Audio: crickets + wind ambient, guitar fingerpicking at 40% volume building
- [ ] Animation: player breathing (blanket rise/fall loop), curtain sway
- [ ] DustMotes particle in moonbeam
- [ ] PanelText: "Willowbrook. Population: quiet enough." (fade in/out)
- [ ] Transition: hard cut to Panel 2

### Panel 2: Town Sleeps (6s)
- [ ] Camera: wide aerial shot, slow pan left-to-right across rooftops
- [ ] All house windows dark, lamp posts with dim warm glow
- [ ] Cat silhouette crosses rooftop (RooftopRunner, easter egg)
- [ ] ChimneySmoke particle on one chimney (Big Smoke's restaurant)
- [ ] Cat meow SFX at ~3s (left channel)
- [ ] PanelText: "Every night, the same peace and quiet..."
- [ ] Transition: smash cut (abrupt) to Panel 3

### Panel 3: The Disruption (8s)
- [ ] Camera: medium shot, El Pollo Loco silhouetted on rooftop, neck extended
- [ ] SFX: guitar snap → silence → bass-boosted "BAWK-BAWK-BAWKKKKKK!!!"
- [ ] Screen shake on crow (0.3s, ±4px horizontal via ScreenEffects.ScreenShake)
- [ ] WindowCascade: house windows light up in sequence radiating from rooster position (0.2s intervals)
- [ ] Post-crow SFX: dogs barking (staggered), baby crying (faint), "NOT AGAIN" yell
- [ ] Muffled V.O. lines: Big Smoke, then Niko (from inside houses)
- [ ] ComicBurst: "COCK-A-DOODLE-DOO!!!" with impact font, rotation, scale-in
- [ ] BootProjectile: thrown from window, arcs toward rooster, misses, rooster doesn't flinch
- [ ] Transition: fade to black (0.5s), fade in to Panel 4

### Panel 4: The Walk (15-20s)
- [ ] Camera: medium-wide tracking shot behind player, slight elevated angle
- [ ] Player walks from town toward hills with LanternHolder (warm light, pendulum swing)
- [ ] LightingTransition: Night → PreDawn as walk progresses
- [ ] Fireflies particle in meadow
- [ ] Creek crossing with CreekShimmer particle + water ambient SFX
- [ ] Music crossfade: guitar → tension track (minor key)
- [ ] Footsteps SFX synced to walk, lantern creak on swing
- [ ] NPC Tenpenny at lamp post (optional): auto-plays street dialogue as player passes
- [ ] Town lights shrink in background
- [ ] PanelText possible: Tenpenny's key line as subtitle
- [ ] Transition: slow dissolve as sky lightens

### Panel 5: The Pen — Transition to Gameplay (10s cinematic + gameplay start)
- [ ] Camera: wide establishing shot of McTavish Farm at dawn
- [ ] LightingTransition: PreDawn → Dawn preset (dusty pink → orange → gold)
- [ ] DewSparkle particles on grass
- [ ] El Pollo Loco perched on stump, idle animation (head tilt, peck, fourth-wall stare 1s)
- [ ] Chop sits outside fence, barking rhythmically (SpeechBubble: "Woof!" → translation subtitle)
- [ ] Baby chicks (4-6) milling inside pen (BabyChick.cs, pecking)
- [ ] Music: kazoo standoff whistle
- [ ] SFX: morning birds, rooster confident clucks, gate creak
- [ ] Player stops at gate, hand reaches for latch
- [ ] ScreenEffects.HideLetterbox (bars retract, 0.5s)
- [ ] MissionManager.StartMission: "POLLO LOCO" title card (western font), subtitle "Capture El Pollo Loco. Don't harm the chicks.", holds 3s, fades
- [ ] HUD fades in: Timer (5:00), ChaosMeter (empty), action hints
- [ ] Player control ACTIVE — cutscene ends, gameplay begins
- [ ] AutoSave triggers, game clock set to 6:15 AM

### Global
- [ ] SkipPrompt appears after 3 seconds ("Hold [Space] to skip"), skips to Panel 5 gameplay
- [ ] Letterbox bars active during all cinematic panels (8% top and bottom)
- [ ] Audio layering: VO > SFX > Music
- [ ] All DialogueData, CameraPath, and CinematicSequence assets wired
- [ ] Editor menu item "FarmSimVR > Setup Intro Scene" creates/resets entire scene

## Edge Cases
- Skip during any panel: SkipPrompt fires → lighting snaps to Dawn, all audio stops, jump to Panel 5 HUD transition
- Scene entered from editor (not TitleScreen): plays normally from Panel 1
- Missing audio clips: SimpleAudioManager logs warning, sequence continues
- Player walks off path in Panel 4: invisible walls constrain to path corridor
- El Pollo chase timeout (60s): auto-enters Tired phase
- Chop SpeechBubble off-screen: clamps to edge

## Performance Impact
- Greybox + Synty prefabs: under 100 draw calls
- Particles (all combined): under 250 active particles
- One directional light + 8-10 point lights (no shadows): under budget
- ElPolloController: simple transform movement, no physics
- Target: 60+ FPS editor, 90 FPS Quest

## Dependencies
- **INT-001** Screen Effects (fade, shake, letterbox, objective, mission banner) — **Done**
- **INT-002** Simple Audio Manager (music, SFX, crossfade) — **Done**
- **INT-003** Dialogue System (typewriter, auto-advance) — **Done**
- **INT-004** Cinematic Camera (waypoints, paths, FOV) — **Done**
- **INT-005** Cinematic Sequencer (step orchestration) — **Open, required**
- **INT-006** NPC Controller (Tenpenny, Chop interaction) — **Done**
- **INT-007** Mission Manager (objectives, completion) — **Done**
- **INT-009** Lighting Presets & Transitions (night→dawn, window cascade) — **Open, required**
- **INT-010** Particle Effects (fireflies, smoke, dew, creek, dust motes) — **Open, required**
- **INT-011** Comic Text & Speech Bubbles (panel text, comic burst, bubbles) — **Open, required**
- **INT-012** Skip & Auto-Save (hold-to-skip, save on gameplay start) — **Open, required**
- **INT-013** Intro Props (lantern, baby chicks, boot, cat silhouette) — **Open, required**
- **Existing code:** ElPolloController.cs, ChaosMeter.cs (built as part of this spec), AnimalPen, PlayerMovement, GameManager
- **After this spec:** INT-014 (Art & Audio Polish) replaces all placeholders with real assets

## Asset Strategy (Placeholders First)
- **Use existing Synty assets** where available: SM_Prop_Bed_01 (bed), SM_Prop_Chair_Rocking_01 (chair), SM_Prop_Table_01 (nightstand), SM_Env_Tree_Stump_01 (El Pollo perch), SM_Prop_Fence_Wood_Gate_01 (pen gate), SM_Gen_Prop_Light_Roof_01 (lamp posts)
- **Use existing models** where available: chicken.glb at 1.5x for El Pollo Loco, chicken.glb at 0.3x with yellow tint for baby chicks, farmgirl.glb for player
- **Primitive placeholders** for missing models: brown cube for Chop (dog), cylinder+sphere for lantern, brown cube for boot, black capsule for cat silhouette
- **No audio required** — all SFX/music/VO slots wired with null checks (SimpleAudioManager logs warnings, sequence continues silently)
- **Default TMPro bold font** for comic burst text
- All placeholders replaced in **INT-014 Art & Audio Polish** after scene assembly is verified

## Out of Scope
- Real art assets or final audio (that's INT-014)
- Real voice acting (that's INT-014)
- Multiplayer or network sync
- Full day/night cycle (intro sets clock, cycle is a future L3 spec)
- Difficulty settings for the El Pollo chase

---

## Technical Plan

### Architecture
```
Assets/_Project/Scenes/IntroScene.unity
Assets/_Project/Scripts/MonoBehaviours/Cinematics/ElPolloController.cs
Assets/_Project/Scripts/MonoBehaviours/Cinematics/ChaosMeter.cs
Assets/_Project/Editor/IntroSceneSetup.cs
Assets/_Project/ScriptableObjects/Cinematics/DialogueData/*.asset   (7+ assets)
Assets/_Project/ScriptableObjects/Cinematics/CameraPaths/*.asset    (5 paths, one per panel)
Assets/_Project/ScriptableObjects/Cinematics/Lighting/*.asset       (3 presets)
Assets/_Project/ScriptableObjects/Cinematics/MasterIntroSequence.asset
```

### Build Approach
1. Create IntroScene.unity with ground plane and skybox
2. Build Bedroom interior (Panel 1): room cube, bed, window with moonlight cookie, nightstand
3. Build Town aerial area (Panel 2): reuse WorldMain town zone or simplified version, lamp posts, dark windows
4. Build Disruption area (Panel 3): rooftop for El Pollo Loco, window objects with toggleable lights
5. Build Walk path (Panel 4): trail from town to farm with invisible wall corridor
6. Build Farm/Pen area (Panel 5): reuse AnimalPen, add stump, barn, baby chick spawns, Chop position
7. Place all NPCs: El Pollo Loco, Tenpenny, Chop, baby chicks, boot source window
8. Create ElPolloController.cs: 3-phase chase (Normal→Dodge→Tired), integrates with CatchZone
9. Create ChaosMeter.cs: UI fill bar, threshold events at 0.4 and 0.8, 60s timeout
10. Create all DialogueData assets: Big Smoke VO, Niko VO, "NOT AGAIN", Tenpenny street, Tenpenny farm, Chop subtitles, Panel text overlays
11. Create 5 CameraPath assets (one per panel)
12. Create MasterIntroSequence: wire all 5 panels in order with timing from the script
13. Wire SkipPrompt, AutoSave, LightingTransitions, Particles, ComicText, Props
14. Create IntroSceneSetup.cs editor menu item
15. Full playtest: run all 5 panels, adjust timing, verify flow, fix null refs

### Testing Strategy
- EditMode: ElPolloController phase math, ChaosMeter clamping/thresholds
- PlayMode: IntroScene loads, sequencer plays all 5 panels, OnSequenceComplete fires
- PlayMode: Skip from Panel 2 → lands at Panel 5 gameplay with correct state
- PlayMode: All DialogueData and CameraPath assets load with expected content
- Manual: full 90-second playthrough, timing feel, comedy beats land, transition smoothness

---

## Task Breakdown

| # | Type | Action | Depends | Acceptance |
|---|------|--------|---------|------------|
| 1 | scene | Create IntroScene.unity with ground, skybox, section markers | — | Scene opens, sections delineated |
| 2 | scene | Build bedroom interior (Panel 1) | 1 | Room, bed, window, moonlight, nightstand |
| 3 | scene | Build town aerial area (Panel 2) with dark windows + lamp posts | 1 | Town visible from above, all windows dark, lamps dim |
| 4 | scene | Build rooftop area (Panel 3) with El Pollo silhouette spot + window lights | 1 | Rooftop peak, toggleable window lights positioned |
| 5 | scene | Build walk path (Panel 4) with trail + invisible wall corridor | 1 | Path from town to farm, creek crossing point |
| 6 | scene | Build farm/pen area (Panel 5) with stump, barn, chick spawns, Chop spot | 1 | Farm area complete, pen has stump + spawn points |
| 7 | scene | Place all NPCs: El Pollo, Tenpenny, Chop, baby chicks | 2-6, INT-006, INT-013 | All NPCs at correct positions with correct components |
| 8 | code | Create ElPolloController.cs (3-phase: Normal→Dodge→Tired) | — | Phase transitions at thresholds, dodge works, catchable when tired |
| 9 | code | Create ChaosMeter.cs (UI fill bar, threshold events) | — | Fills by proximity/time, events at 0.4 and 0.8, 60s timeout |
| 10 | asset | Create all DialogueData assets (7+) with lines from script | INT-003 | All speaker names, lines, timing match the intro script |
| 11 | asset | Create 5 CameraPath assets (one per panel) | INT-004 | Waypoints match script camera descriptions |
| 12 | asset | Create 3 LightingPreset assets + transitions | INT-009 | Night, PreDawn, Dawn presets with correct values |
| 13 | asset | Place all particle prefabs (fireflies, smoke, dew, dust, creek) | INT-010 | Particles at correct positions per panel |
| 14 | asset | Wire ComicTextManager (panel text, comic burst, speech bubbles) | INT-011 | All text overlays match script descriptions |
| 15 | asset | Wire SkipPrompt + AutoSave | INT-012 | Skip works from any panel, auto-save on gameplay start |
| 16 | asset | Wire all intro props (lantern, boot, cat, chicks) | INT-013 | Props triggered at correct moments in sequence |
| 17 | asset | Verify all audio assets loaded in AudioLibrary | INT-014 | Zero missing clips, all keys map correctly |
| 18 | asset | Create MasterIntroSequence wiring all 5 panels | INT-005, 10-17 | Full sequence plays all panels in order |
| 19 | code | Create IntroSceneSetup.cs editor menu item | 1-18 | Menu creates/resets scene, wires all refs |
| 20 | test | Write tests for ElPolloController + ChaosMeter | 8, 9 | Phase transitions + meter thresholds tested |
| 21 | qa | Full 90-second playtest, adjust timing, fix issues | 1-20 | Intro plays start to finish, comedy lands, transitions smooth |
