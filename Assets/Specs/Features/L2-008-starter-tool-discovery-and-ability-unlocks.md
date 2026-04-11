# Feature Spec: Starter Tool Discovery & Ability Unlocks — L2-008

## Summary
This PRD defines the starter progression layer that sits on top of the base
farming loop. Instead of granting every farming verb immediately, the player
reclaims physical farm tools from the homestead, and each recovered tool
unlocks a specific set of farming abilities in a readable, low-stress order.

The goal is to make early farming feel earned, tactile, and spatially grounded
without turning onboarding into a grind. Tools are permanent ability unlocks,
not disposable items, and each one remains relevant after it is found.

## User Story
As a new farmer, I want to recover the missing farm tools one by one so that I
learn the farm through action and each new ability feels like a natural step in
mastering the land.

## Product Goals
- Teach farming verbs through diegetic discovery instead of abstract tutorials.
- Keep the first 20-30 minutes structured, tactile, and easy to read in the
  current first-person 3D build.
- Make each collected tool unlock a real verb, not a passive stat bonus.
- Preserve the cozy design pillar: progression should feel restorative, not
  grindy, punishing, or time-pressured.

## Acceptance Criteria
- [ ] A fresh save starts with no advanced farming abilities unlocked and clear
      visibility of which starter tools are still missing.
- [ ] Collecting the `HandTrowel` unlocks `PrepareSoil` and `PlantSeedPocket`.
- [ ] Collecting the `WateringCan` unlocks `RefillCan` and `WaterPlot`.
- [ ] Collecting the `HarvestSickle` unlocks `HarvestCrop` and `ClearSpentCrop`.
- [ ] Collecting the `Hoe` unlocks `CreateRow`, `ExpandField`, and access to
      additional starter plots beyond the initial tutorial patch.
- [ ] Collecting the `SoilMeter` unlocks diegetic plot readouts for moisture,
      fertility, and crop suitability.
- [ ] Collecting the `CompostTool` unlocks `CompostPlot` and nutrient recovery.
- [ ] Collecting the `PipeWrench` unlocks irrigation placement, valve tuning,
      and low-stress automation hooks.
- [ ] Missing-tool interactions fail soft with world-space hints, ghost props,
      or rack silhouettes; the player is never blocked without knowing why.
- [ ] Tool unlock state persists independently from the scene pickup object,
      and any recovered tool continues to show the correct rack state after
      reloads or scene resets.
- [ ] The first closed farm loop is: recover trowel -> recover watering can ->
      plant and water -> recover sickle -> harvest.
- [ ] The unlock sequence is fully testable in editor/debug flows, with XR or
      hand-tracking interaction explicitly deferred to a later spec.

## 3D Interaction Model
- **Navigation**: reuse the existing `FirstPersonExplorer` movement model:
  WASD to move, mouse to look, and a center-screen focus point.
- **Targeting**: reuse the existing raycast-driven interaction pattern so tools
  and plots are focused by looking at them within a short interaction distance.
- **Tool recovery**: pressing `E` on a highlighted tool pickup claims it
  permanently and updates the rack or recovery location visuals.
- **Verb execution**: after a tool is unlocked, the current plot prompt flow
  should surface only the actions the player has earned, using the same style
  of contextual prompt already used for farming actions.
- **Clarity**: if a verb is locked, the prompt should name the missing tool and
  point back to the relevant recovery location.

