# Feature Spec: Standing Slice Immutable Artifact Snapshots — GSO-010d

## Summary
Standing-slice jobs now persist rich metadata, review notes, and per-asset
provenance. But the actual generated files still live at shared package-level
paths such as:

- `GeneratedStoryboards/<package_id>/<beat_id>/shot_01.png`
- `GeneratedStoryboards/<package_id>/<beat_id>/shot_01.mp3`

That means a newer regeneration can overwrite the files that an older stored
job still points at. The job record survives, but the reviewed artifact no
longer represents the original run.

This slice makes standing-slice jobs immutable for review by snapshotting the
package JSON and generated media into a job-scoped archive root.

## User Story
As a developer reviewing generated slices, I want each stored standing-slice
job to keep its own immutable package and media artifacts, so later runs cannot
silently replace what I already reviewed.

## Acceptance Criteria

### Immutable Job Snapshots
- [ ] Each standing-slice job archives a package snapshot under a job-scoped
      directory.
- [ ] Each generated image/audio asset is copied into a job-scoped archive
      directory.
- [ ] Audio alignment sidecar files are also archived when present.
- [ ] Stored job records point at archived asset paths instead of mutable
      shared generation paths.

### Review Integrity
- [ ] Fetching an older job after a newer regeneration still returns the
      original archived content for the older job.
- [ ] The standing-slice asset-content endpoint serves archived job artifacts.
- [ ] Archived paths remain under the configured storyboard output root so the
      existing constrained file-serving guard still applies.

### Provenance
- [ ] Archived asset metadata preserves the original source path.
- [ ] Archived audio metadata preserves the original alignment path when one
      existed.
- [ ] Stored package output paths inside standing-slice job results point at
      the archived package snapshot for that job.

### Integration Rules
- [ ] Direct regeneration endpoints may continue writing the live standing
      package and shared beat assets.
- [ ] Immutable archiving happens in the job flow, not by duplicating the
      generation logic itself.
- [ ] Existing review/status APIs remain intact.

## Non-Negotiable Rules
- Do not leave persisted standing-slice jobs pointing only at mutable shared
  asset paths.
- Do not break the current standing title-slice generation path while adding
  snapshots.
- Do not move archived artifacts outside the validated storyboard output root.

## Out Of Scope
- Publish/promote approved jobs into runtime
- Asset deduplication across jobs
- Cleanup policies or retention windows
- Multi-job list/history UI

## Done Definition
- [ ] Spec exists.
- [ ] Job creation archives immutable package and asset snapshots.
- [ ] Older jobs keep serving their original content after newer runs.
- [ ] Focused tests cover snapshot paths and old-job stability.
