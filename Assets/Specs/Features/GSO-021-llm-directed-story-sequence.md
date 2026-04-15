# Feature Spec: LLM-Directed Story Sequence Generation — GSO-021

## Summary
The current generated playthrough can produce live images and narration audio,
but the narrative layer is still mostly a fixed proof template. This slice
replaces the rule-written bridge text with a real LLM-directed story turn:
OpenAI chooses the next bounded minigame beat and writes the cutscene script,
Gemini renders the resulting storyboard images, ElevenLabs voices the
narration, and Unity continues to play the resulting sequence through the
existing generated title-screen slice.

## User Story
As a developer, I want `Generate Unique Playthrough` to produce a genuinely new
story beat instead of replaying a preset bridge, so I can test the real
generative experience: a fresh narrative setup, a linked minigame objective,
generated storyboard shots, generated narration audio, and a believable next
step in the endless farm loop.

## Research Reference
- See `.ai/memory/research-notes.md#research-llm-directed-sequence-narrative-generation`.
- Apply existing project-memory guidance for fail-closed generated slices,
  title-screen loading visibility, canonical scene resolution, and continuity
  reference usage.
- Cross-check `.ai/memory/completion-learnings.md` entries about generated
  slices falling back into stale content or being handed off without one healthy
  live launch path.

## Acceptance Criteria

### Turn Direction
- [x] Story-sequence session planning can ask an LLM turn director to choose
      the next bounded minigame generator from the existing catalog.
- [x] The LLM turn director can choose a character from the session character
      pool instead of relying on a fixed round-robin only.
- [x] The LLM turn director produces a generated cutscene display name and
      story brief that are persisted into the turn request payload.
- [x] If the LLM turn director returns an invalid generator, invalid character,
      malformed payload, or transport failure, the session falls back to the
      existing bounded rule-based turn selection instead of failing open.

### Storyboard Script Generation
- [x] Generated cutscene storyboard shots can be authored by an OpenAI-backed
      structured-output planner instead of only by the hardcoded template shot
      builder.
- [x] The generated storyboard output includes subtitle text, narration text,
      image prompts, and durations for each shot.
- [x] The generated storyboard prompt includes the active story brief, the
      linked minigame goal, and existing continuity/reference-image constraints.
- [x] If the OpenAI storyboard planner is unavailable or returns invalid
      structured output, the service falls back to the existing template
      storyboard planner.

### Runtime Experience
- [ ] The standing generated title-screen slice can still prepare and play a
      generated run through the existing `Generate Unique Playthrough` then
      `Play Unique Playthrough` flow.
- [ ] When the LLM storyboard planner is available, the first generated slice
      no longer shows only the fixed `Nice work...` bridge text from the proof
      template.
- [x] The generated minigame remains bounded to the existing generator catalog,
      but the chosen beat is narratively framed by the LLM-generated bridge.

### Testing
- [x] A backend unit test proves an LLM-selected valid generator and character
      are used for the next sequence turn.
- [x] A backend unit test proves invalid LLM turn-direction output falls back
      to the bounded rule-based selection.
- [x] A backend unit test proves the OpenAI storyboard planner can emit
      generated shots that differ from the fixed template lines.
- [x] A backend unit test proves storyboard-planner failure falls back to the
      template planner without breaking package assembly.

## Non-Negotiable Rules
- Do not allow the model to invent arbitrary minigame IDs or Unity scene names.
- Do not remove the existing Gemini/ElevenLabs fallback behavior while adding
  OpenAI-authored narrative generation.
- Do not let invalid or unavailable LLM output silently route the title-screen
  slice into stale authored fallback content.
- Do not bypass the existing bounded minigame parameter validation layer.

## Out Of Scope
- New minigame generator definitions beyond the current three bounded
  generators
- Full background pre-generation of later turns while the player is still
  inside the current minigame
- Runtime Unity editing or scene assembly of new mechanics
- Lip-synced character animation or cinematic camera systems

## Done Definition
- [x] Spec exists.
- [x] Sequence sessions can use a real LLM turn director with bounded fallback.
- [x] Generated storyboard shots can use a real LLM planner with bounded
      fallback.
- [x] Focused backend tests cover valid generation and fallback behavior.
- [ ] The standing generated title-screen slice remains the live test surface
      for the new narrative path.
