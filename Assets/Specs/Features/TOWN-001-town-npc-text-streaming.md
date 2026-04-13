# Feature Spec: Town NPC Text Streaming - TOWN-001

## Summary
`Assets/_Project/Scenes/Town.unity` already contains LLM-backed NPC interactions,
but the current path is still a prototype: `TownPlayerController` triggers
`NPCController`, `LLMConversationController` calls `OpenAIClient.ChatStream()`,
and `DialogueChoiceUI` tries to recover visible text by scanning partial JSON
from the raw stream.

This feature turns that prototype into a real streamed-character slice for the
town scene. NPC replies should appear as they are generated, the stream
lifecycle should be explicit and testable, control should always return
cleanly, and the Town scene should validate player-facing streaming UX without
being treated as the final production narrative platform.

## User Story
As a player exploring town, I want an NPC's line to appear while it is being
generated so the conversation feels alive and immediate instead of pausing and
then dumping a finished block of text.

## Product Goals
- Make town NPC conversations feel live and responsive.
- Replace brittle partial-JSON UI scraping with a dedicated streaming contract.
- Keep the Unity runtime client thin, deterministic, and compatible with the
  repo's "Core first, wrappers second" architecture.
- Validate streamed character dialogue in the Town slice without committing the
  project to a full Unity-side generative story architecture.

## Current Slice Anchors
- `Assets/_Project/Scenes/Town.unity`
- `Assets/_Project/Scripts/MonoBehaviours/TownPlayerController.cs`
- `Assets/_Project/Scripts/MonoBehaviours/Cinematics/NPCController.cs`
- `Assets/_Project/Scripts/MonoBehaviours/LLMConversationController.cs`
- `Assets/_Project/Scripts/MonoBehaviours/DialogueChoiceUI.cs`
- `Assets/_Project/Scripts/MonoBehaviours/OpenAIClient.cs`
- `Assets/_Project/Scripts/Core/NPCPersonaCatalog.cs`

## Acceptance Criteria
- [ ] Interacting with a town NPC opens the dialogue surface immediately and
      shows a waiting state before the first streamed text arrives.
- [ ] NPC reply text appears incrementally from transport-provided text deltas;
      the visible UI does not recover text by rescanning the full accumulated
      raw stream every chunk.
- [ ] The conversation runtime exposes explicit lifecycle states for `Waiting`,
      `Streaming`, `Completed`, `Cancelled`, and `Failed`.
- [ ] Only one active NPC stream can exist at a time. Extra interaction input
      during an active turn is ignored or soft-blocked with stable UI.
- [ ] When a turn finishes, reply options or fallback continuation controls are
      revealed only after the final turn payload has been validated.
- [ ] Malformed payloads, missing credentials, network failures, timeouts, or
      model errors show a readable fallback message and always restore player
      control and cursor state.
- [ ] Conversation history is bounded, remains local to the active NPC session,
      and never bleeds one NPC's context into another's.
- [ ] The OpenAI credential source is not a serialized scene value; runtime
      configuration is supplied from a non-committed source or local override.
- [ ] `TownInteractionAutoplay` can still trigger a streamed conversation and
      correctly wait for the turn to finish.
- [ ] The feature is text-only for this slice: no voice playback, lip sync, or
      gesture generation is required for completion.
- [ ] A repo-local golden-set eval exists for Town NPC generations and fails
      when outputs leak raw JSON, option arrays, code fences, or other
      non-spoken payload formatting.

## VR Interaction Model
- **Primary input**: the current validation slice uses the existing `E`
  interaction on the nearest NPC. The streaming system must sit behind that
  action so a future VR-specific interact binding can reuse it unchanged.
- **Feedback**: immediate panel open, speaker name visible, loading indicator,
  incremental text reveal, options shown only after the NPC line completes.
- **Comfort**: no forced camera movement, no camera snaps, no blinking panel
  state; when the conversation ends or fails, control returns cleanly.

## Edge Cases
- Player presses `E` repeatedly while a turn is already streaming.
- Player exits or cancels the conversation mid-stream.
- The stream starts but stalls before completion.
- The model returns text but no valid reply options.
- The NPC is disabled, destroyed, or leaves the scene while the request is
  still in flight.
