# World Farming Playable Slice — Team Explainer

## What This Is
This is the focused `WorldMain` farming slice. Instead of exposing the full
prototype world and all its unfinished demo areas, the scene now behaves like a
compact vertical slice built around three spaces:

- `Farm Plots`
- `Farm House`
- `Chicken Coop`

The goal is to make the world readable, testable, and demo-ready without
waiting for every other world system to be complete.

## Why We Reduced The World
Before this pass, `WorldMain` contained multiple unrelated zones, demo roots,
duplicate scene systems, and leftover exploration content. That made it hard to
evaluate farming as a cohesive experience.

The playable slice now deactivates unrelated clutter at runtime and keeps only
the content needed to explain the farming loop and its future hooks.

## The Three World Zones

### 1. Farm Plots
This is where the player performs the core farming loop.

What happens here:
- look at a plot
- plant seeds
- water crops
- wait for growth
- harvest
- compost depleted plots

Why it matters:
- proves the farming loop in the actual world scene
- shows weather affecting the same plots the player is standing in
- gives progression something real to modify

### 2. Farm House
This is the planning and meta-progression hub.

What happens here:
- sell harvested crops
- buy watering upgrades
- unlock the next expansion hook
- spend skill points
- save and load farming progression

Why it matters:
- turns harvesting into forward progress
- makes the house meaningful without needing a full interior gameplay loop yet
- gives the team a clear place to anchor future economy and upgrade UX

### 3. Chicken Coop
This is the animal-facing connection point.

What happens here right now:
- the zone is preserved and atmospherically active
- it now hosts the in-world animal pen game
- it gives the world slice a second gameplay-adjacent destination beyond crops

Why it matters:
- keeps farming connected to the broader farm fantasy
- preserves the chicken space as a future expansion target
- makes the slice feel like a farm, not just six isolated soil squares

## Core Farming Loop In WorldMain
The world slice uses the same direct plot workflow already built in the farming
prototype:

1. Walk up to a plot
2. Look directly at it
3. Use the plot prompt to choose an action
4. Watch the soil and crop visuals update in place
5. Harvest crops into inventory
6. Go to the house zone to convert that harvest into progression

This keeps farming grounded in the world instead of hidden in a debug panel.

## Input And Interaction Reference

### Plot Actions
- `T` plant tomato
- `C` plant carrot
- `L` plant lettuce
- `P` water
- `H` harvest
- `M` compost

These appear contextually when the player looks at a valid plot.

### World Shortcuts
- `Shift+Y` force sunny weather
- `Shift+U` force cloudy weather
- `Shift+I` force rain
- `Shift+O` return weather to automatic mode
- `Shift+P` save farming progression
- `Shift+L` load farming progression
- `K` sell harvested crops
- `U` buy watering-can upgrade
- `O` unlock the next expansion hook
- `[` add 100 coins
- `]` add 100 XP
- `1` spend a point on Green Thumb
- `2` spend a point on Merchant
- `3` spend a point on Rain Tender

## Weather Integration
Weather already affects the farming loop directly in world space.

### Rain
- automatically waters outdoor plots
- accelerates crop progress
- activates rain atmosphere particles
- shifts the mood of the slice visually and aurally
- becomes stronger if the player invests in `Rain Tender`

### Cloudy
- shifts lighting and mood
- acts as a softer environmental state between sun and rain

### Sunny
- baseline outdoor farming condition

## Season Integration
Season work is still being built in parallel, but this slice is already wired to
adopt it automatically.

When the season driver is present:
- the reference overlay shows the active season
- crop suitability continues to use the planted crop, not just the currently
  selected seed
- world farming systems pick up the same seasonal context without requiring a
  second integration pass

This means the slice is future-compatible with the season implementation
instead of blocking on it.

## Progression System
The slice now has a lightweight but functional progression layer.

### Economy
- harvested crops go into inventory
- selling converts them into coins
- coins are spent on upgrades and expansion hooks

### XP And Levels
- sales award XP
- every 100 XP grants a level
- each level grants 1 skill point

### Upgrades
Current upgrade path:
- watering can tier increases watering effectiveness

### Expansion Hooks
Current hook ladder:
- Starter Farm
- Expanded Plots
- House Workbench
- Coop Upgrade

These are hooks, not full content drops yet. Their job is to prove that
farming progress opens future space.

### Skills
There are 3 current skill tracks:

- `Green Thumb`
  - increases crop growth speed
- `Merchant`
  - increases sale value
- `Rain Tender`
  - makes rain watering stronger

This keeps progression tightly attached to the existing farming loop.

## Atmosphere Layer
The slice now has an atmosphere pass designed to make it feel alive without
overcomplicating the gameplay.

### Audio Mood
- farm ambience near the plots
- softer house ambience near the farmhouse
- chicken ambience in the coop zone
- intermittent coop calls for life and variation

### Particle Mood
- pollen or dust around the plots
- chimney-like smoke near the house
- coop dust in the pen area
- rain curtain particles when weather is rainy

### Educational Surface
The on-screen reference overlay explains:
- what zone the player is in
- what the current weather/day/season state is
- what progression modifiers are active
- what each area of the slice is for

This makes the scene self-explanatory during demos.

## Persistence
The slice now saves farming progression separately.

What persists:
- coins
- XP
- level
- skill points
- watering-can tier
- expansion level
- skill ranks

What this means:
- the player can progress across sessions
- demos can show that farming is not just a disposable test scene

## What The On-Screen GUI Reference Is Doing
The top-left overlay is intentionally not just a debug box.

It serves four purposes:
- tells the player where they are
- shows the current farming state at a glance
- explains what each zone is for
- gives the developer or tester the active shortcut map without leaving play mode

This is the fastest way to explain the system to a new teammate while the slice
is running.

## How To Demo This To The Team

### Demo Path
1. Open `WorldMain`
2. Enter Play mode
3. Walk to the farm plots and plant a crop
4. Force rain with `Shift+I`
5. Show that rain changes atmosphere and helps the plots
6. Fast-forward or wait until harvest
7. Harvest the crop
8. Move to the farmhouse zone
9. Sell the crop with `K`
10. Use `U` or `O` to show progression converting into future capability
11. Spend a point on `1`, `2`, or `3`
12. Walk to the chicken coop and start the pen game with `G`
13. Save with `Shift+P` and reload with `Shift+L`

### What To Call Out
- the world is intentionally reduced to improve clarity
- farming is no longer isolated from the world scene
- progression is now attached to the world loop
- weather is no longer cosmetic only
- the house and coop both have explicit roles in the slice
- seasons will slot into the same surface once that branch lands

## Talking Points For The Team
- We are no longer demoing “just a prototype map”; we are demoing a focused
  farm slice.
- The three destinations each have a purpose: action, planning, future animal
  expansion.
- Progression is now visible and testable in world space.
- Atmosphere is supporting clarity, not distracting from mechanics.
- The slice is intentionally compact so iteration stays fast.
- Seasons are designed to snap into this slice without another major rewrite.

## Current Limits
- This is still a vertical slice, not the final open world.
- The house is currently a meta hub, not a full interior gameplay space.
- The coop is currently a progression and atmosphere hook, not a full chicken
  system.
- The progression layer is intentionally lightweight and will deepen later.

## Bottom Line
This world slice is the first version of `WorldMain` that explains the farming
game cleanly:

- where you farm
- where you plan
- where animals connect
- how weather affects crops
- how progression grows the experience
- how the player can test all of it quickly

That is the version worth showing to the team.
