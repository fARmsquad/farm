# Tutorial Sequence Playtest Guide

## Goal
Verify the full linear tutorial path now runs as isolated scenes:

`TitleScreen -> Intro -> ChickenGame -> Post-Chicken Cutscene -> Placeholder Cutscene -> FindToolsGame -> Pre-Farm Cutscene -> FarmMain`

`WorldMain` should remain untouched and outside this path.

## Scenes
- `Assets/_Project/Scenes/TitleScreen.unity`
- `Assets/_Project/Scenes/Intro.unity`
- `Assets/_Project/Scenes/ChickenGame.unity`
- `Assets/_Project/Scenes/Tutorial_PostChickenCutscene.unity`
- `Assets/_Project/Scenes/Tutorial_MidpointPlaceholder.unity`
- `Assets/_Project/Scenes/FindToolsGame.unity`
- `Assets/_Project/Scenes/Tutorial_PreFarmCutscene.unity`
- `Assets/_Project/Scenes/FarmMain.unity`

## Core Verification
1. Open `TitleScreen.unity`.
2. Enter Play mode.
3. Start the game.
4. Confirm the title screen fades into `Intro`, not `WorldMain`.
5. Let the intro finish or skip it.
6. Confirm the next scene is `ChickenGame`.
7. Win the chicken game by catching the chicken and dropping it in the coop.
8. Confirm the tutorial auto-advances to `Tutorial_PostChickenCutscene`.
9. Let the cutscene auto-advance or press `Space`.
10. Confirm the placeholder cutscene scene loads next.
11. Advance again and confirm `FindToolsGame` loads.
12. Recover all three starter tools.
13. Confirm the scene advances into `Tutorial_PreFarmCutscene`.
14. Advance again and confirm `FarmMain` loads.
15. Plant, water, and harvest at least one crop.
16. Confirm the tutorial finishes without loading `WorldMain`.

## Find Tools Checks
- Player can move with `WASD` and look with the mouse.
- Looking at a tool shows a clear `[E] Recover ...` prompt.
- Recovering a tool hides or disables that pickup.
- The objective panel updates tool-by-tool.
- Once all tools are recovered, the scene advances automatically.

## Farm Tutorial Checks
- Farm plots still use the look-at interaction flow.
- Prompt keys still read correctly:
  - `T` tomato
  - `C` carrot
  - `L` lettuce
  - `P` water
  - `H` harvest
  - `M` compost
- Tutorial panel updates in order:
  - plant
  - water
  - harvest
- `Shift+G` fast-forwards crop growth for testing.
- Weather and season readouts appear if their drivers are present.

## Dev Shortcuts
- `Shift+.` force complete current tutorial scene and go next
- `Shift+,` go back one tutorial scene
- `Shift+/` reload current tutorial scene
- `Shift+1` jump to intro
- `Shift+2` jump to chicken game
- `Shift+3` jump to post-chicken cutscene
- `Shift+4` jump to midpoint placeholder
- `Shift+5` jump to find tools
- `Shift+6` jump to pre-farm cutscene
- `Shift+7` jump to farm tutorial
- `Shift+0` reset the tutorial and restart from intro

## Expected Outcome
- The tutorial behaves as a clean, linear onboarding route.
- Each scene teaches one thing only.
- `FindToolsGame` works as a dedicated isolated gameplay beat.
- `FarmMain` serves as the final tutorial scene, not an open-world dependency.
- `WorldMain` remains available but is not part of the required onboarding path.
