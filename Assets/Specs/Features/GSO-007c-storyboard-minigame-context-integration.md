# Feature Spec: Storyboard Minigame Context Integration — GSO-007c

## Summary
This slice connects the new generator-backed minigame beat path to storyboard
generation.

The storyboard service must be able to derive gameplay objective copy from a
materialized minigame beat that already exists in the story package, instead of
requiring a separate freeform `minigame_goal` string in the cutscene request.

The immediate use case is the intro chain:
- generator-backed planting beat exists in package JSON
- post-chicken bridge cutscene points at that minigame beat
- storyboard copy reflects the same objective text the minigame runtime will use

## User Story
As a developer, I want bridge cutscenes to pull their gameplay objective from
the minigame beat that follows, so narrative copy and gameplay config stop
drifting apart.

## Acceptance Criteria

### Request Contract
- [x] Storyboard requests can reference a linked minigame beat by ID.
- [x] `minigame_goal` becomes optional when a linked minigame beat is provided.
- [x] `crop_name` can be derived from linked minigame resolved parameters when
      available.

### Service Behavior
- [x] Before planning shots, the storyboard service can load the package and
      resolve linked minigame objective text.
- [x] If the linked minigame beat is missing or invalid, the service fails with
      a readable error.
- [x] If the cutscene request already provides `minigame_goal`, the service
      preserves it rather than overriding it.

### Integration Proof
- [x] One backend test proves a package can contain:
      - a generator-backed minigame beat
      - a generated storyboard cutscene that derives its objective from that beat
- [x] The resulting storyboard subtitle line matches the linked minigame
      objective text instead of a duplicated freeform request field.

## Non-Negotiable Rules
- Storyboard generation must not silently invent gameplay objective text when a
  linked minigame beat is provided but invalid.
- The linked beat must be resolved from package JSON, not a parallel hidden map.
- Existing callers that still provide `minigame_goal` directly must continue to
  work.

## Out of Scope
- Full multi-beat episode assembly endpoint
- Unity runtime consumption changes
- Automatic reverse-linking from minigame beats back to cutscenes

## Done Definition
- [x] Spec exists.
- [x] Storyboard service can derive objective text from a linked minigame beat.
- [x] Existing storyboard callers still work.
- [x] A package-level integration test proves the minigame-to-cutscene handoff.
