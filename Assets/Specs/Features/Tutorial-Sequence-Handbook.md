# Tutorial Sequence Handbook

This page explains the intended onboarding flow for FarmSim VR in plain
language so the whole team can discuss the same structure.

## Core Decision
The tutorial is not the open world.

`WorldMain` stays in the project and can remain useful for sandbox or later
integration work, but first-time players should not be onboarded through it.

The tutorial should instead be a clean linear chain of separate scenes with
stable handoff labels:

1. `Scene 01` — `Intro Cutscene`
2. `Scene 02` — `Chicken Hunt Game`
3. `Scene 03` — `Post-Chicken Cutscene`
4. `Scene 04` — `Placeholder Cutscene`
5. `Scene 05` — `Find Tools Game`
6. `Scene 06` — `Pre-Farm Cutscene`
7. `Scene 07` — `Farm Tutorial`

## Why This Is Better
The current game already has several isolated pieces that make more sense as a
scene-by-scene onboarding route than as a single world-driven tutorial.

This approach gives us:
- a clearer first-time-player experience
- smaller scenes that are easier to test
- cleaner pacing
- less accidental complexity
- less pressure to finish the whole open world before the tutorial works

## What Each Scene Is Supposed To Do

### 1. Intro Cutscene
This is the tone-setter. It should tell the player what kind of world they are
in and move them quickly into the first playable task.

It should not try to explain every mechanic.

### 2. Chicken Hunt Game
This is the first simple game loop.

Its job is to give the player a clear action, a clear success condition, and a
quick win. It teaches that the game is tactile and readable.

It should stay focused. It does not need to introduce farming or progression.

### 3. Post-Chicken Cutscene
This is a short narrative bridge after the first minigame.

It should acknowledge what happened and gently push the player toward the next
activity.

### 4. Placeholder Cutscene
This exists to preserve the full tutorial structure even if some narrative
content is not ready yet.

It can be very simple for now. The important part is that it exists as its own
scene in the chain.

### 5. Find Tools Game
This is the second gameplay beat.

Its job is to teach that before farming starts, the player needs to recover the
basic tools. This should feel like its own contained tutorial game, not like a
full open-world scavenger hunt.

The target learning outcome is:
- find the tools
- understand that tools matter
- become ready for farming

### 6. Pre-Farm Cutscene
This is the final narrative bridge before farming starts.

Its job is to put the player in the right mindset for the farm loop and make
the transition feel deliberate.

### 7. Farm Tutorial
This is the final onboarding payoff.

Its job is to teach the smallest complete farm loop:
- plant
- water
- observe growth
- harvest

This scene should not try to teach every advanced farming system at once.

## What The Tutorial Explicitly Avoids
For now, the tutorial should not depend on:
- `WorldMain`
- advanced open-world quest routing
- large progression systems
- economy depth
- deep season strategy
- complicated weather logic
- broad farm expansion

Those things can exist elsewhere in the project, but they should not block a
clean tutorial.

## What Still Matters In The Tutorial
The tutorial should still feel like the real game.

That means:
- clear scene transitions
- readable interaction prompts
- cozy pacing
- simple cutscene glue
- minimal but real gameplay

If weather is present in the farm tutorial, keep it simple and readable:
- sun should make the farm feel welcoming
- rain should clearly help the farm

If seasons are integrated later, they should layer on after the farm tutorial
works, not become a blocker.

## Existing Scene Mapping
Current repo scenes already give us a strong starting point:

| Scene | Tutorial Beat | Current Scene Candidate |
|---|---|---|
| Scene 01 | Intro Cutscene | `Assets/_Project/Scenes/Intro.unity` |
| Scene 02 | Chicken Hunt Game | `Assets/_Project/Scenes/ChickenGame.unity` |
| Scene 03 | Post-Chicken Cutscene | `Assets/_Project/Scenes/Tutorial_PostChickenCutscene.unity` |
| Scene 04 | Placeholder Cutscene | `Assets/_Project/Scenes/Tutorial_MidpointPlaceholder.unity` |
| Scene 05 | Find Tools Game | `Assets/_Project/Scenes/FindToolsGame.unity` |
| Scene 06 | Pre-Farm Cutscene | `Assets/_Project/Scenes/Tutorial_PreFarmCutscene.unity` |
| Scene 07 | Farm Tutorial | `Assets/_Project/Scenes/FarmMain.unity` |
| Scene 08 | World Sandbox | `Assets/_Project/Scenes/WorldMain.unity` |

The bridge cutscenes should be added as dedicated scenes instead of forcing
those transitions through the world.

## Shared Systems We Can Reuse
We do not need a heavy new architecture for this.

The tutorial can reuse:
- [`SceneLoader`](/Users/youss/My%20project/Assets/_Project/Scripts/MonoBehaviours/SceneLoader.cs)
- [`TitleScreenManager`](/Users/youss/My%20project/Assets/_Project/Scripts/MonoBehaviours/TitleScreenManager.cs)
- [`MissionManager`](/Users/youss/My%20project/Assets/_Project/Scripts/MonoBehaviours/Cinematics/MissionManager.cs)
- existing intro/cinematic systems
- existing chicken-hunt content
- existing farm tutorial work

What should be new is only a tiny tutorial flow layer that remembers which step
the player is on and what scene comes next.

## Minimal Technical Shape
The tutorial needs:
- one small flow controller
- one small step/state model
- one clear completion rule per scene
- one next-scene target per scene
- dev shortcuts for jumping between tutorial scenes

That is enough.

## Recommended Dev Shortcuts
To make iteration fast, the tutorial should support:
- `Shift+.` next scene
- `Shift+,` previous scene
- `Shift+/` reload current scene
- `Shift+1` to `Shift+7` jump to tutorial scenes
- `Shift+0` reset tutorial flow

There should also be a small overlay showing:
- scene number
- current tutorial step
- current scene
- next scene

## Team Handoff Rule

When dividing work, refer to the beats by scene number first and by scene name
second.

Good examples:

- `Take Scene 03 and tighten the bridge cutscene pacing.`
- `Work on Scene 05 only. Leave shared tutorial flow files alone unless needed.`
- `Scene 08 is the world sandbox, not the onboarding route.`

The detailed AI-facing handoff map lives in `.ai/docs/scene-work-map.md`.
- current completion state

## Implementation Priority
The order should be:

1. tutorial flow contract
2. intro to chicken transition
3. chicken to cutscene transition
4. placeholder cutscene chain
5. find tools game
6. pre-farm bridge
7. farm tutorial
8. shortcuts and build settings cleanup

## Final Team Rule
When discussing the tutorial, treat it as a sequence of self-contained beats,
not as a watered-down version of the open world.

That rule will keep the onboarding simple, testable, and finishable.
