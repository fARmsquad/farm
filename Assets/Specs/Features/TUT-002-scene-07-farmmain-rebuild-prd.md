# PRD: Scene 07 FarmMain Rebuild

## Purpose
This document replaces the current Scene 07 implementation attempt with a
developer-ready rebuild brief.

Use it as the authoritative handoff for `Assets/_Project/Scenes/FarmMain.unity`.
The next game developer should treat the current FarmMain scene-specific logic
as disposable and rebuild the scene as an authored, readable, cozy tutorial
space rather than a runtime-generated placeholder.

As of 2026-04-11, `Assets/_Project/Scenes/FarmMain.unity` has been reset to a
blank settings-only scene file. There is intentionally no legacy hierarchy,
layout, or mission scaffolding left in the scene asset.

## Conversation Intent Capture
This brief reflects the explicit product direction from the developer:

- simplify the farming mechanics for Scene 07 so the loop is straightforward
  and casual-player friendly
- beautify the environment so it reads as a proper farm rather than a debug
  arena or greybox
- give the scene one clear mission and goal instead of broad sandbox freedom
- fully gut the previous FarmMain attempt so the next developer inherits no
  confusing scene clutter or accidental design direction
- integrate the newly purchased asset packs into both the visual plan and the
  execution handoff
- produce a handoff that explains the player-facing intent, not just the
  technical steps

If a future implementation conflicts with those bullets, the implementation is
off brief even if the scene technically functions.

## Asset Integration Status
Two newly purchased packs are now imported and verified locally:

| Pack | Imported root | Verified example assets | Primary Scene 07 use |
|---|---|---|---|
| `Farm Crops` | `Assets/FarmCrops` | `Assets/FarmCrops/Prefabs/Crops/TomatoSeed_01.prefab`, `Assets/FarmCrops/Prefabs/Crops/Tomato_03.prefab`, `Assets/FarmCrops/Prefabs/Fields/Field_1x1m_01.prefab`, `Assets/FarmCrops/Prefabs/Planters/PlankPlanterLow_1x1m.prefab`, `Assets/FarmCrops/Prefabs/Foliage/FoliageGrass_01.prefab` | starter crop, plot bases, raised beds, secondary crop dressing, edge foliage |
| `Tools LowPoly Lite` | `Assets/ResilientLogicGames/ToolsLowPolyPackLite` | `Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/Spade.prefab`, `Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/Hoe.prefab`, `Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/SpadingFork.prefab`, `Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/Sickle.prefab`, `Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/Bucket.prefab` | tool rack dressing, work-zone props, bucket at watering point |

Useful visual review scenes:
- `Assets/FarmCrops/Scenes/Overview.unity`
- `Assets/FarmCrops/Scenes/DemoScene.unity`
- `Assets/ResilientLogicGames/ToolsLowPolyPackLite/DemoScenes/DemoScene.unity`

The next developer does not need to rediscover the import roots. The remaining
choice is visual curation: which verified prefabs best fit the starter mission.

## Executive Summary
Scene 07 is the payoff beat of the linear tutorial chain:

1. Intro
2. Chicken chase
3. Post-chicken bridge
4. Placeholder bridge
5. Find tools
6. Pre-farm bridge
7. Farm tutorial

The player should arrive in FarmMain already primed to start farming. This
scene must teach exactly one complete loop for a casual farming game:

- approach the farm
- identify the starter plot
- plant one crop
- water it
- observe it grow
- harvest it

That is the mission. Do not expand the scope inside Scene 07 beyond that loop.

The point of this scene is not to simulate the whole farming game. The point is
to make the player feel, in one compact sequence, "I understand what this game
is and I just completed my first real farming task."

## Why This Needs A Rebuild
The current farm-specific implementation is not the right shape for the scene.
The rejected direction tried to solve the problem with runtime-generated 3D
dressing and extra scene glue, which made the scene feel improvised rather than
designed.

The replacement should be:

- authored in-scene, not assembled from runtime primitives
- visually legible from the first spawn frame
- mechanically simple enough for a first-time player
- grounded in the existing farming systems instead of inventing a parallel
  tutorial mini-game

## Player Experience Goal
When the player enters FarmMain, they should immediately understand:

