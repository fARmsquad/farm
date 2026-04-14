# Feature Spec: Town Shared Conversation Memory - TOWN-004

## Summary
Town NPCs now have deeper local context and reply-aware options, but the slice
still treats each conversation as mostly self-contained. The player cannot yet
carry learned information from one resident to another in a structured way, and
NPCs do not maintain a session memory window that grows as the player talks to
them.

This feature adds session-scoped Town conversation memory: a basic player
knowledge base, per-NPC remembered context, and relay-aware option generation so
the player can bring what they learned from one character into another
conversation.

## User Story
As a player learning about the town, I want to collect information from
different residents and bring it back into later conversations so the town
starts feeling connected instead of like isolated chatbot turns.

## Product Goals
- Give the player a lightweight session knowledge base that starts simple and
  grows from conversation.
- Give each NPC a context window that reflects what they have already told the
  player and what the player has relayed back to them.
- Surface relay prompts through the existing four-choice UI so cross-NPC
  information flow works without requiring a custom text-entry widget.
- Keep the whole memory system in pure C# and bounded enough for fast Town
  responses.

## Current Slice Anchors
- `Assets/_Project/Scripts/Core/TownKnowledgeGraph.cs`
- `Assets/_Project/Scripts/Core/TownDialogueOptionComposer.cs`
- `Assets/_Project/Scripts/MonoBehaviours/LLMConversationController.cs`
- `Assets/_Project/Scripts/MonoBehaviours/DialogueChoiceUI.cs`
- `Assets/Tests/EditMode/TownDialogueDepthTests.cs`

## Acceptance Criteria
- [ ] The player starts with a small newcomer baseline context rather than an
      empty world model.
- [ ] NPC replies can unlock reusable Town knowledge entries in a session memory
      store.
- [ ] Each NPC has a session context window that includes what they already told
      the player and what the player has relayed to them.
- [ ] The live prompt includes the dynamic memory window for the active NPC in
      addition to the static persona/world prompt.
- [ ] Follow-up options can include relay prompts derived from facts the player
      already learned from other characters.
- [ ] Once the player relays a known fact to an NPC, that fact is tracked as
      shared with that NPC and stops reappearing as an immediate relay option.
- [ ] The memory system is session-scoped and local to the Town runtime; no save
      persistence is required for this story.
- [ ] Core/EditMode tests cover fact discovery, relay prompt generation, and
      NPC-specific memory window updates.

## Edge Cases
- The player learns the same fact twice from different residents.
- The player brings a known fact back to the same NPC who originally said it.
- A conversation has no recognizable knowledge facts.
- The player has known facts available for relay before opening a new NPC
  conversation.
- The memory store must stay bounded and not append the whole session transcript
  into every prompt.

## Out Of Scope
- Persistent save-backed memory across play sessions
- Free-text chat input UI
- Backend narrative memory services or embeddings
- Quest-device production tuning beyond the Town scene prototype

---

## Technical Plan

### Research / Memory Reference
- Extend the TOWN-003 shared-world grounding work rather than replacing it.
- Reuse the existing local-history Responses API pattern; append a compact
  dynamic system context for the active NPC before each turn request.
- Keep the prompt memory bounded and explicit to preserve the fast `gpt-4o-mini`
  slice budget.

### Architecture
- **Core/**
  - `TownKnowledgeFact`
  - `TownConversationContextWindow`
  - `TownConversationMemoryStore`
- **Existing Core updates**
  - `TownKnowledgeGraph` exposes matchable knowledge facts and relay templates.
  - `TownDialogueOptionComposer` can prioritize relay prompts supplied by the
    memory store.
- **MonoBehaviours/**
  - `LLMConversationController` owns one session memory store for the Town slice
    and appends its active-NPC context into each request.

### Runtime Flow
```text
NPC reply completes
  -> memory store extracts known town facts from the line
  -> player knowledge journal gains new facts

New or continued conversation with NPC X
  -> memory store builds NPC X context window
  -> controller appends that context as an extra system instruction
  -> option composer receives relay prompts derived from player-known facts
  -> player can choose a relay prompt
  -> memory store marks that fact as shared with NPC X
```

### Testing Strategy
- **EditMode**
  - newcomer base context exists before any fact unlocks
  - NPC responses unlock player-known facts
  - known facts produce relay prompts for other NPCs
  - relayed facts are recorded on the target NPC and stop repeating as relay
    options

### Risks
- Overfeeding prompt memory will slow Town responses, so the context window must
  stay compact and recent.
- Keyword-based fact extraction can over-match if the facts are too broad, so
  the fact list should stay tightly scoped to distinctive town concepts.
