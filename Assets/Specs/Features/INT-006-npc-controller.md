# Feature Spec: NPC Controller — INT-006

## Summary
A simple NPC controller for greybox dialogue encounters, represented as a colored capsule with a floating name tag. NPCs face the player when in range and trigger dialogue when the player presses E, serving as story delivery points during the intro cinematic.

## User Story
As a player, I want to approach NPCs and interact with them so that I can receive dialogue and story context during the intro sequence.

## Acceptance Criteria
- [ ] NPCController MonoBehaviour can be placed on any GameObject
- [ ] Greybox visual: Capsule primitive with configurable color (MeshRenderer material color)
- [ ] Floating name tag: TextMesh or TMPro text positioned above the capsule showing the NPC's name
- [ ] FacePlayer: when the player is within interactionRange (default 5m), NPC rotates on Y-axis to look at the player
- [ ] OnInteract: pressing E (New Input System: Keyboard.current.eKey.wasPressedThisFrame) while within interactionRange triggers DialogueManager.StartDialogue with this NPC's assigned DialogueData
- [ ] Interaction is only available when the NPC is active and the player is within range
- [ ] An interaction prompt UI hint ("Press E") appears when the player is within range and the NPC is not mid-dialogue
- [ ] NPCController can be activated/deactivated via SetActive(bool) — when deactivated, the capsule and name tag are hidden
- [ ] Configurable fields: string npcName, Color capsuleColor, DialogueData dialogueData, float interactionRange
- [ ] NPC cannot be interacted with while DialogueManager is already playing dialogue

## Edge Cases
- Player walks out of range mid-dialogue: dialogue continues to completion (no abort)
- E pressed when no NPC is in range does nothing
- Multiple NPCs in range: interact with the nearest one
- NPC activated while player is already in range: face-player and prompt appear immediately
- NPC with null DialogueData logs a warning and does not trigger dialogue
- FacePlayer only rotates Y-axis (no tilting forward/backward)

## Performance Impact
- Single capsule + TextMesh per NPC — negligible rendering cost
- Distance check in Update only when NPC is active; uses sqrMagnitude to avoid sqrt
- No navmesh or pathfinding calculations

## Dependencies
- **Existing:** None (player Transform found via tag or reference)
- **New:** NPCController.cs (MonoBehaviour)
- **System refs:** INT-003 (DialogueManager for triggering dialogue)

## Out of Scope
- NPC pathfinding or wandering
- Animation or skeletal meshes
- Voice acting or TTS
- Quest tracking or branching dialogue
- NPC inventory or trading
- Multiple interaction options (only dialogue)

---

## Technical Plan

### Architecture
```
Assets/_Project/Scripts/MonoBehaviours/Cinematics/NPCController.cs
Assets/_Project/Prefabs/NPCs/NPCGhost.prefab  (template capsule with NPCController)
```

NPCController is a MonoBehaviour on a GameObject hierarchy:
- Root: NPCController component
  - Child: Capsule (MeshRenderer, CapsuleCollider)
  - Child: NameTag (TextMeshPro or TextMesh, positioned at capsule top + offset)
  - Child: InteractionPrompt (Canvas with "Press E" text, worldspace or screen-space)

Player reference obtained via FindWithTag("Player") or serialized field.

### Build Approach
1. Create NPCController MonoBehaviour with serialized fields: npcName, capsuleColor, dialogueData (DialogueData reference), interactionRange (float, default 5)
2. In Start/OnEnable: set Capsule MeshRenderer.material.color to capsuleColor, set NameTag text to npcName
3. Implement FacePlayer in Update: if player distance <= interactionRange, rotate Y-axis toward player using Quaternion.LookRotation with flattened direction vector
4. Implement interaction prompt: show "Press E" UI element when player is in range and DialogueManager is not playing
5. Implement OnInteract in Update: check Keyboard.current.eKey.wasPressedThisFrame, player in range, DialogueManager not playing, dialogueData not null — then call DialogueManager.Instance.StartDialogue(dialogueData)
6. Implement Activate/Deactivate: public methods that call gameObject.SetActive(bool), hiding/showing all visuals
7. Create NPCGhost prefab template with Capsule, NameTag, and InteractionPrompt children

### Testing Strategy
- EditMode tests: NPCController field initialization, verify interactionRange default is 5
- PlayMode tests: place NPC and player within range, verify FacePlayer rotates NPC toward player
- PlayMode tests: simulate E key press within range, verify DialogueManager.StartDialogue called
- PlayMode tests: deactivate NPC, verify gameObject.activeSelf is false
- PlayMode tests: verify interaction prompt shows when in range, hides when out of range
- PlayMode tests: verify no interaction when DialogueManager is already playing

---

## Task Breakdown

| # | Type | Action | Depends | Acceptance |
|---|------|--------|---------|------------|
| 1 | code | Create NPCController MonoBehaviour with name, color, dialogueData, range fields | — | Component attaches, all fields editable in Inspector |
| 2 | code | Implement capsule color and name tag setup in Start/OnEnable | 1 | Capsule color matches configured value, name tag shows npcName |
| 3 | code | Implement FacePlayer Y-axis rotation when player is in range | 1 | NPC faces player when within interactionRange |
| 4 | code | Implement interaction prompt (Press E) visibility toggle based on range and dialogue state | 1 | Prompt shows in range, hides out of range or during dialogue |
| 5 | code | Implement E key interaction trigger wired to DialogueManager | 1,INT-003 | Pressing E in range starts dialogue with NPC's DialogueData |
| 6 | code | Implement Activate/Deactivate for sequencer integration | 1 | SetActive hides/shows NPC and all child visuals |
| 7 | prefab | Create NPCGhost prefab template (Capsule + NameTag + InteractionPrompt) | 1-6 | Prefab instantiable, all components wired |
| 8 | test | Write EditMode + PlayMode tests for NPC interaction flow | 1-6 | All tests pass, coverage on FacePlayer, interact, activate/deactivate |