## Tool Catalog
| Tool | Recovery Location | Unlocks | How It Works | Fail-Soft Rule |
|---|---|---|---|---|
| Hand Trowel | Potting bench beside the shed | Prepare soil, dig seed pocket, transplant seedling | Look at the trowel and press `E`, then use unlocked plot prompts on the starter patch | Empty plot prompt shows a trowel hint and points to the bench |
| Watering Can | Dry well or refill station | Refill can, water plot | Recover at the well, then unlocks watering prompts and refill points | Dry plot prompt names the missing watering can |
| Harvest Sickle | Overgrown patch on the plot border | Harvest ripe crops, clear dead stalks and weeds | Recover from the border patch; ripe crops then expose harvest prompts | Ripe crop prompt references the sickle instead of failing silently |
| Hoe | Lean-to or barn wall after first harvest | Create rows, widen field, open extra plots | Recover after first harvest to unlock row-making and outer plot conversion prompts | Locked outer plots show hoe markers on their boundary stakes |
| Soil Meter | Tool chest unlocked after first field expansion | Read moisture, fertility, and crop suitability | Recover from chest to unlock inspect prompts on plots | Plot hint shows "unknown soil" until the meter is found |
| Compost Tool | Compost bin area after first nutrient depletion | Restore nutrients, prep premium crops | Recover near compost bin to unlock compost action prompts on depleted plots | Depleted soil points to compost bin instead of presenting hidden math |
| Pipe Wrench | Pump shack or irrigation crate after repeated watering chores | Place hoses, tune valves, enable irrigation | Recover at pump shack to unlock irrigation build points and valve prompts | Irrigation sockets remain visible but inactive until the wrench is owned |

## Feature Blocks
| Block | Name | Player Outcome | Tools | Depends On |
|---|---|---|---|---|
| 1 | Homestead Recovery | Learns the farm layout, tool rack, and recovery loop | None | L1-001, L1-002 |
| 2 | Break Ground | Gains the first "make the soil ready" verb | Hand Trowel | Block 1, L2-001, L2-003 |
| 3 | Bring Water | Understands that crops respond to direct care | Watering Can | Block 2, L2-004 |
| 4 | Earn the First Harvest | Closes the core farming loop and clears plot clutter | Harvest Sickle | Blocks 2-3, L2-002, L2-005 |
| 5 | Expand the Field | Moves from tiny patch to small farm layout | Hoe | Block 4, L2-001 |
| 6 | Read and Repair Soil | Understands invisible soil state through physical tools | Soil Meter, Compost Tool | Block 5, L2-001 |
| 7 | Scale Gently | Upgrades from hand-tending to assisted tending | Pipe Wrench | Block 6, future irrigation spec |

## Block Details
### Block 1: Homestead Recovery
- Establish a tool rack, shed, well, and overgrown starter patch.
- Show missing-tool silhouettes from the first minute so the player reads the
  early game as "restore the farm" rather than "follow prompts."
- Unlock rule: none. This block only teaches place, pathing, and recovery.

### Block 2: Break Ground
- Recovery beat: find the hand trowel at the potting bench.
- New verbs: loosen one starter plot, open a seed pocket, and plant the first
  seed type into the tutorial patch.
- Design note: the trowel should feel like a precise single-plot tool, not a
  field-expansion tool. That distinction is reserved for the hoe.

### Block 3: Bring Water
- Recovery beat: find the watering can at the dry well and fill it.
- New verbs: refill, tilt-to-pour, and visually darken soil moisture state.
- Design note: watering must be readable immediately, with visible wet soil and
  a simple can-empty state, not hidden numeric moisture feedback.

### Block 4: Earn the First Harvest
- Recovery beat: find the sickle while clearing the border growth around the
  tutorial plot as the first crop matures.
- New verbs: harvest mature crops, clear spent plants, and reopen the plot.
- Design note: the sickle should never be a combat weapon in the farming loop.

### Block 5: Expand the Field
- Recovery beat: earn or uncover the hoe after completing the first harvest.
- New verbs: create straight furrows, convert outer ground into usable plots,
  and increase the active plot count from the tutorial patch to the starter
  farm footprint.
- Design note: expansion should feel generous. One unlock should meaningfully
  enlarge the farm, not grant only a single extra tile.

### Block 6: Read and Repair Soil
- Recovery beat: obtain the soil meter from a field chest and the compost tool
  from the compost area after the player sees their first depleted plot.
- New verbs: inspect hidden plot variables and actively restore nutrients.
- Design note: the meter exposes meaning, while compost restores agency. These
  tools should arrive together so "problem" and "solution" stay linked.

### Block 7: Scale Gently
- Recovery beat: unlock the pipe wrench at the pump shack after repeated manual
  watering sessions prove the base loop first.
