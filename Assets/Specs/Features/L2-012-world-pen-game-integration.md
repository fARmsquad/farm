# Feature Spec: World Pen Game Integration — L2-012

## Summary
Integrate the existing animal-pen hunting loop directly into `WorldMain` so the
player can walk into the coop or pen area, get asked whether they want to play,
start the pen game in-place, and earn a separate animal-handling progression
track without leaving the open world.

## User Story
As a player, I want the animal pen to feel like a playable part of the world
instead of a disconnected test scene, so I can move from farming into the pen
game naturally and understand how that activity improves my handling stat over
time.

## Acceptance Criteria
- [ ] `WorldMain` installs the pen game alongside the existing farming slice.
- [ ] Entering the `Chicken Coop` zone shows a clear prompt asking whether the
      player wants to start the pen game.
- [ ] The player can start and stop the pen game in world space without loading
      another scene.
- [ ] The existing first-person world player is the transport layer for the pen
      game; no second locomotion rig is created.
- [ ] Wild animals spawn around the real pen location in `WorldMain`, not at
      world origin.
- [ ] Caught animals can be deposited into the world pen flow and appear inside
      the pen.
- [ ] The pen game has its own progression track with a separate handling stat
      point path.
- [ ] Pen progression is surfaced in an on-screen world reference and can be
      validated through developer shortcuts.
- [ ] The pen game remains soft-fail and readable: prompt, active state,
      deposit feedback, and end state are all explained.

## Product Intent
This feature is not a new standalone minigame scene. It is a world-space animal
activity that sits next to farming. The player should feel like they wandered
from the plots into the coop, chose to engage, and then used the same open
world avatar to play a compact catch-and-pen loop.

## VR Interaction Model
- Desktop validation remains keyboard-first.
- Starting the game is a readable prompt in the coop zone.
- Catching remains `E` for now through the existing hunting input abstraction.
- World movement stays on `FirstPersonExplorer`.
- No separate third-person hunting controller is introduced in `WorldMain`.

## Dependencies
- `L2-007` Hunting Chore
- `L2-011` World Farming Playable Slice
- Existing `WorldSceneBootstrap`, `WorldPlayableSlicePruner`, and zone flow

## Out of Scope
- Rebuilding the hunting loop from scratch
- New riding or chase mechanics
- Multiplayer pen gameplay
- Coop production systems like eggs or milk
- Replacing the existing farming overlay with a full UI framework

## Design Rules
- The pen game must feel world-native, not scene-swapped.
- Prompting the player is mandatory; the game does not auto-start on zone entry.
- Pen progression is separate from farming progression.
- The world player remains authoritative for movement and position.
- Dev testing must be first-class with shortcuts and a readable overlay.

## Technical Plan
- Add pure C# pen progression state and service under `Core/Hunting/`.
- Add a world-specific hunting catalog that resolves the config and animal
  prefabs needed at runtime.
- Add a `WorldPenBootstrap` path that:
  - ensures the world player has `KeyboardPlayerInput`
  - creates or configures runtime pen components
  - installs the pen prompt, overlay, dev shortcuts, and progression bridge
- Extend `WildAnimalSpawner` so its spawn origin can be configured relative to
  the real pen transform in `WorldMain`.
- Keep existing `AnimalPen`, `BarnDropOff`, and `CatchZone` behaviors where
  possible rather than reimplementing the loop.

## Testing Strategy
- EditMode tests for pen progression logic and snapshot round-trip.
- EditMode tests for world pen bootstrap and player wiring.
- EditMode tests for configured spawns happening around the pen transform.
- Manual `WorldMain` playtest for:
  - entering the coop zone
  - start prompt
  - active pen game flow
  - catch and deposit loop
  - pen progression increase
  - start/stop shortcuts

## Task Breakdown
1. Add failing tests for pen progression, world pen bootstrap, and spawn origin.
2. Implement pure pen progression state and service.
3. Implement world pen runtime catalog and bootstrap.
4. Fix spawner origin and expose minimal runtime configuration seams.
5. Add world prompt, overlay, and developer shortcuts.
6. Wire the pen bootstrap into `WorldSceneBootstrap`.
