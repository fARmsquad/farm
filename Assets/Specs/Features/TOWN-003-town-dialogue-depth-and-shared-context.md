# Feature Spec: Town Dialogue Depth And Shared Context - TOWN-003

## Summary
The Town slice now streams NPC lines directly, but the actual conversation
quality is still shallow. Old Garrett repeats the same stories, the follow-up
buttons recycle the same canned prompts, and the scene does not yet treat the
town as a shared world with connected people, places, and history.

This feature adds a shared Town knowledge graph in `Core/`, richer per-NPC
context, anti-repetition guidance for the live LLM prompt, and conversation-
aware follow-up choices that react to the latest reply and the recent turn
history.

## User Story
As a player talking to town NPCs, I want each character to feel like they live
in the same believable place, remember what they have already said in the
current conversation, and offer follow-up choices that actually fit the line I
just heard.

## Product Goals
- Make Town NPCs feel grounded in one shared world instead of isolated prompt
  blobs.
- Stop re-offering the same recent question and stop repeating the same answer
  wording when a topic comes up again.
- Turn follow-up choices into reply-aware conversation leads rather than a
  rotating canned ladder.
- Keep all shared world data in pure C# so the logic remains EditMode-testable.

## Current Slice Anchors
- `Assets/_Project/Scenes/Town.unity`
- `Assets/_Project/Scripts/Core/NPCPersonaCatalog.cs`
- `Assets/_Project/Scripts/Core/TownDialogueOptionComposer.cs`
- `Assets/_Project/Scripts/Core/ChatMessage.cs`
- `Assets/_Project/Scripts/MonoBehaviours/LLMConversationController.cs`
- `Assets/Tests/EditMode/TownConversationFlowTests.cs`
- `Assets/Tests/EditMode/TutorialSceneConfigurationTests.cs`

## Acceptance Criteria
- [ ] Each Town NPC prompt includes shared town facts, cross-character
      relationships, and role-specific personal history while still instructing
      the model to return spoken dialogue only.
- [ ] The opening user seed for a conversation is a readable NPC-specific start
      instruction rather than the opaque `[START_CONVERSATION]` token.
- [ ] If the player repeats a question or revisits the same topic, the prompt
      explicitly tells the model to answer from a fresh angle and add at least
      one new concrete detail instead of repeating the previous anecdote.
- [ ] Follow-up options are built from the latest reply, the active NPC's known
      conversation threads, and recent turn history.
- [ ] Recently asked player questions are not immediately re-offered as follow-
      up buttons.
- [ ] The latest reply can surface connected people, places, and history
      threads such as Miss Edna, the schoolhouse, the market, the old mill, or
      cross-NPC relationships.
- [ ] `Goodbye` still unlocks only after the configured later-turn threshold.
- [ ] All new shared-context logic lives in `Core/` with zero UnityEngine
      references.
- [ ] EditMode coverage proves the richer prompt contains shared-world guidance
      and the option composer avoids recent-question repetition while surfacing
      response-aware leads.

## Edge Cases
- The player asks the exact same question two turns in a row.
- The NPC mentions another character, but the existing canned prompts would
  normally ignore that lead.
- The latest reply contains no obvious keyword match from the previous prompt
  buckets.
- The model returns no follow-up options of its own.
- An unknown NPC name falls back to the generic persona path.

## Out Of Scope
- Persistent cross-session memory or save-backed relationship state
- A runtime vector database, embeddings store, or external knowledge service
- Replacing the current four-choice UI layout
- Backend-side narrative orchestration or moderation changes
- Quest-device voice polish beyond the already separate TOWN-002 scope

---

## Technical Plan

### Research Reference
- Reuse the existing Town streaming research from `TOWN-001`: keep the text
  stream thin and local while strengthening the prompt and the post-turn option
  composition.
- Adopt the same Responses API local-history approach, but add stronger
  prompt-side anti-repetition and grounded world context instead of relying on a
  generic NPC blurb.
- No new packages are required; this is prompt/data/composition work on top of
  the current Town slice.

### Memory Reference
- **Relevant ADRs**:
  - `Pure C# Core + thin MonoBehaviour wrappers`
  - `Assembly definitions enforce boundaries`
- **Relevant completion learnings**:
  - `Town dialogue tests must cover option cadence, not only streamed text`
  - `Town dialogue choices must be composed from the latest reply, not only the turn index`
  - `Town streaming needs final-payload guardrails`
- **Pattern to follow**:
  - Keep shared world knowledge in `Core/`, then let `LLMConversationController`
    and `NPCPersonaCatalog` consume it through thin MonoBehaviour wrappers.

### Architecture
- **Core/**
  - `TownKnowledgeGraph`
  - `TownNpcKnowledgeProfile`
  - `TownDialogueOptionComposer` history-aware overload
- **MonoBehaviours/**
  - `LLMConversationController` updated to seed the opening turn with an
    NPC-specific conversation opener and pass recent history into the option
    composer

### Runtime Flow
```text
Player interacts with NPC
  -> LLMConversationController starts fresh local history
  -> NPCPersonaCatalog builds a richer system prompt from TownKnowledgeGraph
  -> controller seeds an NPC-specific opening prompt
  -> OpenAI streams spoken reply text
  -> reply text completes
  -> TownDialogueOptionComposer inspects latest reply + recent history
  -> four follow-up options are built without re-offering the most recent asks
```

### Testing Strategy
- **EditMode**
  - prompt contains shared-world facts plus anti-repetition guidance
  - option composer avoids recent exact-question repeats
  - option composer surfaces reply-aware leads tied to entities/threads from the
    latest line
  - existing `Goodbye` cadence stays intact

### Risks
- A richer prompt can still drift if the reply budget is too tight; anti-
  repetition guidance needs to stay compact enough for `gpt-4o-mini`.
- Overly aggressive de-duplication could remove useful options and leave the
  composer with weak fallbacks, so the option builder still needs a stable
  four-choice floor.
