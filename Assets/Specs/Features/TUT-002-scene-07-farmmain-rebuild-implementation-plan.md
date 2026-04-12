# Implementation Plan: Scene 07 FarmMain Rebuild

## Scope
This plan turns the Scene 07 PRD into a developer execution sequence.

This document is intentionally more than a task list. It is a self-contained
handoff for another developer who should be able to understand the product
intent, scene purpose, tone, asset strategy, and implementation order without
reconstructing the original conversation.

Primary reference:
- `Assets/Specs/Features/TUT-002-scene-07-farmmain-rebuild-prd.md`

Supporting references:
- `Assets/Specs/Features/Tutorial-Sequence-Handbook.md`
- `Assets/Specs/Features/L2-000-farming-foundation-and-sequence.md`
- `Assets/Specs/Features/L1-001-farm-layout-greybox.md`

## Conversation-Derived Intent
The implementation should stay aligned with the developer's explicit direction:

- simplify the farming mechanics in Scene 07
- make the scene straightforward and easy to follow for a casual farming game
- beautify the environment so it reads as a proper farm
- give the scene a set mission and goal instead of vague sandbox behavior
- leave the scene blank at handoff time so the next developer inherits no
  confusing or low-quality legacy setup
- fold the purchased `Farm Crops` and `Tools LowPoly Lite` packs directly into
  the product and environment plan

This means the scene is not being rebuilt as a technical demo, a broad farming
sandbox, or a salvage/refactor of the previous FarmMain attempt. It is a clean,
authored onboarding slice.

## Product Summary
Scene 07 is the player's first complete farming success moment.

The player should enter, understand the task quickly, complete one coherent
farm loop, and leave with the feeling that the game is readable, cozy, and
worth continuing. Scene 07 should teach confidence, not depth.

## Scene Role In The Tutorial
Scene 07 is the payoff beat after the player has already been walked through
introductory non-farm scenes. By the time they enter FarmMain, the game should
stop feeling like setup and start feeling like the actual product.

The scene's job is to answer:
- What does farming in this game actually look like?
- What is the simplest satisfying thing I can do here?
- What does success feel like?

## Player Promise
When the player enters FarmMain, the game is promising:

- you are now in the farming part of the experience
- you only need to focus on one thing right now
- the world will clearly show you where to go and what to do
- you will get a visible payoff when you complete the task

If the player feels lost, over-instructed, or dropped into a half-finished
sandbox, the scene has broken that promise.

## North Star Outcome
The desired end state is:

1. The player spawns and immediately reads "farm."
2. The starter plot is easy to identify.
3. The mission is obvious: grow and harvest the first tomato.
4. The actions are simple: plant, water, watch growth, harvest.
5. The environment feels authored and pleasant, not improvised.
6. The player finishes with a clean sense of success.

## What Success Looks Like
Success is not only technical correctness. It is all of the following at once:

- the scene is visually legible from the first frame
- the mechanic loop is easy enough for a casual player to follow
- the environment looks like a real farm tutorial space
- the mission scope is tight and explicit
- the next developer can open the blank scene and know exactly what to build
- the implementation does not carry forward unwanted baggage from the rejected
  version

## Anti-Goals
Do not turn Scene 07 into any of the following:

- an early version of `WorldMain`
- an open-ended sandbox with too many possible actions
- a mission board with multiple competing objectives
- a runtime-generated farm dressing experiment
- a pile of nice-looking assets with no clear player goal
- a "teach every farming mechanic now" scene
- a scene that depends on the previous FarmMain hierarchy or layout to make
  sense

## Experience Pillars
Every implementation choice should support these pillars:

### 1. Clarity First
The player always knows the next meaningful thing to do.

### 2. Cozy Farm Read
The space feels warm, cultivated, and intentionally composed.

### 3. One Loop, One Win
The player completes exactly one full farming loop and gets a payoff.

### 4. Product Over Prototype
The scene should feel like part of the actual game, not a testbed.

## Player Journey
The target player journey for Scene 07 is:

