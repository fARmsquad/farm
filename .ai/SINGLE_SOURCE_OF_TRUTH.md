# Single Source of Truth — FarmSim VR

Last updated: 2026-04-14

## Current State

- **Current story**: none
- **Story phase**: idle
- **Recent fix**: Story-orchestrator bootstrap failures now explain the real local setup contract instead of collapsing into a generic "did not become healthy" timeout: `LocalStoryOrchestratorLauncher` now includes `backend/story-orchestrator/.env.local` guidance, venv bootstrap instructions, and relevant launcher-log summaries in Unity-side failure messages, while `backend/story-orchestrator/start_local_backend.sh` logs its own startup preflight, loads `.env.local`, and warns about missing provider keys. Focused EditMode coverage passed in `LocalStoryOrchestratorLauncherTests`. The standalone portal runtime bootstrap fix remains in place.
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