- New verbs: place or tune irrigation pieces and unlock soft automation.
- Design note: automation should remove repetition, not the farm's sense of
  care. Manual tools remain valid even after irrigation exists.

## Edge Cases
- If a tool pickup object is disabled, destroyed, or reloaded after recovery,
  the unlock remains and the rack should still show the reclaimed state.
- If the player discovers tools out of the intended order, the system should
  allow the find but only surface verbs that have valid supporting systems.
- If a crop becomes ready before the sickle is collected, the crop should wait
  in a readable harvestable state with a clear sickle hint.
- If a player revisits a tool location after unlocking it, the world should
  communicate that the tool has been reclaimed rather than showing duplicate
  permanent tools unless duplicates are a deliberate accessibility choice.

## Performance Impact
- Unlock logic must live in pure C# so it can be tested without scene-specific
  dependencies.
- Rack silhouettes, ghost tools, and plot hints should be event-driven visual
  state changes, not per-frame scanning.
- Tool pickups should rely on simple highlights, mesh swaps, and lightweight
  prompt updates rather than heavy world-space UI canvases.

## Dependencies
- `L1-001` Farm Layout (starter homestead anchors)
- `L1-002` Sky & Lighting
- `L2-001` Soil System
- `L2-002` Crop Growth System
- `L2-003` Planting System
- `L2-004` Watering System
- `L2-005` Harvest System
- `L2-006` Inventory System

## Out of Scope
- Full economy balancing and shop pricing
- Multiplayer tool sharing or social gifting
- XR controller input and hand tracking
- Combat uses for tools
- Crafting or cooking unlocked by the same sequence
- MR passthrough placement or spatial-anchor specific onboarding

---

## Technical Plan

### Architecture
- **Core namespace**: `FarmSimVR.Core.Farming.Progression`
- **Core types**:
  - `StarterToolId`
  - `FarmAbility`
  - `ToolUnlockState`
  - `IToolUnlockService`
  - `ToolUnlockService`
  - `AbilityGateResult`
  - `AbilityGateService`
  - `ToolRecoveryDefinition`
- **MonoBehaviour wrappers**:
  - `UnlockableToolPickup`
  - `ToolRackController`
  - `FarmAbilityGateController`
  - `ToolHintPresenter`
- **Existing scene integrations**:
  - `FirstPersonExplorer`
  - `FarmPlotInteractionController`
  - `FarmSimDriver`
- **ScriptableObjects**:
  - `StarterToolDefinition`
  - `ToolUnlockSequenceAsset`

### Build Approach
1. Add a pure Core unlock state that records owned tools, unlocked abilities,
   current block, and recovery history.
2. Add a Core ability gate service that existing farming actions consult before
   allowing plant, water, harvest, compost, or irrigation verbs.
3. Define starter tool data in assets or records so design can reorder blocks
   without rewriting gate logic.
4. Add scene wrappers for tool pickups, rack silhouettes, and hint routing, but
   keep the source of truth in Core.
5. Integrate with the existing first-person and plot-prompt flow so the feature
   is immediately playable in the current 3D farm without requiring carried tool
   props, XR packages, or gesture recognition.
6. Defer physical tool handling, XR controller input, and hand tracking to a
   later follow-up feature once the progression logic feels right in 3D.

### Research Reference
- Unity's `CharacterController` is the right fit for the current non-Rigidbody
  first-person farm exploration model already used in the repo:
  https://docs.unity3d.com/Manual/class-CharacterController.html
- Unity's `Physics.Raycast` API matches the current camera-focus interaction
  pattern and supports the short-distance tool and plot targeting this feature
  needs:
  https://docs.unity3d.com/ScriptReference/Physics.Raycast.html
- Unity's Input System quickstart reinforces staying on the new Input System for
  keyboard and mouse input while this feature remains in the 3D playable slice:
  https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/QuickStartGuide.html

### Memory Reference
- `.ai/memory/project-memory.md` ADR: "Pure C# Core + thin MonoBehaviour
  wrappers" requires unlock logic to stay out of Unity-dependent code.
- `.ai/memory/project-memory.md` ADR: "New Input System only" means any tool
  interaction layer must avoid legacy `UnityEngine.Input`.
