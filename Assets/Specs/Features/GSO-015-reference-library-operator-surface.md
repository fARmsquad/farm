# Feature Spec: Reference Library Operator Surface — GSO-015

## Summary
The storyboard reference library can already persist uploaded character and
style assets, but it still lacks an operator-facing surface for real use.
Today, a developer has to call raw API endpoints to upload reference files,
list them, and verify what the orchestrator will reuse for Gemini continuity.

This slice adds a lightweight reference-library section to the standing-slice
review page plus a safe file-serving endpoint for uploaded reference assets.
The goal is to make reference upload, inspection, and preview part of the
normal local orchestrator workflow.

## User Story
As a developer, I want to upload and inspect storyboard reference assets from
our local operator page, so I can manage continuity inputs for generated
cutscenes without manually calling the API.

## Acceptance Criteria

### Reference Asset Content
- [ ] The backend exposes a content endpoint for a stored storyboard reference
      asset by `reference_id`.
- [ ] The content endpoint rejects unknown reference ids.
- [ ] The content endpoint rejects stored paths outside the reference-library
      asset root.
- [ ] The content endpoint rejects missing files.

### Operator Surface
- [ ] The standing-slice review page includes a Reference Library section.
- [ ] The page can upload a reference asset using multipart form data.
- [ ] The page can fetch the current reference-library listing.
- [ ] The page renders preview cards for stored reference assets.
- [ ] Image references render inline previews from the new content endpoint.
- [ ] The page explains that auto continuity uses uploaded references when the
      cutscene `character_name` matches.

### Testing
- [ ] A backend test proves a stored reference asset can be streamed by the new
      content endpoint.
- [ ] A backend test proves the standing-slice review page contains the new
      reference-library operator surface.

## Non-Negotiable Rules
- Do not serve arbitrary filesystem paths from the manifest without validating
  them against the reference-library asset root.
- Do not add a separate frontend framework for this slice.
- Do not make operators leave the existing standing-slice review page just to
  upload or inspect references.

## Out Of Scope
- Editing or deleting existing reference records
- Bulk uploads
- Semantic search across references
- Unity-side in-scene reference management

## Done Definition
- [ ] Spec exists.
- [ ] Operators can upload and preview reference assets from the local review
      page.
- [ ] Reference content is served through a constrained backend endpoint.
- [ ] Focused backend tests cover the endpoint and review-page surface.