1. where they are
2. what they need to do first
3. what counts as success

The feeling target is:

- cozy
- calm
- tactile
- beginner-friendly
- clearly a farm

The scene should feel like the real game, just focused and simplified.

## Non-Negotiable Decisions
- [x] Scene 07 remains a separate tutorial scene, not `WorldMain`
- [x] The objective is the first harvest only
- [x] The first guided crop is tomato
- [x] The scene should be easy to follow for a casual player
- [x] The scene should look like a proper farm, not a debug testbed
- [x] Reuse existing farm systems where possible
- [x] Do not depend on selling, progression depth, season depth, or sandbox
      integration to finish the scene

## Out Of Scope
- `WorldMain` sandbox integration
- advanced economy or selling flow
- multiple missions or branching objectives
- deep season strategy
- broad weather simulation as a blocker
- animal systems
- multiplayer, social, MR, or progression expansion
- runtime-generated environmental dressing as the primary art solution

## Current Repo Context

### Tutorial chain
- Scene map: `.ai/docs/scene-work-map.md`
- Tutorial scene catalog:
  `Assets/_Project/Scripts/Core/Tutorial/TutorialSceneCatalog.cs`
- Shared scene ownership catalog:
  `Assets/_Project/Scripts/Core/Tutorial/SceneWorkCatalog.cs`

### Existing design documents
Read these before implementation:

- `Assets/Specs/Features/Tutorial-Sequence-Handbook.md`
- `Assets/Specs/Features/L2-000-farming-foundation-and-sequence.md`
- `Assets/Specs/Features/L1-001-farm-layout-greybox.md`
- `Assets/Specs/Features/farm-scene-demo.md`
- `Assets/Specs/Features/TUT-002-scene-07-farmmain-rebuild-implementation-plan.md`

### Purchased asset packs now in scope
These packs should shape the Scene 07 rebuild:

#### `Farm Crops` by BH Black House
Verified project roots and review scenes:
- `Assets/FarmCrops`
- `Assets/FarmCrops/Scenes/Overview.unity`
- `Assets/FarmCrops/Scenes/DemoScene.unity`

Planned use in Scene 07:
- starter tomato sequence candidates:
  - `Assets/FarmCrops/Prefabs/Crops/TomatoSeed_01.prefab`
  - `Assets/FarmCrops/Prefabs/Crops/Tomato_01.prefab`
  - `Assets/FarmCrops/Prefabs/Crops/Tomato_03.prefab`
  - `Assets/FarmCrops/Prefabs/Crops/Tomato_05.prefab`
  - `Assets/FarmCrops/Prefabs/Crops/Tomato_07.prefab`
- starter plot base candidates:
  - `Assets/FarmCrops/Prefabs/Fields/Field_1x1m_01.prefab`
  - `Assets/FarmCrops/Prefabs/Fields/FieldSmall_1x1m_01.prefab`
  - `Assets/FarmCrops/Prefabs/Fields/Furrow_1m_01.prefab`
  - `Assets/FarmCrops/Prefabs/Fields/Furrow_2m_01.prefab`
- raised-bed alternatives:
  - `Assets/FarmCrops/Prefabs/Planters/PlankPlanterLow_1x1m.prefab`
  - `Assets/FarmCrops/Prefabs/Planters/PlankPlanterLow_1x2m.prefab`
  - `Assets/FarmCrops/Prefabs/Planters/StonePlanterLow_1x1m_01.prefab`
- contextual farm dressing:
  - `Assets/FarmCrops/Prefabs/Crops/Carrot_03.prefab`
  - `Assets/FarmCrops/Prefabs/Crops/Cabbage_03.prefab`
  - `Assets/FarmCrops/Prefabs/Crops/Corn_04.prefab`
  - `Assets/FarmCrops/Prefabs/Foliage/FoliageGrass_01.prefab`
  - `Assets/FarmCrops/Prefabs/Foliage/FoliageGrass_02.prefab`
  - `Assets/FarmCrops/Prefabs/Foliage/FoliageFlower_01.prefab`
- optional starter-plot support prop:
  - `Assets/FarmCrops/Prefabs/Crops/TomatoStick_01.prefab`