1. Spawn facing a compact, readable farm setup.
2. Notice the starter plot before noticing side content.
3. Receive a short instruction to plant the tomato.
4. Interact with the plot and see a planted state.
5. Receive a short instruction to water the crop.
6. Water the crop and observe a visible growth change.
7. Receive a short instruction to harvest.
8. Harvest the ripe tomato and get concise success feedback.

At no point should the player need to guess the mission, search a large open
space, or interpret debugging language.

## Definition Of Done
The scene should be considered product-complete only when all of these are
true:

- another developer can understand the scene brief from this plan and the PRD
- `FarmMain.unity` has been rebuilt intentionally from blank
- the environment looks like a farm before mission UI does any heavy lifting
- the mission is a single first-harvest loop with tomato as the hero crop
- the purchased packs are used deliberately to improve the farm read
- the player receives clear success after harvesting
- there is no accidental leftover scaffolding from the previous FarmMain
  attempt

## Current Constraint
The purchased packs are imported and inventoried now, and
`Assets/_Project/Scenes/FarmMain.unity` has already been reduced to a blank
settings-only scene. The implementation should begin from that blank scene and
the verified prefab shortlist below, not from legacy FarmMain content.

Packs in scope:
- `Farm Crops` by BH Black House
- `Tools LowPoly Lite` by ResilientLogic

## Phase 0: Verified Starting State
### Goal
Start the rebuild from a clean scene and a verified asset inventory rather than
rediscovering pack contents.

### Confirmed baseline
1. `Assets/_Project/Scenes/FarmMain.unity` is intentionally blank
2. `Farm Crops` imported under `Assets/FarmCrops`
3. `Tools LowPoly Lite` imported under
   `Assets/ResilientLogicGames/ToolsLowPolyPackLite`
4. Asset review scenes are available:
   - `Assets/FarmCrops/Scenes/Overview.unity`
   - `Assets/FarmCrops/Scenes/DemoScene.unity`
   - `Assets/ResilientLogicGames/ToolsLowPolyPackLite/DemoScenes/DemoScene.unity`

### Verified asset shortlist
| Category | Preferred assets | Notes |
|---|---|---|
| Starter crop progression | `Assets/FarmCrops/Prefabs/Crops/TomatoSeed_01.prefab`, `Assets/FarmCrops/Prefabs/Crops/Tomato_01.prefab`, `Assets/FarmCrops/Prefabs/Crops/Tomato_03.prefab`, `Assets/FarmCrops/Prefabs/Crops/Tomato_05.prefab`, `Assets/FarmCrops/Prefabs/Crops/Tomato_07.prefab` | Recommended ladder from imported names and `Overview.unity`; confirm final ripe stage visually |
| Starter plot base | `Assets/FarmCrops/Prefabs/Fields/Field_1x1m_01.prefab`, `Assets/FarmCrops/Prefabs/Fields/FieldSmall_1x1m_01.prefab` | Use one clean plot base near spawn |
| Furrow alternative | `Assets/FarmCrops/Prefabs/Fields/Furrow_1m_01.prefab`, `Assets/FarmCrops/Prefabs/Fields/Furrow_2m_01.prefab` | Prefer if soil-row readability beats the field tile |
| Raised-bed alternative | `Assets/FarmCrops/Prefabs/Planters/PlankPlanterLow_1x1m.prefab`, `Assets/FarmCrops/Prefabs/Planters/PlankPlanterLow_1x2m.prefab`, `Assets/FarmCrops/Prefabs/Planters/StonePlanterLow_1x1m_01.prefab` | Keep low planters for VR legibility |
| Starter crop support | `Assets/FarmCrops/Prefabs/Crops/TomatoStick_01.prefab` | Optional support prop if the starter tomato reads better staked |
| Secondary crop dressing | `Assets/FarmCrops/Prefabs/Crops/Carrot_03.prefab`, `Assets/FarmCrops/Prefabs/Crops/Cabbage_03.prefab`, `Assets/FarmCrops/Prefabs/Crops/Corn_04.prefab` | Background only; do not compete with tomato mission |
| Edge foliage | `Assets/FarmCrops/Prefabs/Foliage/FoliageGrass_01.prefab`, `Assets/FarmCrops/Prefabs/Foliage/FoliageGrass_02.prefab`, `Assets/FarmCrops/Prefabs/Foliage/FoliageFlower_01.prefab`, `Assets/FarmCrops/Prefabs/Foliage/FoliageWheat_01.prefab` | Use sparingly to frame paths and boundaries |
| Tool rack props | `Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/Spade.prefab`, `Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/Hoe.prefab`, `Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/SpadingFork.prefab`, `Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/Sickle.prefab` | Create scene variants from prefabs, not FBX files |
| Watering prop | `Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/Bucket.prefab` | Place near the well or refill point |

