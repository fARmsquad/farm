# Feature Spec: Farming Foundation & Build Sequence — L2-000

## Summary
This document adapts the broader FarmSim VR PRD into a repo-native farming
blueprint. It defines the farming experience as a sequence of connected
building blocks, starting with the smallest playable farm loop and layering
time, weather, progression, atmosphere, and persistence on top of that core.
It intentionally excludes monetization and any other systems that do not
directly strengthen the farming experience.

## User Story
As a farmer, I want the farm to unfold from a tiny hand-tended patch into a
living, understandable system so that every new mechanic feels like a natural
extension of the last one instead of a disconnected feature drop.

## Acceptance Criteria
- [ ] The starting playable farming loop is: select seed -> plant in plot ->
      water -> wait/grow -> harvest -> store or sell.
- [ ] The loop works with 6 plots, 3 starter crops, one well, one tool rack,
      one basket, and one sell point.
- [ ] Each new mechanic is introduced as a dependency-safe building block that
      extends existing state instead of replacing it.
- [ ] Base farming remains fully buildable and testable in the editor before XR
      hand interaction is added.
- [ ] Real-time rhythm, weather, seasons, progression, atmosphere, education,
      and persistence layer on top of the base farm loop in that order.
- [ ] Monetization, competitive systems, and social pressure are excluded from
      this farming design.
- [ ] Social visiting, multiplayer, MR, animal systems, and cooking or crafting
      remain deferred to future specs.

## VR Interaction Model
- **Primary input**: editor-first simulated input for feature completion, then
  XR grab, tilt, push, and pull wrappers on top.
- **Feedback**: soil darkening, crop stage swaps, watering VFX, harvest burst,
  contextual audio, and targeted haptics.
- **Comfort**: no forced movement, seated-compatible reach zones,
  teleport-friendly layout, and no failure state that punishes absence.

## Edge Cases
- Missing a day should reduce quality or delay progress, not destroy the farm.
- Advanced systems must fail soft when the supporting system is absent.
- Weather and seasons should influence the same farm loop, not replace it.
- Every mechanic must stay readable without requiring a floating HUD.

## Performance Impact
- Core farming rules stay in pure C# services and records under `Core/`.
- MonoBehaviour wrappers handle scene visuals, particles, audio, and editor or
  XR wiring only.
- Farming must stay within Quest budgets: minimal per-frame allocations, low
  plot counts, batched visuals, and simple state-driven VFX.

## Dependencies
- `L1-001` Farm Layout Greybox
- `L1-002` Sky & Lighting
- `L2-001` through `L2-006` as the base farm stack
- `L3-*`, `L4-*`, `L5-*`, `L6-*`, and `L7-*` for later layering

## Out of Scope
- Monetization in any form
- Competitive systems, leaderboards, or PvP
- Multiplayer, co-op, or trading for the core farming build
- Mixed Reality or passthrough features
- Animal husbandry, cooking, crafting, and other secondary loops unless
  separately specified

## Product Intent
The farming experience should feel like a calm ritual performed by hand. The
player starts with a tiny plot, simple tools, and forgiving crops. The design
goal is not to overwhelm the player with systems on day one, but to connect
each new mechanic to something they already understand:
- Soil gives planting a place to matter.
- Water gives soil a changing condition.
- Growth gives watering a reason.
- Harvest gives growth a payoff.
- Economy and progression give harvest a future.
- Time, weather, and seasons give the whole farm rhythm.
- Atmosphere and journal systems give meaning without stress.

## Non-Negotiable Farming Rules
- The core loop must be satisfying before it is deep.
- Every new farming mechanic must attach to existing state, not create a
  parallel mini-game.
- The game should reward attention, not punish absence.
- Real-world rhythm is flavor and planning support, not a source of anxiety.
- Education must emerge from consequences and discovery, not pop-up lectures.
- XR is the delivery surface, not the source of truth. The farming simulation
  must stand on its own in the editor first.

