# Design Doc: Game Intro Cinematic — "The Awakening"

**Date:** 2026-04-07
**Author:** Youssef + Claude
**Fidelity:** Greybox (primitives, text dialogue, placeholder audio)

## Overview

A 4-act intro sequence that establishes the game's story: a loud chicken terrorizes the town at night, the player investigates, chases and catches "El Pollo" in a farm pen, and inherits the farm as reward. The intro transitions seamlessly into gameplay.

## Script Reference

See storyboard image and full script in project docs. Acts:
- **Act I: The Awakening** — Player sleeps, chicken screams, town wakes up
- **Act II: The Moonlit Trek** — Player walks through town, meets Tenpenny
- **Act III: The Standoff** — Player enters pen, chases El Pollo (interactive gameplay)
- **Act IV: The Deed** — Catch chicken, receive farm deed, mission complete

## Systems Required (8 specs)

| Spec | System | Phase |
|------|--------|-------|
| INT-001 | Screen Effects (fade, shake, letterbox, popups) | 1 |
| INT-002 | Simple Audio Manager (music, SFX) | 1 |
| INT-003 | Dialogue System (typewriter text, speaker names) | 1 |
| INT-004 | Cinematic Camera (waypoints, follow, hold) | 1 |
| INT-005 | Cinematic Sequencer (orchestrator) | 3 |
| INT-006 | NPC Controller (capsules with dialogue) | 2 |
| INT-007 | Mission Manager (objectives, mission complete) | 2 |
| INT-008 | Intro Scene Assembly (everything wired) | 4 |

## Build Order

**Phase 1 (parallel, no deps):** INT-001, INT-002, INT-003, INT-004
**Phase 2 (needs Phase 1):** INT-006, INT-007
**Phase 3 (needs all systems):** INT-005
**Phase 4 (final assembly):** INT-008

## Greybox Scope (What We Build)

- Primitive shapes for all geometry (cubes = houses, capsules = NPCs)
- Text-on-screen dialogue (TMPro, typewriter effect)
- Placeholder audio (free SFX, no voice acting)
- Camera waypoint lerps (no Cinemachine)
- El Pollo chase with 3 phases + Chaos Meter

## Deferred to Polish Pass

- Character animations (farmgirl, NPCs)
- Voice acting / DeepVoice TTS integration
- Particle effects (dust, feathers)
- Real skybox blending (night→dawn)
- Cinemachine virtual cameras
- Real building models
