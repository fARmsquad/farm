# Single Source of Truth — FarmSim VR

Last updated: 2026-04-14

## Current State

- **Current story**: none
- **Story phase**: idle
- **Recent fix**: The generated title-screen flow now hands `PostChickenCutscene` over to the runtime story package instead of the old scene-authored slideshow path: `TutorialSceneInstaller` binds the generated storyboard through `TutorialCutsceneSceneController`, disables the authored `SlideshowPanel` / `SlideshowDirector` bridge objects for that scene, and focused EditMode coverage locks both the runtime storyboard binding and the title-screen ready-state recovery. The title-screen diagnostics block also now shows a `Next Step` line plus a more explicit generation message so you can tell whether the pipeline is waiting on story writing, image generation, narration, or scene load, and the local story-orchestrator now writes per-step backend logs for turn planning, minigame assembly, Gemini image requests, ElevenLabs narration attempts, quality-gate rejections, and provider fallbacks. Unity now also owns the local backend bootstrap in development: the title screen warms the story-orchestrator in the background, generated-story requests wait for local orchestrator readiness before hitting HTTP, Town voice token requests do the same, and Unity launches the backend through the repo-owned `backend/story-orchestrator/start_local_backend.sh` path instead of relying on a manual terminal step.
- **Scene work map**: `.ai/docs/scene-work-map.md`
- **Tests**: EditMode 401 passed / 85 failed (legacy baseline still red outside this slice, plus unstable package-content expectations unrelated to this launcher fix); PlayMode not run for this slice
- **Main status**: green
- **Last push**: none yet
- **Tech debt items**: 0 (see project-memory.md)

## Architecture Snapshot
- Core/ systems: (none yet)
- MonoBehaviour/ controllers: (none yet)
- Interfaces/: (none yet)
- ScriptableObjects/: (none yet)
- Active third-party packages: (default Unity 6 packages)

## Open Questions
- XR Interaction Toolkit version to target
- Whether to use Unity Input System or XR-specific input

## Next Up
- Bootstrap feature: Crop Growth Calculator (validates harness)
- Core farming systems: CropData, GrowthConditions, GrowthResult