- `.ai/memory/design-philosophy.md` requires progressive mastery, tactile
  satisfaction, and low-pressure learning; this spec uses tools to teach verbs
  without punitive gating or abstract UI popups.
- Existing repo exemplars `FirstPersonExplorer` and `FarmPlotInteractionController`
  establish the current 3D exploration and prompt pattern this feature should
  extend rather than replace.

### Testing Strategy
- **EditMode**:
  - Tool unlock state persistence
  - Ability gating per tool
  - Block progression rules
  - Recovery and respawn state transitions
  - Save/load round-trip for unlocked tools
- **PlayMode**:
  - Tool pickup and rack state transitions
  - Tool hint routing when a verb is blocked
  - Plot and irrigation affordance visibility per unlock state
- **Manual 3D playtest**:
  - Readability of tool pickup prompts
  - Clarity of locked-versus-unlocked farming actions
  - Whether the first 30 minutes feel guided without becoming restrictive

---

## Task Breakdown

### Task 1: Core Enums and State Records
- **Type**: Core classes
- **Files**: `StarterToolId.cs`, `FarmAbility.cs`, `ToolUnlockState.cs`
- **Depends on**: nothing
- **Acceptance**: all starter tools and gated abilities are represented in pure
  C# and serializable for save/load.

### Task 2: Tool Unlock Service
- **Type**: Core service
- **Files**: `IToolUnlockService.cs`, `ToolUnlockService.cs`
- **Depends on**: Task 1
- **Acceptance**: collecting a tool mutates unlock state exactly once and moves
  the player into the correct block.

### Task 3: Ability Gate Service
- **Type**: Core service
- **Files**: `AbilityGateResult.cs`, `AbilityGateService.cs`
- **Depends on**: Tasks 1-2
- **Acceptance**: plant, water, harvest, compost, and irrigation requests can
  be blocked with a specific missing-tool reason before scene wrappers exist.

### Task 4: Tool Sequence Definitions
- **Type**: Data/configuration
- **Files**: `StarterToolDefinition.cs`, `ToolUnlockSequenceAsset.cs`
- **Depends on**: Task 2
- **Acceptance**: designers can reorder recovery locations, hints, and block
  metadata without rewriting unlock logic.

### Task 5: Rack, Recovery, and Hint Wrappers
- **Type**: MonoBehaviour wrappers
- **Files**: `UnlockableToolPickup.cs`, `ToolRackController.cs`,
  `ToolHintPresenter.cs`
- **Depends on**: Tasks 2-4, `L1-001`
- **Acceptance**: missing tools display silhouettes, recovered tools route back
  to a known anchor, and blocked verbs produce readable hint output.

### Task 6: Existing Farming Gate Integration
- **Type**: Integration updates
- **Files**: wrappers around planting, watering, harvest, soil inspection, and
  future irrigation entry points
- **Depends on**: Task 3, `L2-001` through `L2-006`
- **Acceptance**: no gated verb bypasses the unlock system in editor/debug play.

### Task 7: Expansion and Midgame Tool Hooks
- **Type**: Core + wrapper integration
- **Files**: outer-plot gate, soil readout gate, compost gate, irrigation gate
- **Depends on**: Tasks 5-6
- **Acceptance**: hoe, soil meter, compost tool, and pipe wrench each unlock a
  distinct new layer of farming mastery rather than duplicating old tools.

### Task 8: 3D Interaction Phase
- **Type**: 3D interaction integration
- **Files**: updates to `FirstPersonExplorer`, `FarmPlotInteractionController`,
  and any tool-prompt bridge components
- **Depends on**: Tasks 5-7
- **Acceptance**: the entire tool-gated loop is playable in the current
  keyboard-and-mouse farm build with no XR-specific dependencies.

### Task 9: Playtest and Handoff
- **Type**: validation
- **Files**: spec checklist plus playtest guide
- **Depends on**: Tasks 1-8
- **Acceptance**: developer receives a starter-tool playtest loop covering
  recovery order, blocked verbs, and readability of each tool action in the
  current 3D build.