- The dialogue canvas exists but one or more text fields are missing.
- Town autoplay triggers a conversation when credentials are absent.

## Performance Impact
- Text-only streaming should have negligible GPU cost and minimal Quest impact.
- The main performance risk is CPU allocation churn from repeated string
  rebuilding or whole-buffer reparsing on each chunk; the implementation should
  use incremental parsing and append-only UI updates.
- One in-flight request at a time and bounded local turn history keep memory,
  payload size, and debug complexity predictable.
- The feature should reuse the existing TMP/canvas stack in `Town.unity`.

## Dependencies
- **Existing systems**:
  - `TownPlayerController`
  - `NPCController`
  - `LLMConversationController`
  - `DialogueChoiceUI`
  - `NPCPersonaCatalog`
  - `TownInteractionAutoplay`
  - `com.unity.modules.unitywebrequest`
  - `com.unity.nuget.newtonsoft-json`
- **New systems**:
  - A dedicated streaming transport contract
  - A Responses API client wrapper for streamed text
  - A runtime credential provider
  - A typed turn-result contract for final option validation

## Out of Scope
- The reviewed backend `StoryPackage` platform from
  `docs/plans/2026-04-13-generative-story-orchestrator-prd.md`
- Voice playback, lip sync, facial animation, or gesture synthesis
- Long-term memory across sessions, saves, or scenes
- Moderation dashboards, telemetry pipelines, or publish workflows
- NPC locomotion, schedules, or animation upgrades
- Replacing the Town scene with a headset-first UX in this story

---

## Technical Plan

### Research Reference
- **Key pattern adopted**: consume explicit text-delta events from OpenAI's
  Responses API and route them through a dedicated stream transport instead of
  scraping partial JSON in the UI.
- **Why this approach**: OpenAI recommends the Responses API for new
  text-generation work, and Unity's `DownloadHandlerScript` is the supported
  hook for incremental network processing as bytes arrive.
- **Recommended packages**: no new package required; reuse
  `com.unity.modules.unitywebrequest` and `com.unity.nuget.newtonsoft-json`
  already installed in the repo.
- **Sources consulted**:
  - https://developers.openai.com/api/docs/guides/streaming-responses
  - https://developers.openai.com/api/docs/guides/text#choosing-models-and-apis
  - https://developers.openai.com/api/docs/guides/structured-outputs#structured-outputs-vs-json-mode
  - https://developers.openai.com/api/docs/guides/conversation-state#passing-context-from-the-previous-response
  - https://docs.unity3d.com/es/2017.4/Manual/UnityWebRequest-CreatingDownloadHandlers.html
- **Deviations from community pattern**: the scene slice should keep
  conversation state locally bounded instead of silently depending on
  server-stored threaded history. This keeps the Unity runtime prototype thin
  and avoids coupling Town scene behavior to long-lived backend state.

### Memory Reference
- **Relevant ADRs**:
  - `Pure C# Core + thin MonoBehaviour wrappers`
  - `Assembly definitions enforce boundaries`
  - `New Input System only`
- **Patterns to follow**:
  - Every public Core method gets EditMode coverage.
  - Reuse the existing `NPCController` interaction seam instead of inventing a
    parallel town-only trigger path.
  - Keep scene UI reuse high; avoid adding another floating card or heavy UI
    stack just for this slice.
- **Antipatterns to avoid**:
  - Do not put Unity types or transport code into `Core/`.
  - Do not rely on ad hoc string manipulation as the sole parser for final
    turn payloads.
  - Do not serialize secrets into scene assets or committed components.
- **Lessons from past work**:
  - Scene-level work that touches `.unity` files must include a scene-integrity
    sweep because Unity merges can leave stale or missing-script state behind.
  - Completion summaries must separate verified behavior from assumptions.
- **Asset path note**: no third-party asset path work is required for this
  feature unless the Town scene UI hierarchy changes.

### Completion Learning Reference
- **Completion Claims (2026-04-11)**: the final handoff for this story must say
  exactly what stream behavior was directly verified and what remained assumed.
- **Unity Scene Merges Need Missing-Script Sweeps (2026-04-13)**: if
  `Town.unity` is edited, verification must include a missing-script sweep and
  an interaction smoke test, not just code review.

