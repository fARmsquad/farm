# Feature Spec: Dialogue System — INT-003

## Summary
A dialogue system that displays speaker names and typewriter-animated text in a bottom-screen dialogue box, driven by DialogueData ScriptableObjects. Players advance dialogue with Space or E, or lines auto-advance after a configurable duration.

## User Story
As a player, I want to read character dialogue during cutscenes so that I understand the story and my objectives through NPC conversations.

## Acceptance Criteria
- [ ] DialogueLine struct contains: string speakerName, string text, float duration, bool autoAdvance, Color speakerColor
- [ ] DialogueData ScriptableObject holds an ordered array of DialogueLine
- [ ] DialogueManager MonoBehaviour drives a bottom-screen UI Canvas with speaker name (TMPro, bold) and dialogue text (TMPro)
- [ ] Text displays with a typewriter effect at approximately 30 characters per second
- [ ] Player can advance to the next line by pressing Space or E (New Input System: Keyboard.current.spaceKey.wasPressedThisFrame or Keyboard.current.eKey.wasPressedThisFrame)
- [ ] Pressing advance while typewriter is mid-line completes the current line instantly; pressing again advances to next line
- [ ] Auto-advance mode: when DialogueLine.autoAdvance is true, automatically moves to next line after duration expires
- [ ] Show() makes the dialogue box visible; Hide() makes it invisible
- [ ] StartDialogue(DialogueData) begins playback from line 0
- [ ] OnDialogueComplete UnityEvent fires when all lines have been displayed and dismissed
- [ ] Speaker name color matches DialogueLine.speakerColor

## Edge Cases
- StartDialogue called while dialogue is already playing resets to the new DialogueData from line 0
- DialogueData with zero lines fires OnDialogueComplete immediately
- Very long text lines wrap within the dialogue box without overflow
- Pressing advance input when dialogue box is hidden does nothing
- Duration of 0 on an auto-advance line advances on the next frame after typewriter completes

## Performance Impact
- Single UI Canvas with two TMPro text elements — minimal draw calls
- Typewriter effect uses coroutine, no per-frame string allocation (uses TMPro maxVisibleCharacters)
- Input polling only active while dialogue box is visible

## Dependencies
- **Existing:** TextMeshPro (already installed), Unity Input System package
- **New:** DialogueLine.cs (struct), DialogueData.cs (ScriptableObject), DialogueManager.cs (MonoBehaviour), Dialogue UI Canvas prefab

## Out of Scope
- Voice acting or TTS playback (DeepVoice hook deferred)
- Character portraits or avatars
- Branching dialogue or player choices
- Localization
- Dialogue history/log

---

## Technical Plan

### Architecture
```
Assets/_Project/Scripts/Core/Cinematics/DialogueLine.cs          (struct, FarmSimVR.Core)
Assets/_Project/Scripts/Core/Cinematics/DialogueData.cs          (ScriptableObject — needs UnityEngine, so MonoBehaviours assembly)
Assets/_Project/Scripts/MonoBehaviours/Cinematics/DialogueData.cs (ScriptableObject, FarmSimVR.MonoBehaviours)
Assets/_Project/Scripts/MonoBehaviours/Cinematics/DialogueManager.cs
Assets/_Project/Prefabs/UI/DialogueCanvas.prefab
```

DialogueLine is a serializable struct. DialogueData is a ScriptableObject containing DialogueLine[]. DialogueManager holds references to TMPro text components and drives the typewriter coroutine.

Note: Since DialogueLine uses UnityEngine.Color, both DialogueLine and DialogueData live in the MonoBehaviours assembly.

### Build Approach
1. Define DialogueLine as a [System.Serializable] struct with speakerName, text, duration, autoAdvance, speakerColor fields
2. Create DialogueData ScriptableObject with DialogueLine[] lines array and [CreateAssetMenu] attribute
3. Build DialogueCanvas prefab: bottom-anchored panel (Image background, semi-transparent black), speaker name TMP_Text (bold, top-left of panel), dialogue text TMP_Text (below speaker name, wrapping enabled)
4. Implement DialogueManager with Show/Hide (SetActive on canvas), StartDialogue (set data, reset index, show, begin first line)
5. Implement typewriter effect: set TMP_Text.text to full line, set maxVisibleCharacters to 0, increment at ~30 chars/sec via coroutine
6. Implement input advance: poll Keyboard.current.spaceKey.wasPressedThisFrame and eKey.wasPressedThisFrame in Update; if typewriter is running, complete it; if line is complete, advance to next
7. Implement auto-advance: after typewriter completes, if autoAdvance is true, wait for duration then call AdvanceLine
8. Fire OnDialogueComplete when index exceeds lines array length

### Testing Strategy
- EditMode tests: DialogueData creation, verify lines array is serializable and accessible
- EditMode tests: DialogueManager state transitions (idle -> playing -> complete)
- PlayMode tests: StartDialogue with mock DialogueData, verify typewriter progresses maxVisibleCharacters
- PlayMode tests: verify OnDialogueComplete fires after last line
- PlayMode tests: verify advance input completes typewriter then advances line

---

## Task Breakdown

| # | Type | Action | Depends | Acceptance |
|---|------|--------|---------|------------|
| 1 | code | Define DialogueLine struct with all fields | — | Struct is serializable, all fields accessible in Inspector |
| 2 | code | Create DialogueData ScriptableObject with DialogueLine array | 1 | Asset can be created via Create menu, lines editable in Inspector |
| 3 | prefab | Build DialogueCanvas prefab with panel, speaker name TMP, dialogue TMP | — | Canvas renders at bottom of screen, text wraps correctly |
| 4 | code | Implement DialogueManager Show/Hide and StartDialogue | 2,3 | Dialogue box appears/disappears, first line begins on StartDialogue |
| 5 | code | Implement typewriter effect using maxVisibleCharacters | 4 | Text reveals at ~30 chars/sec, no per-frame string allocation |
| 6 | code | Implement Space/E input advance (New Input System polling) | 5 | First press completes line, second press advances to next line |
| 7 | code | Implement auto-advance timer for autoAdvance lines | 5 | Auto-advance lines proceed after duration without input |
| 8 | code | Fire OnDialogueComplete event after final line | 6,7 | Event fires once when all lines exhausted |
| 9 | test | Write EditMode + PlayMode tests for dialogue flow | 1-8 | All tests pass, coverage on StartDialogue through OnComplete |