Recommended interpretation from the imported names and overview scene:
- treat `TomatoSeed_01 -> Tomato_01 -> Tomato_03 -> Tomato_05/07` as the
  starter crop readability ladder, then confirm the exact final ripe stage
  visually in `Overview.unity`

#### `Tools LowPoly Lite` by ResilientLogic
Verified project roots and review scene:
- `Assets/ResilientLogicGames/ToolsLowPolyPackLite`
- `Assets/ResilientLogicGames/ToolsLowPolyPackLite/DemoScenes/DemoScene.unity`

Planned use in Scene 07:
- tool rack set dressing:
  - `Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/Spade.prefab`
  - `Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/Hoe.prefab`
  - `Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/SpadingFork.prefab`
  - `Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/Sickle.prefab`
- watering-area support:
  - `Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/Bucket.prefab`

Use the imported prefabs, not the raw FBX files, as the starting point for
scene variants.

### Current scene neighbors
- Scene 05 (`FindToolsGame`) is still a placeholder walk-to-marker beat
- Scene 06 (`Tutorial_PreFarmCutscene`) is a short bridge into farming
- Scene 08 (`WorldMain`) is sandbox and explicitly not the onboarding route

### Existing systems worth reusing
These are the main reusable systems already in the repo:

- `Assets/_Project/Scripts/MonoBehaviours/Farming/FarmSimDriver.cs`
  - generic planting, watering, harvesting, composting, starter seeds
- `Assets/_Project/Scripts/MonoBehaviours/Farming/FarmPlotInteractionController.cs`
  - generic look-at plot prompt + keyboard fallback actions
- `Assets/_Project/Scripts/MonoBehaviours/CropPlotController.cs`
  - MonoBehaviour wrapper for crop plot state
- `Assets/_Project/Scripts/Core/Farming/FarmPlotActionResolver.cs`
  - action prompt generation based on plot state
- `Assets/_Project/Scripts/MonoBehaviours/Farming/WorldFarmBootstrap.cs`
  - useful as reference for plot naming and farm-side helper systems
- `Assets/_Project/Scripts/MonoBehaviours/Tutorial/TutorialFlowController.cs`
  - linear tutorial scene flow
- `Assets/_Project/Scripts/MonoBehaviours/Tutorial/TutorialSceneInstaller.cs`
  - scene bootstrapping for the tutorial chain

### Existing systems to treat as non-authoritative
- `Assets/_Project/Scripts/MonoBehaviours/Tutorial/TutorialFarmSceneController.cs`
  currently exists only as an empty placeholder boundary after gutting the
  rejected implementation. The next developer should replace it with a real
  scene-specific controller only if needed.

## Blank Scene Constraint
`Assets/_Project/Scenes/FarmMain.unity` is intentionally blank now. There are
no legacy objects to preserve, salvage, or reverse engineer. The rebuild
should start from zero and add only the authored objects required by this PRD.

### Asset authoring rule for the rebuild
Once the packs are imported:
- do not wire scene references directly to vendor prefabs without review
- create curated prefab variants or scene-local wrappers where needed
- keep vendor originals untouched
- record actual imported root folders and chosen prefab paths in the
  implementation plan

## Product Requirements

### Core mission
The scene must communicate one explicit mission:

`Grow and harvest your first tomato.`

The scene should never ask the player to infer the goal from debug UI or broad
sandbox affordances.

### Completion condition
The scene is complete when the player successfully harvests the starter tomato.

At completion, the scene should do one of the following:

1. show a clean completion state and allow relaxed local free play, or
2. show a clean completion state and offer exit / replay / continue options

Do not require selling, expansion, or a second crop to count as done.

### Time-to-completion target
- first-time player target: 2-4 minutes
- repeat player target: under 90 seconds

### Instruction model
- only one actionable instruction should be emphasized at a time
- prefer world-space guidance or restrained UI over a large debug-style HUD
- every prompt must map to a visible object or location in the scene
- avoid text walls

## Environment Requirements

### The farm must read as a place
On spawn, the player should immediately understand they are standing in a farm
yard, not in an abstract gameplay arena.

Required environmental reads:
- clear entrance / spawn point
- visible starter plot near the main path
- readable working farm cluster:
  - plots
  - tool storage
  - water source
  - harvest/basket destination or obvious work surface