### Deliverable
A blank FarmMain scene plus this verified shortlist, ready for authored scene
assembly.

### Exit Criteria
- blank scene confirmed
- actual root paths known
- candidate prefabs selected
- review scenes identified

## Phase 1: Scene Art Direction Pass
### Goal
Rebuild `FarmMain.unity` as an authored tutorial farm rather than a testbed.

### Product intent for this phase
This phase is not just "place art." It is where the scene earns the promise
that the player has arrived at a real farm. The finished environment should
make the mission feel obvious before any system-heavy guidance appears.

### Tasks
1. Build the scene from zero in the blank `FarmMain.unity` file
2. Author the intended spatial layout:
   - south spawn
   - starter plot near main path
   - west well
   - east tool rack
   - north structure massing
3. Use `Farm Crops` to make the farm read as cultivated from spawn
4. Use `Tools LowPoly Lite` to make the rack and work zones feel real
5. Ensure the starter plot is visually obvious without overwhelming highlights
6. Do not preserve any old FarmMain hierarchy, placement, or temporary dressing

### Asset Mapping
#### `Farm Crops`
- tutorial hero crop: `TomatoSeed_01 -> Tomato_01 -> Tomato_03 -> Tomato_05`
  or `Tomato_07` after visual confirmation
- starter plot base: `Field_1x1m_01`, `FieldSmall_1x1m_01`, or a low planter
- side plots: muted supporting crop prefabs
- field-edge dressing: foliage / crop rows / cultivated accents

#### `Tools LowPoly Lite`
- rack display: spade, fork, hoe, sickle
- water support: bucket near well or watering station

### Exit Criteria
- spawn view clearly frames the farm tutorial
- scene reads as a proper farm before UI appears
- the starter plot is the visual hero of the scene
- surrounding assets support the mission instead of competing with it

## Phase 2: Mission Flow Implementation
### Goal
Implement one clean onboarding mission: first tomato harvest.

### Product intent for this phase
This phase should reduce complexity, not add it. The player should never be
holding more than one active mental instruction.

### Tasks
1. Add or replace the scene-local FarmMain controller
2. Sequence the mission into single-step guidance:
   - go to starter plot
   - plant tomato
   - water crop
   - wait/watch growth
   - harvest crop
3. Keep guidance restrained and world-anchored
4. Reuse generic farming systems where possible:
   - `FarmSimDriver`
   - `FarmPlotInteractionController`
   - `FarmPlotActionResolver`
   - `CropPlotController`
5. Avoid tutorial-only duplicate simulation logic

### Exit Criteria
- one clear objective at a time
- no debug-style mission overload
- first harvest completes reliably
- mission copy is short, plain, and player-facing
- the flow feels like a casual farming game, not a tutorial instrument panel

## Phase 3: Asset-Aware Visual Progression
### Goal
Make crop progression feel authored, not abstract.

### Product intent for this phase
The crop growth read is the heart of the scene's payoff. The player should feel
that they changed the world, not that they toggled a debug state.

### Preferred path
If `Farm Crops` contains stageable crop variants:
- use authored stage swaps for the starter crop
- begin with the verified tomato shortlist rather than inventing a new visual
  progression path

### Fallback path
If `Farm Crops` does not contain usable staged tomato visuals:
- use imported crops for scene dressing and non-hero plots
- keep the starter crop on the project's current crop-state visual path until a
  better staged asset is available

### Exit Criteria
- the player can tell the crop changed state
- the starter crop feels like part of the world, not just a debug cube
- the visual progression supports delight as well as comprehension

