# Feature Spec: Standing Slice Review Actions — GSO-010c

## Summary
The standing-slice backend now persists job records, per-step outputs, and
per-asset provenance, and it exposes a lightweight local operator review page.
What it still lacks is a real review decision layer.

This slice makes review state actionable instead of decorative by persisting
review notes plus explicit operator decisions:
- `pending_review`
- `approved`
- `rejected`

The same review state must be available through both the API and the local
standing-slice review surface.

## User Story
As a developer reviewing a generated story slice, I want to approve, reject, or
mark a job for review with notes, so I can track which runs are acceptable
without relying on memory or ad hoc comments.

## Acceptance Criteria

### Review Data
- [ ] Standing-slice jobs persist `review_notes` alongside `approval_status`.
- [ ] Existing databases can add `review_notes` without deleting prior jobs.
- [ ] A fetched job returns the latest review status and notes.

### Review Actions
- [ ] The backend exposes a review update endpoint for standing-slice jobs.
- [ ] The endpoint accepts a typed request body with `approval_status` and
      `review_notes`.
- [ ] Unknown jobs return `404`.
- [ ] Review updates refresh `updated_at`.

### Local Operator Surface
- [ ] The standing-slice review page includes a notes field for operator review.
- [ ] The page can mark a job as `pending_review`.
- [ ] The page can mark a job as `approved`.
- [ ] The page can mark a job as `rejected`.
- [ ] The rendered status panel shows the persisted review notes.

### Integration Rules
- [ ] Review actions update persisted job records, not transient page state.
- [ ] Existing submit/fetch/content routes continue to work unchanged.
- [ ] This slice remains local-only; no auth or Unity scene wiring is added.

## Non-Negotiable Rules
- Do not treat review notes as client-only text.
- Do not require a new frontend framework or separate operator app.
- Do not break previously created standing-slice jobs when the new field is
  introduced.

## Out Of Scope
- Multi-reviewer workflows
- Review history or audit timelines
- Publish-to-Unity actions after approval
- Auto-approval heuristics

## Done Definition
- [ ] Spec exists.
- [ ] Review status and notes persist in the job store.
- [ ] The API supports review updates and returns persisted values.
- [ ] The local review page can submit review decisions with notes.
- [ ] Focused tests cover store/service persistence, API review updates, and UI
      affordances.