### Architecture
- **Core/ classes**:
  - `ConversationTurnRequest`
  - `ConversationTurnResult`
  - `ConversationReplyOption`
  - `ConversationSessionState`
  - `ConversationStreamEvent`
  - `ConversationFailureReason`
- **Interfaces/**:
  - `IConversationStreamTransport`
  - `IConversationCredentialsProvider`
- **MonoBehaviours/**:
  - `OpenAIResponsesClient` (new Unity transport wrapper)
  - `LLMConversationController` (refactor onto explicit turn-state handling)
  - `DialogueChoiceUI` (consume typed stream events, not raw JSON)
  - `TownPlayerController` (minor gating only)
  - `TownInteractionAutoplay` (wait on new completion signal)
- **ScriptableObjects/**:
  - none required for V1; continue using `NPCPersonaCatalog` for persona text
    until a later profile asset spec exists

### Data Flow
```text
Player / autoplay interaction
  -> NPCController.TriggerInteraction()
  -> LLMConversationController.BuildTurnRequest()
  -> IConversationStreamTransport.BeginTurn()
  -> OpenAIResponsesClient / DownloadHandlerScript.ReceiveData()
  -> SSE event parser
  -> ConversationStreamEvent(TextDelta)
  -> DialogueChoiceUI appends visible text
  -> ConversationStreamEvent(Completed / Failed / Cancelled)
  -> LLMConversationController finalizes state
  -> DialogueChoiceUI reveals options or fallback controls
```

### Turn Contract
- **Streamed concern**: visible NPC utterance text
- **Finalized concern**: validated turn result and reply options
- **Design rule**: the live text path must not depend on the final structured
  payload being complete or parseable

Two implementation shapes are acceptable during TDD:
1. **Preferred**: stream plain utterance text, then validate structured
   follow-up data after completion.
2. **Fallback**: if one-call structured output is retained, the transport layer
   owns incremental extraction and validation. The UI still consumes only typed
   text-delta events.

### Dependencies
- **Depends on**:
  - `Town.unity`
  - existing town NPC interaction flow
  - existing persona catalog
- **Depended on by**:
  - future VR-specific interact bindings
  - future voice/animation conversation polish
  - any later move from the Town prototype to a reviewed backend dialogue
    platform

### Testing Strategy
- **EditMode**:
  - SSE line parser and event classifier
  - turn-state reducer (`Waiting -> Streaming -> Completed/Failed/Cancelled`)
  - bounded per-NPC history behavior
  - final turn payload validation and fallback-option generation
  - credential-source behavior when config is absent
  - legacy JSON compatibility parsing for `response` plus `options`
- **PlayMode**:
  - Town NPC interaction starts and ends cleanly
  - UI streams text without raw-buffer scraping behavior
  - cursor/control lock state restores correctly after success, failure, and
    cancel
  - `TownInteractionAutoplay` still completes after the streamed turn
  - only the nearest in-range NPC is triggered
- **Manual scene test**:
  - talk to each town NPC
  - verify first-token responsiveness
  - cancel a stream mid-turn
  - unplug credentials / force a network error
  - reopen a conversation after a failure
- **Golden-set eval**:
  - run a repo-local Town generation dataset against captured or fresh outputs
  - fail if any case leaks raw JSON, option arrays, code fences, or misses its
    content guardrails

### Performance Considerations
- Use a preallocated `DownloadHandlerScript` buffer where practical.
- Avoid `Substring` plus full-buffer `ToString()` rebuilding on every chunk.
- Keep parsing event-oriented and lightweight because Unity callback handling
  occurs on the main thread.
- Cap local history by turn count and/or characters so payload size remains
  predictable.
- Keep the feature text-only for this slice; no audio streaming or heavy
  animation hooks.

### Risks
- **Risk**: migrating the Town scene from Chat Completions to Responses may
  break the current prototype if transport and UI are refactored together.
  **Mitigation**: put the streaming transport behind an interface and keep a
  stub/fake transport for tests.
- **Risk**: final reply options may not validate cleanly if the model is still
  asked for JSON-like output.
  **Mitigation**: validate the final turn separately from the visible text path
  and provide deterministic fallback options.
- **Risk**: scene state can get stuck if failure/cancel paths do not restore
  cursor and control state.
  **Mitigation**: treat control restoration as explicit state-machine coverage
  in PlayMode tests.
- **Risk**: Town scene YAML edits can introduce stale component wiring.
  **Mitigation**: include a scene-integrity sweep whenever `.unity` wiring is
  part of the implementation.

---

## Task Breakdown

### Task 1: Define the stream contract
- **Type**: Core classes / interfaces
- **Files**:
  - `Assets/_Project/Scripts/Core/Conversation/ConversationTurnRequest.cs`
  - `Assets/_Project/Scripts/Core/Conversation/ConversationTurnResult.cs`
  - `Assets/_Project/Scripts/Core/Conversation/ConversationReplyOption.cs`
  - `Assets/_Project/Scripts/Interfaces/IConversationStreamTransport.cs`
- **Tests**: EditMode tests for state transitions and bounded history inputs
- **Depends on**: nothing
- **Acceptance**: the Town conversation flow has typed request/result objects
  and a transport seam that contains no Unity API details

### Task 2: Build the Unity Responses transport
- **Type**: MonoBehaviour / infrastructure
- **Files**:
  - `Assets/_Project/Scripts/MonoBehaviours/OpenAIResponsesClient.cs`
  - `Assets/_Project/Scripts/MonoBehaviours/Streaming/SseDownloadHandler.cs`
  - `Assets/_Project/Scripts/MonoBehaviours/Streaming/OpenAICredentialsProvider.cs`
- **Tests**: EditMode parser tests plus PlayMode smoke around request start and
  completion
- **Depends on**: Task 1
- **Acceptance**: Unity can emit typed text-delta/completion/error events from
  a streamed OpenAI response without exposing raw partial JSON to the UI

### Task 3: Refactor conversation orchestration
- **Type**: MonoBehaviour
- **Files**:
  - `Assets/_Project/Scripts/MonoBehaviours/LLMConversationController.cs`
- **Tests**: EditMode tests for session isolation; PlayMode tests for turn
  start/end/cancel behavior
- **Depends on**: Tasks 1-2
- **Acceptance**: the controller owns one active turn, bounded NPC-local
  history, explicit failure/cancel paths, and stable finalization

### Task 4: Refactor the town dialogue UI for streaming
- **Type**: MonoBehaviour / UI
- **Files**:
  - `Assets/_Project/Scripts/MonoBehaviours/DialogueChoiceUI.cs`
- **Tests**: PlayMode tests for visible text updates, loading state, option
  reveal timing, and fallback messaging
- **Depends on**: Tasks 2-3
- **Acceptance**: the UI consumes typed stream events and no longer extracts
  visible dialogue by scanning the accumulated raw stream buffer

### Task 5: Wire Town scene interaction and autoplay
- **Type**: Scene integration / MonoBehaviour
- **Files**:
  - `Assets/_Project/Scripts/MonoBehaviours/TownPlayerController.cs`
  - `Assets/_Project/Scripts/MonoBehaviours/Autoplay/TownInteractionAutoplay.cs`
  - `Assets/_Project/Scenes/Town.unity`
- **Tests**: PlayMode smoke for nearest-NPC interaction, cursor restoration,
  and autoplay completion
- **Depends on**: Tasks 3-4
- **Acceptance**: the Town scene remains playable and autoplay-compatible while
  using the new streaming contract

### Task 6: Verify the slice
- **Type**: Test / verification
- **Files**:
  - `Assets/Tests/EditMode/TownConversationStreamTests.cs`
  - `Assets/Tests/PlayMode/TownConversationPlayTests.cs`
- **Tests**: full story coverage
- **Depends on**: Tasks 1-5
- **Acceptance**: parser, controller, UI, and Town scene smoke coverage exist
  for success, failure, cancel, and malformed-output cases

## Estimated TDD Cycles
- 6 main tasks x ~1 focused RED/GREEN/VERIFY/REFACTOR cycle each = 6 cycles
- Likely bottlenecks:
  - incremental SSE parsing in Unity
  - deciding the final option payload path without reintroducing partial JSON
    coupling
  - keeping Town scene control/cursor restoration correct across every exit path
