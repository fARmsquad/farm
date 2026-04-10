# Farming Systems Handbook

## Purpose
This document explains the current farming implementation in plain product
language so the team can review what already exists in the build, what affects
moment-to-moment farming, what the current debug surfaces are, and where the
next tool and system hooks already exist.

This is intentionally implementation-accurate. It describes the current
mechanics in the repo, not just the target design.

## 1. Current Farming Experience

The player moves through the world in first person and works each plot directly.
The loop is:

1. Walk to the farm plots.
2. Look at an individual plot.
3. Use the context prompt to plant, water, harvest, or compost.
4. Watch soil and crop visuals update on the plot itself.
5. Let time, weather, and seasons affect growth.
6. Harvest crops into inventory.
7. Go to the farmhouse side of the slice to sell crops, gain XP, unlock
   upgrades, and spend skill points.

The system is already integrated into `WorldMain`, not only into the isolated
farm scene.

## 2. World Layout

The current focused world farming slice keeps three meaningful spaces active:

- `Farm Plots`
- `Farm House`
- `Chicken Coop`

Everything else in the prototype world is pruned at runtime so the slice stays
readable. The coop zone now also connects to the world animal pen game, so the
world no longer feels like “plots only”.

## 3. Plot Interaction Model

Farming is no longer driven only by a debug panel. The player works each plot
directly through a look-at interaction prompt.

### Plot Actions
- `T` plant tomato
- `C` plant carrot
- `L` plant lettuce
- `P` water
- `H` harvest
- `M` compost

These actions are context-sensitive:
- empty plots show planting options
- growing plots show water and compost
- harvestable plots show harvest, water, and compost
- depleted plots show restore-soil compost only

## 4. Plot Visuals

The world plots now match the clean square farming pads from the isolated farm
slice.

### Current visual rules
- each runtime plot gets a square `1m x 1m` soil surface
- planted plots show a sprout immediately
- crop visuals grow as progress increases
- soil color changes by plot state
- color writes target both `_Color` and `_BaseColor` so the visuals work across
  built-in and URP-style materials

### Plot state visuals
- `Empty`
  - neutral tilled soil
- `Planted`
  - immediate planted-state soil shift
  - visible sprout
- `Growing`
  - crop rises and becomes more visible
  - soil remains active and responsive to watering
- `Harvestable`
  - crop reaches ready state
  - soil stays active
- `Depleted`
  - soil reads as exhausted until compost restores it

## 5. Soil System

Each plot has persistent soil state managed in pure C#.

### Per-plot state
- `Status`
- `CurrentCropId`
- `Moisture`
- `Nutrients`
- `SoilType`

### Current soil types
- `Loam`
- `Sandy`
- `Clay`
- `Rich`

The current world flow defaults new farming plots to loam unless otherwise
configured.

### What soil does right now
- moisture decays over time
- watering restores moisture
- harvest consumes nutrients
- compost restores nutrients
- depleted plots become slower/weaker until restored

### Current soil state machine
- `Empty`
- `Planted`
- `Growing`
- `Harvestable`
- `Depleted`

`Planted` is a short transitional state. On the next simulation tick it becomes
`Growing`.

## 6. Crop Set

The current implemented starter crops are:

- tomato
- carrot
- lettuce

### Starter crop behavior
- tomato
  - slower than lettuce
  - more summer-friendly in season logic
- carrot
  - medium growth speed
  - strong in spring/autumn season logic
- lettuce
  - fastest starter crop
  - more sensitive to harsh seasons

## 7. Growth Model

Growth runs through a pure C# calculator and uses:

- crop base growth rate
- weather multiplier
- temperature multiplier
- soil quality multiplier
- season multiplier
- world progression growth multiplier

### Current implemented base rates
- tomato: `0.05`
- carrot: `0.08`
- lettuce: `0.10`

### Current implemented weather effect
- `Rain` = `2.0x` growth
- other weather = `1.0x`

### Current implemented temperature effect
- below `10C` or above `35C` = `0.5x`
- otherwise = `1.0x`

### Current implemented soil quality effect
- `Poor` = `0.5x`
- `Normal` = `1.0x`
- `Rich` = `1.5x`

### Current implemented season effect
Season multiplies growth per planted crop. The system uses the crop planted in
that plot, not just the currently selected seed.

## 8. Weather System

Weather is already a real gameplay system, not only a visual layer.

### Current weather states
- `Sunny`
- `Cloudy`
- `Rain`

### Weather gameplay effects

#### Sunny
- baseline outdoor farming state
- no weather growth penalty
- full sun presentation

#### Cloudy
- currently mostly a presentation and mood state
- also used as the effective weather during dawn, dusk, and night in growth
  resolution, which softens the growth profile outside core daylight

#### Rain
- auto-waters all outdoor plots over time
- doubles crop growth rate
- activates rain atmosphere
- darkens lighting
- becomes stronger if the player invests in the `Rain Tender` skill

### Weather automation
Weather changes automatically over time unless forced by debug shortcuts.

### Weather debug shortcuts
- `Shift+Y` sunny
- `Shift+U` cloudy
- `Shift+I` rain
- `Shift+O` return to automatic weather

## 9. Sun / Day-Night Lighting

The slice now boots into a brighter morning-start state so the farm reads as a
sunlit destination instead of a muted dawn setup.

### Current day phases
- `Night`
- `Dawn`
- `Morning`
- `Noon`
- `Afternoon`
- `Dusk`

### What the sun system does
- drives directional light color
- drives light intensity
- drives sun elevation/pitch across the day arc

### Current morning/sun behavior
- the day clock now starts in `Morning`
- the sun is explicitly preserved and reattached in the world slice
- sunny weather is applied to the lighting controller immediately so the world
  comes up in a readable state

## 10. Season System