## Canonical Starting Farm
The first fully playable farm slice should include:
- 6 crop plots in a readable 2x3 grid
- 1 tool rack
- 1 well or refill point
- 1 harvest basket or storage point
- 1 simple market or sell interaction
- 3 starter crops: tomato, carrot, and lettuce
- 1 seed inventory source
- 1 readable day phase
- 1 fake weather provider before live weather integration

This is the minimum farm that proves the game works.

## Delivery Contract
The farm must be built in sequence. A higher block can only proceed when the
previous block is already:
- working in the editor
- covered by Core tests where applicable
- visually readable in scene
- simple enough that the player can explain cause and effect

The final result should feel "fully built" because each layer is stable before
the next one depends on it.

Per developer direction, each block also ends with a handoff checkpoint. The
implementation should pause after every completed block and provide:
- what to open or run
- what to test directly
- the intended outcome
- the specific feedback needed before the next block begins

## Building Blocks First
| Block | Purpose | Primary Systems | Depends On | Unlocks Next |
|---|---|---|---|---|
| 1. Farm Space | Give the player a readable place to farm | `L1-001`, `L1-002` | None | Plot interactions |
| 2. Item Contract | Define seeds, crops, and storage | `L2-006` | Block 1 | Planting and harvest rewards |
| 3. Soil State | Make each plot persist meaningful state | `L2-001` | Blocks 1-2 | Watering and growth |
| 4. Planting | Turn a seed into a crop instance in a plot | `L2-003` | Blocks 2-3 | Growth simulation |
| 5. Growth | Advance crops over time based on conditions | `L2-002` | Blocks 3-4 | Harvest timing |
| 6. Watering | Let the player directly influence growth | `L2-004` | Blocks 3-5 | Moisture management |
| 7. Harvest | Return crops back into inventory and value | `L2-005` | Blocks 2-6 | Economy |
| 8. Time | Make the farm change even when the player is not acting | `L3-001` | Blocks 3-7 | Day rhythm and weather hooks |
| 9. Weather | Let environment modify the same farm loop | `L3-002` | Blocks 3-8 | Seasonal strategy |
| 10. Seasons | Change what thrives and what is worth planting | `L3-003` | Blocks 3-9 | Long-term planning |
| 11. Progression | Give harvests a future through upgrades and unlocks | `L4-001` to `L4-005` | Blocks 2-10 | Expansion and mastery |
| 12. Atmosphere & Knowledge | Make the farm feel alive and meaningful | `L5-*`, `L6-*`, `L7-*` | Blocks 1-11 | Finished product feel |

## Developer Handoff Checkpoints
| After Block | What You Test | Intended Outcome |
|---|---|---|
| 1. Farm Space | Open the farm scene and move around the playable area. Check plot spacing, rack placement, well placement, and overall readability. | The farm already feels like a coherent place even before crops exist. Nothing important feels too far away or visually confusing. |
| 2. Item Contract | Use the debug inventory path to add, remove, and inspect seeds, crops, and tools. Try edge cases like empty counts and full stacks. | Every farming interaction now speaks one item language. Seeds, harvests, and tools move through one consistent inventory model. |
| 3. Soil State | Inspect several plots and force status, moisture, and nutrient changes. Verify the scene reflects those states clearly. | Each plot feels persistent and meaningful. The player can tell that plots are not interchangeable empty squares anymore. |
| 4. Planting | Try planting valid and invalid seeds in empty and occupied plots. Check that inventory changes and soil status changes together. | Planting feels like the first real farming action. A seed becomes a specific planted crop in a specific plot. |
| 5. Growth | Let planted crops advance through stages under controlled conditions. Check stage transitions, timing, and failure-safe behavior. | Growth makes the farm feel alive. The player can see clear cause and effect between conditions and crop progress. |
| 6. Watering | Water dry, normal, and saturated plots. Refill the can. Verify soil moisture, visuals, and capacity behavior. | Watering is readable and worthwhile. The player understands that moisture is a direct lever on crop success. |
| 7. Harvest | Harvest mature crops, try harvesting too early, and inspect inventory, soil, and payout effects. | The core farm loop is now closed. Planting and waiting pay off with a satisfying harvest that changes the farm state. |
| 8. Time | Run the farm forward through day phases and confirm that lighting and crop simulation still behave correctly. | The farm now has rhythm. Time passing deepens the loop instead of destabilizing it. |
| 9. Weather | Force clear, cloudy, and rain states. Watch crop, soil, audio, and visual reactions. | Weather feels like a modifier on the same farm, not a separate system layered awkwardly on top. |
| 10. Seasons | Swap seasons and verify crop suitability, farm visuals, and planning implications. | The player can now think in longer arcs. Seasons create strategy without creating stress. |
| 11. Progression | Sell crops, unlock upgrades, buy better tools or seeds, and validate expansion hooks. | Harvests now have a future. The player sees how tending the farm opens new options and space. |
| 12. Atmosphere & Knowledge | Play a normal farm session with all systems active, then review journal or educational surfaces. | The farm finally feels whole: readable, calming, responsive, and quietly instructive. |

