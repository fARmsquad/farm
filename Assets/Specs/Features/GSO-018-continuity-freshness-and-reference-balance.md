# Feature Spec: Continuity Freshness And Reference Balance — GSO-018

## Summary
The current sequence-session continuity path is preserving too much of the last
scene's composition. Prior generated storyboard frames are being fed back into
new cutscene requests strongly enough that the next generated image can look
like a near-clone of the previous shot instead of a fresh beat with shared
identity and style.

This slice rebalances continuity. Uploaded character references should dominate
identity, outfit, and style consistency. Session continuity should become a
light hint rather than the primary visual anchor, and the prompt should
explicitly ask for a fresh camera angle, pose, and staging.

## User Story
As a developer, I want generated cutscenes to stay visually consistent without
copying the last scene's composition, so each new beat feels genuinely new.

## Acceptance Criteria

### Continuity Selection
- [ ] Sequence sessions pass at most one generated continuity frame into the
      next cutscene request.
- [ ] The selected generated continuity frame is a stable anchor shot rather
      than the most copy-prone trailing shot from the previous beat.
- [ ] Sequence-session cutscene requests use a character-priority continuity
      mode so uploaded character references come before generated continuity
      frames.

### Reference Resolution
- [ ] When character-priority continuity mode is used and uploaded character
      refs exist, character refs are ordered before generated continuity refs.
- [ ] In character-priority continuity mode, generated continuity refs are kept
      to a light hint instead of a stack of near-duplicate prior shots.

### Prompting
- [ ] Storyboard image prompts explicitly say to use references for identity,
      outfit, palette, and style only.
- [ ] Storyboard image prompts explicitly forbid copying camera angle,
      composition, background layout, staging, or pose from prior images.

### Testing
- [ ] A story-sequence test proves the next turn carries only one continuity
      anchor path and uses character-priority mode.
- [ ] A reference-library test proves character refs are resolved ahead of
      explicit continuity refs in character-priority mode.
- [ ] A storyboard prompt test proves the anti-copy composition language is
      present.

## Non-Negotiable Rules
- Do not remove uploaded character references from the continuity path.
- Do not let prior generated cutscene frames dominate the reference set when
  dedicated character refs exist.
- Do not ask for continuity in a way that invites composition reuse.

## Out Of Scope
- Learned visual similarity scoring
- User-tunable continuity strength controls
- Replacing the standing slice entry point

## Done Definition
- [ ] Spec exists.
- [ ] Continuity references are lighter and character-first.
- [ ] Prompt wording explicitly asks for fresh composition.
- [ ] Focused backend tests lock the new behavior.