Seasons are already wired into the farming pipeline and will become richer as
the season branch lands.

### Current seasons
- `Spring`
- `Summer`
- `Autumn`
- `Winter`

### Current season timing
- one season lasts `7` in-game days by default

### Current crop season suitability

#### Tomato
- Spring: `0.5x`
- Summer: `1.0x`
- Autumn: `0.5x`
- Winter: `0.0x`

#### Carrot
- Spring: `1.0x`
- Summer: `0.5x`
- Autumn: `1.0x`
- Winter: `0.0x`

#### Lettuce
- Spring: `1.0x`
- Summer: `0.3x`
- Autumn: `0.8x`
- Winter: `0.0x`

### Planting rule
If a crop’s season multiplier is `0`, it cannot be planted in that season.

### Season lighting effect
Seasons tint the sun and outdoor light:
- Spring: fresh green-white
- Summer: warm yellow-white
- Autumn: golden amber
- Winter: cooler blue-white

## 11. Atmosphere Layer

The farming slice has a light atmosphere pass to make the space feel alive
without changing the core loop.

### Audio
- plot ambience
- wind
- birds/crickets depending on day phase
- chicken ambience in the coop area

### Particles
- plot pollen
- house smoke
- coop dust
- rain curtain near the camera in rainy weather

## 12. Inventory and Selling

Farming uses a slot-based pure C# inventory.

### Inventory role today
- stores seeds
- stores harvested crops
- removes seeds when planting
- removes crops when selling

### Selling flow
- harvest crops into inventory
- move to the farm-house side of the slice
- press `K`
- all harvested crops are sold
- coins and XP are awarded through the farming progression service

## 13. Farming Progression

The current world farming progression path is lightweight but functional.

### State that persists
- coins
- XP
- level
- skill points
- watering can tier
- expansion level
- Green Thumb rank
- Merchant rank
- Rain Tender rank

### Leveling
- every `100 XP` grants a level
- every level grants `1` skill point

### Current farming skills

#### Green Thumb
- increases growth multiplier

#### Merchant
- increases sale multiplier

#### Rain Tender
- increases rain watering strength

### Watering can upgrade
Watering upgrades increase watering strength.

### Expansion hooks
The current labels are:
- Starter Farm
- Expanded Plots
- House Workbench
- Coop Upgrade

These are current progression hooks, not full content drops yet.

## 14. Current Tools

### Implemented now
- seed actions through the plot prompt
- watering action
- harvest action
- compost action
- watering can tier progression

### Not yet fully productized as physical tools
- hoe
- fertilizer bag
- pruning shears
- sprinklers
- wheelbarrow
- scarecrow

## 15. Potential Tool Roadmap

These are the most natural next tool layers based on the current architecture.

### Near-term
- `Hoe`
  - prepare or restore untilled plots
- `Fertilizer Bag`
  - temporary nutrient or growth boost
- `Sprinkler`
  - persistent passive moisture support for unlocked areas

### Mid-term
- `Pruning Shears`
  - tree/orchard support when orchard systems land
- `Wheelbarrow`
  - bulk transport or harvest movement
- `Scarecrow`
  - future pest reduction hook if pests are added

### Why these fit the current codebase
- the progression layer already supports upgrade hooks
- the plot state model already has enough structure for per-plot modifiers
- the world slice already has zones and overlays where tools can be explained

## 16. Current Developer Shortcuts

### Weather
- `Shift+Y` sunny
- `Shift+U` cloudy
- `Shift+I` rain
- `Shift+O` auto weather

### Farming progression
- `Shift+P` save progression
- `Shift+L` load progression
- `K` sell harvest
- `U` buy watering-can upgrade
- `O` unlock next expansion hook
- `[` add `100` coins
- `]` add `100` XP
- `1` spend Green Thumb point
- `2` spend Merchant point
- `3` spend Rain Tender point

## 17. World Debug / Reference Surfaces

There are two intentional in-world or on-screen reference surfaces for demos and
testing:

- the world farming overlay
- the weather debug overlay

These tell the team:
- what zone the player is in
- what weather, day phase, and season are active
- what progression values are live
- what shortcuts are available during testing

## 18. Persistence

Farming progression currently persists to a JSON save under Unity persistent
data.

### Current persistence events
- save shortcut
- application pause
- application quit

### Current persistence scope
Progression is persisted. The world farming slice already has the save seam in
place for broader state persistence later.

## 19. Relationship To The Animal Pen

The chicken-coop side of the world is no longer only atmospheric. It now also
hosts the world animal pen game. That means the world slice currently supports:

- crop farming progression
- environmental farming simulation
- animal-side gameplay expansion in the same scene

This is important when explaining the game to the team because it shows that the
farm is already becoming a multi-activity space rather than a single mechanic
prototype.

## 20. Team Demo Path

Use this flow when showing the current implementation:

1. Open `WorldMain`.
2. Enter Play mode.
3. Walk to the plots and plant on one square.
4. Water it.
5. Force rain with `Shift+I`.
6. Show rain atmosphere and explain auto-watering plus growth acceleration.
7. Show the world overlay updating weather, day phase, season, and progression.
8. Move to the farmhouse side and sell with `K`.
9. Buy an upgrade with `U` or unlock an expansion hook with `O`.
10. Spend skill points with `1`, `2`, or `3`.
11. Walk to the coop and explain the connected animal-side activity.

## 21. Key Talking Points

- Farming is now integrated into the open world, not isolated to a sandbox
  scene.
- Plot interaction is direct and readable.
- Soil, crop visuals, weather, sun, and season logic all affect the same plots
  in the world.
- Progression is attached to the farming loop, not separate from it.
- The world slice already supports future tools and animal-side expansion.
- The current implementation is demoable, testable, and explainable to new team
  members.
