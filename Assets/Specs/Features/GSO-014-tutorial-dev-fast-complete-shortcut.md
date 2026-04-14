# Feature Spec: Tutorial Dev Fast-Complete Shortcut — GSO-014

## Summary
The generated story slice is now useful enough that scene-by-scene testing
happens constantly, but the current tutorial dev shortcuts only provide a hard
scene skip (`Shift+.`). That is useful for routing, but it is not the same as
finishing the current beat. For minigames and generated story slices, the
developer needs one shortcut that behaves like "complete this scene now" so the
sequence can be traversed quickly without playing every objective manually.

This slice adds a dedicated fast-complete shortcut for dev mode and routes it
through scene-specific completion hooks before falling back to the existing
scene-advance path.

## User Story
As a developer, I want a single dev shortcut that completes the current story
beat or minigame, so I can move through generated cutscenes and gameplay slices
quickly while testing sequence flow.

## Acceptance Criteria

### Shortcut Surface
- [ ] Tutorial dev shortcuts expose a dedicated fast-complete binding.
- [ ] The overlay text lists the fast-complete binding separately from the
      existing `Next`, `Back`, `Reload`, `Scene`, and `Reset` bindings.
- [ ] The existing `Shift+.` skip-to-next shortcut remains available.

### Runtime Behavior
- [ ] The fast-complete shortcut tries to complete the current scene through a
      scene-specific controller first.
- [ ] Supported tutorial scene controllers include:
      - generated / placeholder cutscenes
      - chicken chase
      - find-tools
      - farm tutorial / package-driven plant-rows
- [ ] If no scene-specific fast-complete handler is available, the flow falls
      back to the existing tutorial next-scene behavior.
- [ ] Terminal generated beats still show the completion banner instead of
      forcing an invalid scene load.

### Testing
- [ ] An EditMode test locks the new shortcut labels/summary text.
- [ ] An EditMode test proves package-driven find-tools can be fast-completed.
- [ ] An EditMode test proves package-driven farm progression can be
      fast-completed.
- [ ] An EditMode test proves the flow fallback still handles a terminal
      generated beat safely.

## Non-Negotiable Rules
- Do not remove or repurpose the existing tutorial navigation shortcuts.
- Do not hardcode scene-name-to-scene-load behavior outside the existing
  tutorial flow/catalog path.
- Do not make fast-complete depend on editor-only APIs.
- Do not block progression if a scene-specific controller is absent; fall back
  cleanly.

## Out Of Scope
- Persisting dev-cheat state between sessions
- New on-screen debug panels
- Simulating every intermediate animation or VFX event during fast-complete
- Non-tutorial world shortcuts

## Done Definition
- [ ] Spec exists.
- [ ] `Shift+Enter` fast-completes the active tutorial/generated beat.
- [ ] Existing `Shift+.` next-scene skip still works.
- [ ] Focused EditMode tests cover labels plus the key runtime fast-complete
      paths.
