# Feature Spec: Standing Slice Review Surface And Asset Provenance — GSO-010b

## Summary
The standing-slice backend can now regenerate a package and persist the job
with per-step outputs. What is still missing is the operator-facing layer that
makes those records useful in practice:
- no lightweight status/review surface
- no submit/fetch tool above raw API calls
- no first-class persisted asset provenance per generated image/audio asset

This slice adds a backend-served local operator page for the standing slice,
plus asset-level provenance persistence and content access. The page can submit
and fetch jobs, inspect per-step status, and review the actual generated assets
behind those steps.

## User Story
As a developer, I want a lightweight local review page that can submit and
inspect standing-slice jobs with per-asset provenance, so I can evaluate runs
without manually calling APIs or digging through filesystem paths.

## Acceptance Criteria

### Asset Provenance
- [ ] Storyboard generation returns per-asset provenance records for generated
      image and audio outputs.
- [ ] Package assembly results include the storyboard asset provenance.
- [ ] Standing-slice job storage persists asset records separately from step
      records.
- [ ] A fetched standing-slice job includes persisted asset records.

### Review Surface
- [ ] A backend-served local review/status page exists for the standing slice.
- [ ] The page can submit a standing-slice job with a prefilled sample request.
- [ ] The page can fetch an existing job by `job_id`.
- [ ] The page renders:
      - top-level job status
      - per-step status/errors
      - per-asset provenance details
      - links/previews for generated image/audio outputs

### Asset Content Access
- [ ] A constrained backend endpoint can serve persisted generated assets by job
      and asset ID.
- [ ] The endpoint rejects missing or out-of-root asset paths.

### Integration Rules
- [ ] The review page reads from persisted job records, not transient in-memory
      data.
- [ ] The local operator tool is backend-served; no Unity scene wiring is
      required in this slice.
- [ ] Existing standing-slice submit/fetch APIs remain intact.

## Non-Negotiable Rules
- Do not serve arbitrary filesystem paths without validating them against the
  configured storyboard output root.
- Do not add a separate frontend framework for this slice.
- Do not stop at step-level provenance when the user requested asset-level
  persistence.

## Out of Scope
- Authentication or multi-user review
- Publish/approve actions beyond display
- Unity-side in-scene review tooling
- Asset regeneration actions

## Done Definition
- [ ] Spec exists.
- [ ] Asset provenance persists per generated asset.
- [ ] A local operator page can submit and fetch standing-slice jobs.
- [ ] Asset previews/content can be opened through the backend.
- [ ] Focused tests cover provenance persistence, asset serving, and review page load.