## Block Delivery Sequence
Do not begin the next block until the current block has been handed to the
developer, tested, and answered with feedback. The point of the sequence is to
lock in the feel of each layer before more dependencies stack on top of it.

### Block 1: Farm Space
- **Build**: `L1-001` and `L1-002` with the starter farm footprint only.
- **Connects**: establishes plot positions, well access, tool rack position,
  spawn, and lighting for every later block.
- **You test**: scene readability, distances, comfort of plot spacing,
  visibility of key anchors, and whether the farm already feels cozy.
- **Feedback gate**: what feels too close, too far, too empty, or in the wrong
  place before logic is attached to the space.

### Block 2: Item Contract
- **Build**: `L2-006` inventory types, item ids, stack rules, and a debug path
  for starter seeds, harvested crops, and tools.
- **Connects**: gives planting, harvesting, upgrades, and selling one shared
  item language.
- **You test**: adding items, removing items, stack limits, starter loadout,
  and whether the inventory model matches the intended farming loop.
- **Feedback gate**: whether the starter item set is correct and whether the
  item model feels too abstract or too restrictive.

### Block 3: Soil State
- **Build**: `L2-001` plot state, moisture, nutrients, soil type, and visible
  plot status feedback.
- **Connects**: becomes the state hub that planting, watering, growth, and
  harvesting all read from.
- **You test**: forcing plot state changes, moisture changes, nutrient changes,
  and verifying the scene communicates those states clearly.
- **Feedback gate**: whether plot state is understandable without explanation
  and whether the visual language for soil is strong enough.

### Block 4: Planting
- **Build**: `L2-003` seed validation, seed consumption, crop instance creation,
  and plot occupancy rules.
- **Connects**: bridges inventory into soil and starts the crop lifecycle.
- **You test**: valid planting, invalid planting, occupied plot behavior, seed
  consumption, and whether planting feels like a meaningful first action.
- **Feedback gate**: whether planting is intuitive, satisfying, and strict in
  the right ways without feeling annoying.

### Block 5: Growth
- **Build**: `L2-002` crop data, stage progression, growth calculation, and
  visible stage transitions for the 3 starter crops.
- **Connects**: gives planted crops time-based behavior and a visible payoff
  path toward harvest.
- **You test**: growth timing, stage readability, condition sensitivity,
  starter crop differences, and whether the farm starts to feel alive.
- **Feedback gate**: whether growth is too fast, too slow, too opaque, or too
  detached from the player's actions.

### Block 6: Watering
- **Build**: `L2-004` can state, refill loop, moisture application, and wetness
  visuals tied directly to soil.
- **Connects**: makes player action directly alter the variables that growth
  depends on.
- **You test**: watering dry plots, refilling, empty-can behavior, saturation
  behavior, and whether watering feels worth doing every session.
- **Feedback gate**: whether watering is tactile and readable enough, and
  whether moisture changes feel too weak or too dominant.

### Block 7: Harvest
- **Build**: `L2-005` harvest validation, yield result, inventory award, and
  soil cost after harvest.
