# Feature Spec: Unity Runtime Sequence Session Bridge — GSO-011b

## Summary
GSO-011a added persistent backend sequence sessions and automatic next-turn
planning, but Unity still consumes a static `Resources` story package by
default. That means the game can generate turns on the backend without truly
playing them as a live endless sequence.

This slice bridges the runtime:
- Unity can request a new generated turn from the local story orchestrator
- Unity can inject the returned story package into an in-memory runtime
  override instead of relying only on the authored `Resources` asset
- the title-screen `Generative Story Slice` can bootstrap from a live backend
  session
- when a generated beat chain reaches a terminal generated minigame, Unity can
  request the next turn and continue into the next generated cutscene
- runtime beat lookup prefers exact generated scene names over older
  tutorial-alias fallback matches

## User Story
As a developer, I want the Unity runtime to bootstrap and continue a live
story-sequence session from the backend, so the `Generative Story Slice`
actually plays generated cutscenes and edited minigames instead of only the
static sample package.

## Acceptance Criteria

### Runtime Package Override
- [ ] `StoryPackageRuntimeCatalog` can accept a runtime override package.
- [ ] The runtime override can be cleared.
- [ ] When a runtime override is present, runtime lookups use it before the
      authored `Resources` package.
- [ ] Runtime package import still validates through the existing story-package
      contract.

### Beat Resolution
- [ ] `StoryPackageNavigator.TryGetBeatBySceneName` prefers an exact
      `SceneName` match before the normalized tutorial-alias fallback.
- [ ] A generated `PostChickenCutscene` beat can win over the older authored
      `Tutorial_PostChickenCutscene` beat for runtime lookup.

### Unity Runtime Client
- [ ] A Unity-side story-sequence client can:
      - create a backend sequence session
      - request the first generated turn
      - request the next generated turn for an existing session
- [ ] The runtime client supports the same local backend environment override
      used elsewhere in the project.
- [ ] Backend failure falls back cleanly instead of crashing the scene flow.

### Runtime Session Controller
- [ ] A persistent Unity runtime controller can own the active `session_id`.
- [ ] Starting the title-screen `Generative Story Slice` can bootstrap a live
      generated turn and load its entry cutscene.
- [ ] When a generated terminal beat is completed, the runtime controller can
      request the next generated turn and load its entry cutscene.
- [ ] If continuation fails, the tutorial flow shows completion instead of
      hanging.

### Integration Proof
- [ ] One EditMode test proves exact scene-name beat lookup wins over the
      tutorial alias fallback.
- [ ] One EditMode test proves the runtime package override is used when set.
- [ ] One EditMode test proves the runtime session controller can apply a fake
      generated turn payload and request the generated entry scene.
- [ ] One EditMode test proves the runtime session controller can advance an
      active session into a later generated turn.

## Non-Negotiable Rules
- Do not replace the existing authored `Resources` story package path; use it
  as the fallback baseline.
- Do not require a Unity asset database refresh to consume a generated runtime
  package.
- Do not route the title-screen generated slice through the authored intro
  Timeline when a live generated sequence is available.
- Do not silently reuse an older alias-matched cutscene beat when a newer
  exact-match generated beat exists.

## Out Of Scope
- Multiplayer/shared sequence sessions
- Persisting Unity-side sequence session state across full app restarts
- Background prefetching of future turns
- Runtime creation of brand-new scene assets

## Done Definition
- [ ] Spec exists.
- [ ] Unity can bootstrap a live generated story-sequence session from the
      title screen.
- [ ] Unity can continue a generated terminal beat into the next generated
      turn.
- [ ] Runtime package lookup prefers generated exact scene matches.
- [ ] Focused EditMode tests cover the runtime bridge behavior.