- a north-side anchor structure or silhouette such as barn/farmhouse
- fence, hedges, tree line, or terrain edge that gives the farm a boundary

The new purchased packs should help solve this:
- `Farm Crops` should supply the crop density and cultivated field language
- `Tools LowPoly Lite` should supply the tool readability around the rack and
  watering area

### Target layout
Base the rebuild on the repo's starter farm assumptions, but author it with
more intention than the current greybox:

- spawn at the south entrance facing inward
- starter plot closest to the main path and easiest to see
- remaining plots visible but visually secondary
- well or refill point on the west side
- tool rack or seed/tool pickup area on the east side
- barn / farmhouse massing to the north as spatial anchor
- paths connecting spawn -> starter plot -> well -> rack cleanly

### Visual direction
The farm should feel:
- warm
- welcoming
- compact
- hand-tended
- readable from Quest-scale distances

Do:
- use authored environment dressing
- use `Farm Crops` as the primary source for crop-field dressing
- use `Tools LowPoly Lite` to make the rack and work zones feel diegetic
- use simple but intentional silhouettes
- create one strong spawn view that frames the farm goal
- keep the playable area compact enough for VR comfort

Do not:
- rely on runtime primitive generation for the finished scene
- fill the space with generic debug cubes
- hide mission-critical areas behind clutter
- make the farm feel like a giant open sandbox in this scene

## Gameplay Requirements

### Required loop
The exact onboarding loop for Scene 07 is:

1. enter the farm and orient
2. identify the starter plot
3. plant tomato
4. water tomato
5. wait long enough to observe growth change
6. harvest tomato
7. receive clear success feedback

Recommended diegetic support using the purchased packs:
- the starter plot should be visually distinct via crop-state presentation, not
  only UI
- the tool rack should visibly carry the farm tools from `Tools LowPoly Lite`
- the watering beat should be staged near a readable bucket/water setup

### Required teaching outcomes
By the end of the scene, the player must understand:

- plots are the places where crops live
- seeds become crops in a specific plot
- water changes growth conditions
- crops visibly progress over time
- harvest is the payoff for tending a crop

### Optional but acceptable
- one very light weather beat if it helps readability
- one lightweight completion banner
- unlock messaging for other plots after completion

### Not required in Scene 07
- selling
- money
- upgrades
- deep inventory management
- multiple crop comparison
- seasons as decision-making

## UX Requirements

### Guidance
The guidance should feel like onboarding, not instrumentation.

Preferred guidance order:
- visual highlight on the starter plot
- very short on-screen instruction
- contextual interaction prompt when looking at the plot
- success feedback after each step

### UI tone
User-facing copy should be plain and direct. Example tone:
- "Plant the tomato seed."
- "Water the planted crop."
- "Harvest the ripe tomato."

Avoid:
- debug wording
- system jargon
- long mission text
- multiple simultaneous checklist branches

## Technical Requirements

### Architecture constraints
- Core farming logic must remain in `Core/` with zero `UnityEngine` references
- Scene-specific tutorial behavior should live in thin MonoBehaviour wrappers
- Do not create a second farming simulation just for the tutorial
- Editor-first implementation is required before XR-specific interaction polish

### Recommended implementation approach
1. Author the environment directly in `FarmMain.unity`
2. Reuse existing plot naming and farm interaction contracts
3. Add only the minimum scene-specific controller needed to sequence the first
   harvest mission
4. Keep all "teach one thing at a time" logic in one scene-local controller or
   a tiny supporting service, not scattered across many unrelated systems
5. Use existing prompts and plot state where possible before introducing new UI
6. Start from the verified imported prefabs listed in this brief and create
   reviewed prefab variants for Scene 07 usage

### Reuse before rewrite
The next developer should try to reuse:
- plot ids like `CropPlot_0`
- `FarmSimDriver` for the actual farm loop
- `FarmPlotInteractionController` for baseline interactions
- existing soil/crop visual updaters

Only extend these systems if the tutorial readability requirement cannot be met
cleanly with the current behavior.