- **Connects**: closes the farm loop and exposes the first real reward cadence.
- **You test**: harvesting mature crops, failing early harvests, yield clarity,
  inventory results, and soil aftermath after repeated harvests.
- **Feedback gate**: whether harvest is satisfying enough to justify the entire
  build so far and whether the loop deserves expansion.

### Block 8: Time
- **Build**: `L3-001` day phases, time scale, and farm-side lighting rhythm.
- **Connects**: lets crops, lighting, and session rhythm share one time source.
- **You test**: phase transitions, lighting stability, crop simulation under
  advancing time, and whether the farm rhythm feels pleasant.
- **Feedback gate**: whether time passing improves the farm or introduces
  pressure, confusion, or visual instability.

### Block 9: Weather
- **Build**: `L3-002` fake provider first, then weather state effects on soil,
  growth, visuals, and audio.
- **Connects**: turns environment into a modifier of the same farming loop.
- **You test**: clear, cloudy, and rain states first; then more severe weather
  only if the core weather responses feel good.
- **Feedback gate**: whether weather enriches planning and atmosphere without
  becoming stressful or mechanically noisy.

### Block 10: Seasons
- **Build**: `L3-003` season state, crop suitability, and simple visual season
  identity across the starter farm.
- **Connects**: moves the player from short-loop tending into long-loop
  planning.
- **You test**: crop availability changes, season readability, and whether
  planning depth increases without making the farm hostile.
- **Feedback gate**: whether seasons feel like strategy or punishment.

### Block 11: Progression
- **Build**: `L4-001` through `L4-005` in order: economy, upgrades, unlocks,
  expansion hooks, then skills.
- **Connects**: gives harvest a future and turns repetition into mastery.
- **You test**: earning, spending, upgrading, unlocking, and whether growth in
  player capability feels earned but still generous.
- **Feedback gate**: whether progression is motivating, readable, and cozy
  instead of grindy.

### Block 12: Atmosphere & Knowledge
- **Build**: `L5-*`, `L6-*`, and `L7-*` only after the farm already works:
  ambience, particles, education surfaces, and persistence polish.
- **Connects**: turns a functional farm into a place the player wants to return
  to.
- **You test**: a normal session end to end, then a return session after save
  and load, plus journal or teaching surfaces.
- **Feedback gate**: whether the game now feels complete, memorable, and
  teachable without losing the calm farming core.

## How The Base Mechanics Connect
### 1. Inventory establishes the language of farming
Before the player can plant or harvest, the game needs one shared item model
for:
- seeds
- harvested crops
- tools
- materials

This prevents each mechanic from inventing its own storage rules.

### 2. Soil becomes the source of truth for every plot
Each plot owns the state that the other systems consult:
- plot status
- moisture
- nutrients
- current crop id
- soil type

This makes soil the hub that planting, watering, growth, and harvest all
connect through.

### 3. Planting consumes inventory and mutates soil
Planting is the first bridge between player action and farm simulation:
- a seed leaves inventory
- a crop instance is created
- the target plot changes from empty to planted

From this point on, the farm has something to simulate.

### 4. Growth reads soil and time
Growth must not own a hidden state machine disconnected from the plot. It
should read:
- crop data
- soil state
- moisture
- nutrients
- time and weather modifiers

This keeps crop outcomes explainable to the player.

### 5. Watering only matters because growth reads moisture
Watering is not its own mini-game. Its job is simple:
- add moisture to a plot
- change visuals and feedback
- improve or stabilize growth

This creates a direct cause-and-effect loop the player can learn quickly.

### 6. Harvest closes the loop
Harvest is the payoff connection:
- growth reaches harvestable
- player performs the harvest action
- items return to inventory
- soil nutrients change
- economy and progression can react

Without this connection, planting and growth are just animation.

## Sequential Outcome By Layer
### Layer 2: The Farm Works
The first complete farming outcome is:
1. Pick a seed from inventory.
2. Plant it into an empty plot.
3. Water the plot.
4. Let time advance growth.
5. See visual stage changes.
6. Harvest the mature crop.
7. Store or sell the crop.
8. See soil reflect the cost of farming.

