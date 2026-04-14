# Codex Session Memory — FarmSim VR

## Last Session
(no Codex sessions yet)

## Active Tasks
- 2026-04-13: Continue Generative Story Orchestrator by adding durable
  submission/status, lightweight operator review, and per-asset provenance for
  the standing-slice regeneration flow.
- 2026-04-14: Continue the standing-slice operator flow by persisting review
  notes and approval actions through the backend and local review page.
- 2026-04-14: Make standing-slice job artifacts immutable by archiving
  package/media snapshots per job so later regenerations cannot overwrite older
  reviewed runs.
- 2026-04-14: Add approval-gated publish from archived standing-slice jobs back
  into the live package and shared asset paths.
- 2026-04-14: Pivot the GSO backend toward autonomous sequence sessions that
  persist story context and generate the next cutscene/minigame turn
  automatically without approval gates.
- 2026-04-14: Bridge Unity runtime consumption onto backend sequence sessions
  so the title-screen Generative Story Slice can bootstrap and continue live
  generated turns instead of reading only the static fallback package.
- 2026-04-14: Fix intermittent Town voice fallback where Unity timed out token
  minting before the local backend and ElevenLabs could answer.
- 2026-04-14: Redesign the Town dialogue HUD so `V` voice input, waiting
  states, and long reply buttons render adaptively without overlapping.

## Notes
- 2026-04-14: GSO-011b is now in place. Unity can bootstrap a live
  story-sequence session from the title-screen `Generative Story Slice`,
  inject the returned package into an in-memory runtime override, and continue
  from generated terminal minigames into the next generated cutscene.
- 2026-04-14: Four new EditMode tests for the runtime bridge passed:
  exact scene-name preference, runtime override lookup, generated bootstrap,
  and generated advance into a later turn.
- 2026-04-14: The full EditMode suite is still red for unrelated dirty-tree
  failures in farming, materials, MCP Unity tool tests, and the overwritten
  authored story package snapshot. Do not treat a red global suite as caused
  by GSO-011b without checking the per-test results.
- 2026-04-14: GSO-012 is in place. `tutorial.plant_rows` now rounds desired
  plots up to full rows instead of multiplying by row count, and
  `tutorial.chicken_chase` now consumes generated timer, arena preset,
  guidance level, and multi-capture progress inside the existing scene.
- 2026-04-14: Focused EditMode coverage for GSO-012 passed:
  rounded plant-row count, chicken mission multi-capture progress, and
  package-driven chicken runtime config application.
- 2026-04-14: After the GSO-012 pass, the full EditMode suite remains red but
  improved from 79 to 78 failures. The remaining failures are still unrelated
  farming/material/MCP debt plus stale authored-package expectation tests.
