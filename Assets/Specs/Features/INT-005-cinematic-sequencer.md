# Feature Spec: Cinematic Sequencer — INT-005

## Summary
The central orchestrator that executes an ordered list of cinematic steps to drive the entire intro sequence. Each step dispatches commands to screen effects, audio, dialogue, camera, NPC, and mission systems, with support for sequential execution, wait-for-completion, pause, resume, and skip.

## User Story
As a designer, I want to author a cinematic sequence as a ScriptableObject so that I can orchestrate camera moves, dialogue, audio, effects, and gameplay toggles in a defined order without writing code.

## Acceptance Criteria
- [ ] CinematicStep is a serializable struct with: CinematicStepType enum, string parameters, float duration, bool waitForCompletion
- [ ] CinematicStepType enum includes: CameraMove, Dialogue, Wait, PlaySFX, PlayMusic, Fade, Shake, Letterbox, ObjectivePopup, MissionStart, MissionComplete, EnablePlayerControl, DisablePlayerControl, ActivateNPC, DeactivateNPC, SetLighting
- [ ] CinematicSequence ScriptableObject holds an ordered array of CinematicStep
- [ ] CinematicSequencer MonoBehaviour executes steps sequentially from index 0
- [ ] Play() starts execution from step 0
- [ ] Pause() halts execution at the current step; Resume() continues from where it paused
- [ ] Skip() jumps to the end, applying final states of all remaining steps instantly
- [ ] When waitForCompletion is true, the sequencer waits for the current step to finish before starting the next
- [ ] When waitForCompletion is false, the sequencer immediately starts the next step (allowing concurrent effects)
- [ ] DisablePlayerControl disables PlayerMovement component; EnablePlayerControl re-enables it
- [ ] OnSequenceComplete UnityEvent fires when the last step completes
- [ ] Sequencer holds serialized references to: ScreenEffects, SimpleAudioManager, DialogueManager, CinematicCamera, MissionManager

## Edge Cases
- Play called while already playing restarts from step 0
- Pause called when not playing is a no-op
- Resume called when not paused is a no-op
- Skip called when not playing fires OnSequenceComplete immediately
- CinematicSequence with zero steps fires OnSequenceComplete immediately on Play
- Steps referencing null systems (e.g., no ScreenEffects assigned) log a warning and skip

## Performance Impact
- Coroutine-based step execution — no Update polling when idle
- Each step delegates to an existing system; sequencer itself has near-zero overhead
- ScriptableObject data is read-only at runtime, no GC allocation from sequence iteration

## Dependencies
- **Existing:** PlayerMovement.cs (for enable/disable control)
- **New:** CinematicStep.cs (struct), CinematicStepType.cs (enum), CinematicSequence.cs (ScriptableObject), CinematicSequencer.cs (MonoBehaviour)
- **System refs:** INT-001 (ScreenEffects), INT-002 (SimpleAudioManager), INT-003 (DialogueManager), INT-004 (CinematicCamera), INT-006 (NPCController), INT-007 (MissionManager)

## Out of Scope
- Visual node-based sequence editor
- Timeline integration (this replaces Timeline for greybox)
- Undo/redo during playback
- Runtime sequence modification
- Branching or conditional steps

---

## Technical Plan

### Architecture
```
Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicStepType.cs   (enum)
Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicStep.cs       (serializable struct)
Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicSequence.cs   (ScriptableObject)
Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicSequencer.cs  (MonoBehaviour)
```

CinematicSequencer is the brain. It holds [SerializeField] references to all subsystems and iterates through CinematicSequence.steps. A large switch statement in ExecuteStep maps CinematicStepType to the appropriate system call.

CinematicStep uses string fields for flexible parameterization:
- CameraMove: waypointIndex (int parsed from string)
- Dialogue: reference name of DialogueData asset (resolved via a serialized dictionary or direct reference array on Sequencer)
- Wait: uses duration field directly
- PlaySFX/PlayMusic: audio key string for AudioLibrary lookup
- Fade/Shake/Letterbox: uses duration + a float parameter field
- ObjectivePopup/MissionStart/MissionComplete: text string parameter
- EnablePlayerControl/DisablePlayerControl: no parameters
- ActivateNPC/DeactivateNPC: NPC name string (resolved from serialized NPC reference list)
- SetLighting: lighting preset name string

### Build Approach
1. Define CinematicStepType enum with all 16 types
2. Define CinematicStep struct: CinematicStepType type, string stringParam, float floatParam, int intParam, float duration, bool waitForCompletion
3. Create CinematicSequence ScriptableObject with CinematicStep[] steps and [CreateAssetMenu]
4. Create CinematicSequencer MonoBehaviour with serialized references to all subsystems (ScreenEffects, SimpleAudioManager, DialogueManager, CinematicCamera, MissionManager, PlayerMovement, Transform[] npcReferences)
5. Implement ExecuteStep(CinematicStep) with switch on type, calling the appropriate subsystem method
6. Implement Play(): start coroutine that iterates steps; for each step call ExecuteStep, then if waitForCompletion yield until the step signals completion (via callback or duration wait)
7. Implement Pause/Resume: use a bool flag checked in the coroutine loop (yield while paused)
8. Implement Skip: stop coroutine, apply terminal states (enable player control, hide dialogue, etc.), fire OnSequenceComplete
9. Wire OnSequenceComplete UnityEvent

### Testing Strategy
- EditMode tests: CinematicSequence creation, verify steps array serialization
- EditMode tests: CinematicStepType enum has all expected values
- PlayMode tests: Play with a 3-step sequence (DisablePlayerControl, Wait 0.1s, EnablePlayerControl), verify player control toggled
- PlayMode tests: Pause during execution, verify step does not advance until Resume
- PlayMode tests: Skip fires OnSequenceComplete
- PlayMode tests: waitForCompletion=false allows next step to start immediately
- Integration test: wire ScreenEffects + DialogueManager, run a Fade + Dialogue sequence, verify both execute

---

## Task Breakdown

| # | Type | Action | Depends | Acceptance |
|---|------|--------|---------|------------|
| 1 | code | Define CinematicStepType enum with all 16 step types | — | Enum compiles, all types present |
| 2 | code | Define CinematicStep struct with type, params, duration, waitForCompletion | 1 | Struct is serializable, all fields editable in Inspector |
| 3 | code | Create CinematicSequence ScriptableObject with steps array | 2 | Asset creatable via Create menu, steps editable |
| 4 | code | Create CinematicSequencer MonoBehaviour with subsystem references | — | Component attaches, all references assignable in Inspector |
| 5 | code | Implement ExecuteStep switch dispatching to all subsystems | 1,4 | Each step type calls the correct subsystem method |
| 6 | code | Implement Play coroutine with sequential step execution and waitForCompletion | 3,5 | Steps execute in order, waiting steps block, non-waiting steps overlap |
| 7 | code | Implement Pause and Resume | 6 | Execution halts on Pause, resumes on Resume |
| 8 | code | Implement Skip with terminal state application | 6 | All remaining steps skipped, OnSequenceComplete fires |
| 9 | code | Implement player control toggle (DisablePlayerControl / EnablePlayerControl) | 4 | PlayerMovement.enabled toggled correctly |
| 10 | test | Write EditMode + PlayMode tests for sequencer execution flow | 1-9 | All tests pass, coverage on Play/Pause/Resume/Skip |
