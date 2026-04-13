# Single Source of Truth — FarmSim VR

Last updated: 2026-04-13

## Current State

- **Current story**: none
- **Story phase**: idle
- **Recent fix**: The EditMode harness now clones a disposable project copy, runs tests through a repo-owned `BatchmodeTestRunner`, and emits NUnit XML even while the main Unity editor keeps the live project locked; Town dialogue follow-up choices now compose from the streamed reply text instead of a generic fallback ladder
- **Scene work map**: `.ai/docs/scene-work-map.md`
- **Tests**: 0 EditMode passing, 0 PlayMode passing
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