## Deliverables
- [ ] Reauthored `Assets/_Project/Scenes/FarmMain.unity`
- [ ] Imported and inventoried `Farm Crops` assets relevant to Scene 07
- [ ] Imported and inventoried `Tools LowPoly Lite` assets relevant to Scene 07
- [ ] Clear scene-specific mission flow for first harvest
- [ ] Proper farm environment dressing authored in-scene
- [ ] Starter plot guidance that is visually obvious
- [ ] Step-by-step onboarding that never overwhelms the player
- [ ] Completion state after first harvest
- [ ] Playtest guide for editor and VR verification
- [ ] Tests for any new Core logic introduced

## Acceptance Criteria
- [ ] From spawn, the scene immediately reads as a farm
- [ ] Imported crop assets from `Farm Crops` are used intentionally rather than
      dumped generically into the scene
- [ ] Imported tool assets from `Tools LowPoly Lite` visibly support the rack,
      water, or work-zone reads
- [ ] The player can identify the starter objective within 10 seconds
- [ ] The scene teaches plant -> water -> grow -> harvest with no extra
      dependency on selling or sandbox systems
- [ ] The player only needs to follow one clear objective at a time
- [ ] The first harvest feels like the obvious payoff of the scene
- [ ] The scene uses existing farm systems instead of inventing a disconnected
      tutorial-only simulation
- [ ] The environment is authored and intentional, not runtime-generated
- [ ] The scene is comfortable and readable in VR scale
- [ ] The scene still works in editor fallback controls for iteration

## Implementation Notes For The Next Developer
- Treat Scene 07 as a small, shippable vertical slice, not as a partial open
  world.
- Keep `WorldMain` out of the onboarding contract.
- Resist the urge to teach every farming mechanic in the first scene.
- Scene art direction matters here; the farm must feel finished enough that the
  player trusts the game.
- Use the two purchased packs to strengthen the scene read, but do not let the
  available assets dictate the mission design.
- Start from the blank `FarmMain.unity` scene asset. Do not rebuild on top of
  old hierarchy assumptions.
- If a system is only needed to make Scene 07 work, keep it local to Scene 07.
- If a system improves the real farming loop generally, extend the shared farm
  systems instead.

### Pack-specific direction
#### `Farm Crops`
- start with the verified tomato shortlist in this brief instead of searching
  the pack again from scratch
- prioritize tomato-capable visuals for the starter plot
- use secondary crop visuals only to add context, not to compete with the
  starter mission
- if the pack contains multiple growth-stage variants, prefer authored swaps
  over runtime primitive scaling for the tutorial crop read
- use `Overview.unity` to make the final pick between `Tomato_05` and
  `Tomato_07` for the ripe state
- use `Field_1x1m_01`, `FieldSmall_1x1m_01`, `Furrow_1m_01`, and low planter
  variants as the first layout options before exploring larger pieces

#### `Tools LowPoly Lite`
- use the five verified prefab assets listed above and place them on or near
  the tool rack in a deliberate display
- use the bucket near the watering affordance or well zone
- the tools should teach "farm work happens here" even if they are static props
- do not overcomplicate Scene 07 by making every tool interactable unless that
  directly improves onboarding clarity

## QA / Playtest Checklist
### Editor
- enter FarmMain directly
- verify spawn facing
- verify starter plot visibility
- complete the full first harvest loop
- verify no step feels ambiguous
- verify no instruction depends on hidden debug knowledge

### VR / headset
- verify pathing and reach feel comfortable
- verify the starter plot is visible and readable without neck strain
- verify prompts are legible at the intended viewing distance
- verify the farm feels cozy and not empty
- verify completion feels satisfying enough to end the tutorial chain

## Open Questions
- After first harvest, should Scene 07 remain in-place for free play or show a
  completion overlay with explicit next-action options?
- Should extra plots be interactable immediately after completion or stay
  visually present but mechanically muted?
- Do we want one diegetic completion prop such as a harvest basket or table, or
  should the scene end at the harvest itself?

## Handoff Summary
Build Scene 07 as a authored first-harvest farm tutorial. Reuse the existing
farm loop systems, keep the scope tight, and make the environment look like a
real farm from the first frame. The mission is one tomato, one watering step,
one growth read, one harvest, and a clean sense of success.
