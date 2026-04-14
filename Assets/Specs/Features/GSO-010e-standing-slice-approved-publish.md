# Feature Spec: Standing Slice Approved Publish — GSO-010e

## Summary
Standing-slice jobs now have durable review state and immutable archived
artifacts. What is still missing is the deliberate promotion step from a
reviewed job back into the live standing slice that Unity consumes.

This slice adds an approval-gated publish action:
- only approved jobs can be published
- publishing copies the archived package snapshot back to the live story
  package path
- publishing restores the archived media files back to their live shared asset
  paths
- the job record persists that it has been published

## User Story
As a developer, I want to publish one approved standing-slice job into the live
standing package and asset paths, so the title-screen generative slice updates
only when I explicitly promote a reviewed run.

## Acceptance Criteria

### Publish Guardrails
- [ ] A standing-slice publish endpoint exists.
- [ ] Publishing an unknown job returns `404`.
- [ ] Publishing a non-approved job returns a conflict/error instead of
      mutating the live package.

### Live Publish Behavior
- [ ] Publishing copies the archived package snapshot to the configured live
      story package path.
- [ ] Publishing copies archived image/audio files back to their original live
      shared generation paths.
- [ ] Publishing also restores archived alignment sidecar files when present.
- [ ] The publish flow validates both archived source paths and live
      destination paths against the configured storyboard output root.

### Persistence
- [ ] Standing-slice jobs persist publish status.
- [ ] Published jobs persist `published_at`.
- [ ] A fetched published job reflects that state.

### Operator Surface
- [ ] The local standing-slice review page can trigger publish for an approved
      job.
- [ ] The page shows the persisted publish state.

## Non-Negotiable Rules
- Do not auto-publish on generation completion.
- Do not allow non-approved jobs to overwrite the live standing slice.
- Do not publish from mutable shared job paths; publish must source archived
  immutable snapshots.

## Out Of Scope
- Rollback history across multiple published jobs
- Multi-environment publish targets
- Unity auto-refresh beyond writing the live package/assets

## Done Definition
- [ ] Spec exists.
- [ ] Approved jobs can be published into the live standing slice.
- [ ] Publish state persists on the job record.
- [ ] Focused tests cover approval guardrails and live-path restoration.
