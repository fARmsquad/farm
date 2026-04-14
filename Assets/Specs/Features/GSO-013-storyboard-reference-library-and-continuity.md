# Feature Spec: Storyboard Reference Library And Continuity Selection — GSO-013

## Summary
The orchestrator already calls Gemini for image generation and can pass
explicit `reference_image_paths` into the request, but continuity is still
manual. There is no first-class place to upload character sheets, style refs,
or prior approved art for later reuse, and there is no automatic selection of
continuity references from prior generated storyboard assets.

This slice adds a reference-asset foundation so the orchestrator can:
- accept uploaded storyboard references and character sheets
- persist them in a local library
- automatically combine uploaded character references with prior generated art
  when building Gemini image requests

## User Story
As a developer, I want to upload character and art references to the
orchestrator and let it reuse prior generated shots when appropriate, so Gemini
can preserve visual continuity across generated cutscenes.

## Acceptance Criteria

### Reference Library
- [ ] The backend can accept uploaded storyboard reference assets.
- [ ] A reference asset stores:
      `reference_id`, `reference_role`, `label`, `character_name`, `tags`,
      `stored_path`, `mime_type`, and timestamps.
- [ ] Uploaded reference assets persist across orchestrator restarts.
- [ ] The backend can list stored reference assets.

### Continuity Selection
- [ ] Generated storyboard requests support bounded continuity behavior with an
      auto mode by default.
- [ ] In auto mode, reference selection can combine:
      - explicit request reference paths
      - uploaded character reference assets for the current character
      - recent generated image assets from the same package
- [ ] Reference selection deduplicates paths and enforces a bounded maximum.
- [ ] The final reference paths used for Gemini are recorded in generated asset
      metadata.

### API Surface
- [ ] The backend exposes an upload endpoint for storyboard reference assets.
- [ ] The backend exposes a listing endpoint for storyboard reference assets.
- [ ] Existing storyboard generation endpoints remain backward compatible.

### Integration Proof
- [ ] One backend test proves a reference upload persists and can be listed.
- [ ] One backend test proves storyboard generation auto-selects uploaded
      character references plus prior generated package art.

## Non-Negotiable Rules
- Do not require Unity to know filesystem paths for continuity references.
- Do not make continuity depend on one hardcoded reference image.
- Do not let reference selection grow unbounded with every prior shot.
- Do not replace explicit references; merge them with auto-selected continuity
  references in a deterministic order.

## Out Of Scope
- Semantic image embeddings or visual similarity search
- Approval workflows for reference assets
- Per-shot manual masking or edit instructions
- Automatic character naming from image analysis

## Done Definition
- [ ] Spec exists.
- [ ] Character/style reference uploads are persisted by the orchestrator.
- [ ] Gemini image requests can reuse uploaded references and previous package
      art automatically.
- [ ] Focused backend tests cover upload/list and continuity reference
      selection.
