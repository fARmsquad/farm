# Feature Spec: Town Adaptive Dialogue HUD - TOWN-006

## Summary
`Town.unity` already supports reply buttons plus optional push-to-talk on `V`,
but the current HUD still behaves like a fixed mockup. The dialogue body,
loading text, and voice-input feedback compete for the same rectangle, while
choice buttons assume single-line labels and fixed heights.

This feature redesigns the Town conversation HUD so it adapts to live voice
input states, longer streamed prompts, and multi-line reply options without
overlapping or feeling brittle.

## User Story
As a player talking to Town NPCs, I want the dialogue HUD to clearly react when
I hold `V`, release it, wait for transcription, or fall back to button picks so
the interaction feels responsive instead of crowded or static.

## Product Goals
- Separate persistent control hints from transient voice / waiting status.
- Keep the voice-input path legible during idle, recording, transcribing, and
  warning states.
- Make generated Town reply buttons wrap and resize for longer labels.
- Let the dialogue panel and choice stack adapt to the content instead of
  relying on one fixed layout slot.

## Current Slice Anchors
- `Assets/_Project/Scenes/Town.unity`
- `Assets/_Project/Scripts/MonoBehaviours/DialogueChoiceUI.cs`
- `Assets/_Project/Scripts/MonoBehaviours/TownVoiceInputController.cs`
- `Assets/_Project/Scripts/MonoBehaviours/LLMConversationController.cs`
- `Assets/Tests/EditMode/DialogueChoiceUITests.cs`
- `Assets/Tests/EditMode/TownVoiceInputTests.cs`

## Acceptance Criteria
- [ ] While Town choices are visible, the HUD shows a dedicated control hint
      that can describe both button fallback and `V` voice input.
- [ ] Holding `V` changes the HUD into a recording state with clear "release to
      send" feedback without overwriting the dialogue body text.
- [ ] Transcribing and waiting states render in a dedicated status area instead
      of sharing the main dialogue text rectangle.
- [ ] Voice-input warnings such as unavailable microphone or empty capture leave
      the preset buttons usable and legible.
- [ ] Generated reply buttons support word wrapping and dynamic height so longer
      labels remain readable.
- [ ] The dialogue panel and choice stack reposition cleanly when longer player
      prompts, longer NPC replies, or active voice status need more space.
- [ ] Existing text-streaming and reply-selection behavior remains unchanged
      outside the HUD presentation redesign.
- [ ] EditMode coverage locks the adaptive button layout, voice-status hinting,
      and dynamic panel sizing behavior.

## Edge Cases
- The player has no microphone, so only button hints should remain.
- The player taps `V` too quickly and gets an empty capture warning.
- The NPC response or player prompt is long enough to grow the panel.
- A long generated reply option wraps to two or more lines.
- The conversation is waiting for the next streamed reply while choices are
  hidden.

## Out Of Scope
- New art assets, shaders, or animated waveform visuals
- Typed freeform text entry
- Quest-device-only HUD differences
- Replacing the current Town conversation interaction model

---

## Technical Plan

### Research Reference
- Use Unity UI auto-layout primitives for adaptive sizing:
  - `LayoutElement` to control min / preferred / flexible sizing per choice
  - `ContentSizeFitter` where the stack should grow to preferred content size
- Use TextMeshPro wrapping / overflow configuration instead of single-line
  button labels for generated text.
- Sources consulted:
  - https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-LayoutElement.html
  - https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-ContentSizeFitter.html
  - https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/TMPObjectUIText.html

### Memory Reference
- Follow `Pure C# Core + thin MonoBehaviour wrappers`.
- Keep the Town dialogue logic intact and limit this slice to HUD rendering and
  status plumbing.
- Reuse existing scene objects such as `HintLabel` where possible instead of
  rebuilding the Town canvas from scratch.

### Architecture
- **Updated runtime**
  - `DialogueChoiceUI` becomes the adaptive HUD presenter.
  - `TownVoiceInputController` exposes enough structured status for the HUD to
    distinguish idle, recording, transcribing, and warning states.
- **No new backend work**
  - This is a presentation redesign on top of the existing Town voice flow.

### Runtime Flow
```text
Town choices become visible
  -> DialogueChoiceUI renders wrapped choice buttons
  -> HUD shows a persistent control hint
  -> player holds V
  -> TownVoiceInputController enters recording state
  -> DialogueChoiceUI swaps hint + status presentation
  -> player releases V
  -> transcription begins
  -> HUD shows transcribing status while body text remains intact
  -> transcript is submitted through the normal prompt path
  -> waiting / streamed NPC reply updates continue through the same HUD
```

### Testing Strategy
- **EditMode**
  - long choice labels create wrapped, layout-driven buttons
  - idle voice state uses a dedicated hint without showing a warning badge
  - recording state shows a status badge plus a release hint
  - long dialogue content grows the panel and moves the choice stack upward
- **Manual**
  - open `Town.unity`
  - start a conversation
  - verify reply buttons stay readable with long labels
  - hold `V`, release, and confirm the HUD changes through recording and
    transcribing states without obscuring the dialogue text

### Risks
- Town currently mixes fixed anchors with runtime-generated buttons, so the
  adaptive layout must be conservative and avoid destabilizing the whole canvas.
- Full Unity test verification may still be limited by unrelated repo compiler
  debt, so this slice needs strong targeted regression coverage.