## Phase 4: Completion State
### Goal
End Scene 07 cleanly after harvest.

### Product intent for this phase
Success should feel like a satisfying finish to the first farming lesson, not a
hard cut or a systems explosion into full sandbox mode.

### Tasks
1. Decide completion state:
   - local free play after success, or
   - explicit completion overlay / continue affordance
2. Add success feedback with minimal text
3. Ensure the farm does not suddenly become noisy or system-heavy after success

### Exit Criteria
- completion feels earned
- scene does not overstay its welcome
- the player is left with a clear sense of "I did it"

## Phase 5: VR Readability And Comfort
### Goal
Make sure the authored scene still works at Quest scale.

### Tasks
1. Validate viewing distances for starter plot, tool rack, and well
2. Validate walk path clarity from spawn
3. Validate instruction readability
4. Check for over-clutter or occlusion from imported crops/tools
5. Keep performance in Quest budget

### Exit Criteria
- starter plot readable in-headset
- critical props legible without strain
- imported assets do not create visual clutter or perf regressions

## Environment Content Brief
The final farm composition should communicate the following without relying on
heavy UI:

- where the player entered from
- which plot matters most
- where water comes from
- where farm tools live
- that this is a maintained working farm, not an abandoned lot

Recommended scene read:
- south entry path into the farm
- starter plot closest to path and centered in the main cone of attention
- water/well on the west side
- tool rack on the east side
- a north-side silhouette such as barn, farmhouse, shed, or similar anchor
- fencing, hedges, crops, foliage, or terrain boundaries to keep the space
  feeling intentional and contained

## Mission Copy And UX Tone
Copy should be short, plain, and product-facing.

Preferred examples:
- `Plant the tomato seed.`
- `Water the planted crop.`
- `Harvest the ripe tomato.`

Avoid:
- debug wording
- system jargon
- multi-step paragraphs
- multiple active objectives at once
- language that implies the player should experiment broadly in this scene

## Engineering Rules
- keep `Core/` free of Unity references
- keep scene tutorial logic thin
- prefer prefab variants over editing vendor originals
- use the verified imported asset paths in this plan as the first pass
- do not let purchased assets expand Scene 07 scope
- do not rebuild on top of any removed FarmMain content

## Risk Register
### Risk 1: Packs are visually inconsistent
Mitigation:
- use imported assets selectively
- support them with existing materials / lighting / layout polish

### Risk 2: `Farm Crops` lacks the exact tomato stage visuals needed
Mitigation:
- use the pack for contextual farm dressing
- keep the tutorial hero crop on existing state-driven visuals if needed

### Risk 3: Tool props encourage scope creep into full tool simulation
Mitigation:
- keep tools primarily diegetic unless direct interaction materially improves
  onboarding clarity

### Risk 4: Imported materials need URP fixes
Mitigation:
- inspect the imported demo/overview scenes immediately before authoring
- convert or replace incompatible materials before scene authoring continues

### Risk 5: Duplicate or odd vendor prefab names cause accidental wrong picks
Mitigation:
- prefer the cleanly named shortlisted prefabs in this plan
- avoid ambiguous assets like `PlankPlanterHigh_1x2m (1).prefab` unless there
  is a specific reason to use them

## Suggested Work Order
1. Open the blank `FarmMain.unity`
2. Review `Overview.unity` / vendor demo scenes for final visual picks
3. Lock scene layout
4. Author environment using the shortlisted imported assets
5. Implement mission controller
6. Integrate crop progression visuals
7. Add completion state
8. Run editor and headset playtests

## Final Handoff Expectation
The next developer should be able to read this plan and know:

- why the scene exists
- what emotional and product outcome it is trying to deliver
- what not to build
- which assets to start from
- why the scene was blanked
- how to sequence the work from environment to mission to polish

## Handoff Note
Do not start Scene 07 by excavating the previous FarmMain scene. It is blank on
purpose. Start from the verified asset shortlist in this plan, confirm the
final tomato-stage picks in `Overview.unity`, and build the authored farm
composition first. The mission flow should sit inside a farm that already feels
real.