If this loop is not satisfying yet, nothing higher-layer should be added.

### Layer 3: Time Passes
Once the base loop is trustworthy, time-based systems can deepen it:
- day and night change lighting and farm mood
- weather changes moisture and growth multipliers
- seasons change crop suitability and planning

These systems should always modify the existing loop, never bypass it.

### Layer 4: I Progress
Once the base loop and time systems are readable, progression gives the player
reasons to care about repeated farming:
- economy converts harvest into choice
- tool upgrades improve efficiency
- crop unlocks widen planning space
- farm expansion gives new physical room
- skill trees reward preferred play patterns

Progression should deepen mastery, not add grind.

### Layer 5: It Feels Alive
Only after the loop works should polish escalate:
- ambient audio
- watering and harvest particles
- growth sparkle and stage transitions
- weather soundscapes
- haptics and tactile cues

Polish is there to strengthen clarity and feeling, not hide weak mechanics.

### Layer 6: The World Teaches
The journal and crop knowledge systems should explain what the player has
already started to notice:
- why legumes restore soil
- why tomato and basil help each other
- why rain matters
- why seasons change outcomes

The game should never front-load textbook content before the player has context
for it.

### Layer 7: The Farm Persists
Persistence locks the ritual in place:
- save plot state
- save crop progress
- save inventory
- save progression
- restore the farm exactly as the player left it

Persistence turns a farming session into a farm life.

## Crop Plan For The Sequential Build
### Starter Set
These are the only crops required to prove the base farming loop:
- tomato: medium growth, tutorial crop, clear harvest payoff
- carrot: faster and forgiving root crop
- lettuce: very fast leafy crop for early satisfaction

### First Expansion Set
Add only after the base three feel good:
- potato
- pea
- pepper
- strawberry
- sunflower
- corn

These introduce crop families, soil tradeoffs, and companion logic without
overwhelming the player.

### Later Expansion Set
Only after time, weather, and progression are stable:
- pumpkin
- watermelon
- wheat
- garlic
- eggplant
- beet

Exotic greenhouse crops and orchard trees should remain future scoped until the
base outdoor farm is excellent.

## Farm Design Rules Adapted From The PRD
### Keep
- tactile hand-first interactions
- cozy tone
- real-world rhythm
- progressive mastery
- learn-by-doing farming knowledge
- generous, low-stress economy
- weather and seasonal flavor
- strong accessibility posture

### Adapt
- real-time growth needs a configurable time scale for testing and balancing
- real-world weather should ship behind an interface with a deterministic
  fallback
- night gameplay should remain valid for tending, planning, and atmosphere,
  even if growth is slower
- overripe crops may lose quality, but the farm should not become punitive

### Exclude For Now
- monetization
- ads
- battle pass or seasonal monetized rewards
- competitive social systems
- co-op and trading
- MR or passthrough
- animal husbandry
- cooking or crafting

## Final Farming Outcome
The finished farming experience should feel like this:
- The player enters a small, readable farm that immediately makes spatial
  sense.
- They can pick up a seed, place it in soil, water it, and understand exactly
  what changed.
- Over time, crops visibly grow because of conditions the player can read and
  influence.
- Harvest returns items, money, and progression without adding pressure.
- Weather, seasons, and upgrades deepen the same core ritual instead of
  distracting from it.
- The world sounds alive, looks responsive, and quietly teaches real farming
  ideas.
- When the player returns later, the farm remembers them.

## Spec Map
This umbrella doc sets direction. Detailed implementation remains in:
- `L2-001` Soil System
- `L2-002` Crop Growth System
- `L2-003` Planting System
- `L2-004` Watering System
- `L2-005` Harvest System
- `L2-006` Inventory System
- `L3-001` Day/Night Cycle
- `L3-002` Weather System
- `L3-003` Seasons
- `L4-001` to `L4-005` progression systems
