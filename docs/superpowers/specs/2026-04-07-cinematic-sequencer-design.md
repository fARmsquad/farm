# INT-005: Cinematic Sequencer — Design

**Date:** 2026-04-07
**Status:** Approved
**Depends on:** INT-001 (ScreenEffects), INT-002 (SimpleAudioManager), INT-003 (DialogueManager), INT-004 (CinematicCamera), INT-006 (NPCController), INT-007 (MissionManager)

---

## Summary

The Cinematic Sequencer orchestrates all cinematic subsystems (camera, dialogue, audio, screen effects, NPCs, missions) through a data-driven step list authored as a ScriptableObject. It uses a hybrid reference registry where steps reference subsystems by string key, validated upfront on Play().

## Architecture

### Files (all in `Assets/_Project/Scripts/MonoBehaviours/Cinematics/`)

| File | Type | LOC est. | Purpose |
|------|------|----------|---------|
| `CinematicStepType.cs` | enum | ~25 | 16 step type constants |
| `CinematicStep.cs` | serializable struct | ~30 | One step: type, params, duration, waitForCompletion |
| `CinematicSequence.cs` | ScriptableObject | ~15 | Ordered CinematicStep[] array with CreateAssetMenu |
| `CinematicSequencer.cs` | MonoBehaviour | ~300 | Brain: registries, Validate, Play/Pause/Resume/Skip, ExecuteStep dispatch |

### Hybrid Reference Registry

CinematicSequencer holds named reference arrays for type-safe resolution with Inspector-friendly keys:

```csharp
[Serializable]
public struct NamedDialogue
{
    public string key;
    public DialogueData data;
}

[Serializable]
public struct NamedCameraPath
{
    public string key;
    public CameraPath path;
}

[Serializable]
public struct NamedNPC
{
    public string key;
    public NPCController controller;
}

// On CinematicSequencer:
[Header("Registries")]
[SerializeField] private NamedDialogue[] dialogueAssets;
[SerializeField] private NamedCameraPath[] cameraPaths;
[SerializeField] private NamedNPC[] npcs;
```

CinematicStep.stringParam holds the key. On `Play()`, `Validate()` iterates all steps and checks every key resolves against the appropriate registry. Bad key = warning log + step skipped at runtime.

### CinematicStepType Enum (16 types)

```csharp
public enum CinematicStepType
{
    CameraMove,
    Dialogue,
    Wait,
    PlaySFX,
    PlayMusic,
    StopMusic,
    Fade,
    Shake,
    Letterbox,
    ObjectivePopup,
    MissionStart,
    MissionComplete,
    EnablePlayerControl,
    DisablePlayerControl,
    ActivateNPC,
    DeactivateNPC,
    SetLighting
}
```

Note: 17 types total (StopMusic added for explicit music stop control). SetLighting is a future integration point for INT-009 — logs warning and no-ops until LightingTransition exists.

### CinematicStep Struct

```csharp
[Serializable]
public struct CinematicStep
{
    public CinematicStepType type;
    public string stringParam;    // key into registry, text, or audio key
    public float floatParam;      // intensity, height%, direction flag
    public int intParam;          // waypoint index
    public float duration;        // seconds
    public bool waitForCompletion;
}
```

### CinematicSequence ScriptableObject

```csharp
[CreateAssetMenu(fileName = "NewSequence", menuName = "FarmSimVR/Cinematic Sequence")]
public class CinematicSequence : ScriptableObject
{
    public CinematicStep[] steps;
}
```

## Execution Flow

### Play()

```
Play()
  → StopAllCoroutines (restart if already playing)
  → Validate() — check all keys resolve, log warnings for bad ones
  → _isPlaying = true
  → StartCoroutine(RunSequence)

RunSequence:
  for i = _startIndex to steps.Length:
    while _isPaused: yield return null
    ExecuteStep(steps[i], out callback)
    if waitForCompletion:
      yield until callback fires OR duration elapses (whichever comes first)
  _isPlaying = false
  OnSequenceComplete.Invoke()
```

### ExecuteStep Dispatch

| StepType | Call | Params |
|----------|------|--------|
| CameraMove | `CinematicCamera.PlayPath(path)` or `.MoveToWaypoint(intParam)` | stringParam = path key (if set), intParam = waypoint index |
| Dialogue | `DialogueManager.Show(); .StartDialogue(data)` | stringParam = dialogue key |
| Wait | yield WaitForSecondsRealtime(duration) | duration |
| PlaySFX | `SimpleAudioManager.PlaySFXByKey(key)` | stringParam = audio key |
| PlayMusic | `SimpleAudioManager.PlayMusicByKey(key, duration)` | stringParam = audio key, duration = fade-in |
| StopMusic | `SimpleAudioManager.StopMusic(duration)` | duration = fade-out |
| Fade | floatParam > 0 → `FadeToBlack(duration)`, else `FadeFromBlack(duration)` | floatParam = direction, duration |
| Shake | `ScreenShake(floatParam, duration)` | floatParam = intensity |
| Letterbox | floatParam > 0 → `ShowLetterbox(floatParam, duration)`, else `HideLetterbox(duration)` | floatParam = height% (0 = hide) |
| ObjectivePopup | `ShowObjective(stringParam)` | stringParam = text |
| MissionStart | `StartMission(name, objective)` | stringParam = "name\|objective" split on pipe |
| MissionComplete | `CompleteMission()` | none |
| EnablePlayerControl | `PlayerMovement.enabled = true` | none |
| DisablePlayerControl | `PlayerMovement.enabled = false` | none |
| ActivateNPC | `npc.Activate()` | stringParam = NPC key |
| DeactivateNPC | `npc.Deactivate()` | stringParam = NPC key |
| SetLighting | no-op (INT-009 integration point) | stringParam = preset key |

### Completion detection per step type

- **Callback-based** (Fade, Shake, Letterbox, Objective, MissionPassed, Dialogue, CameraMove): subsystem fires `Action onComplete` callback → sequencer resumes.
- **Duration-based** (Wait, PlaySFX, PlayMusic, StopMusic): yield `WaitForSecondsRealtime(duration)`.
- **Instant** (EnablePlayerControl, DisablePlayerControl, ActivateNPC, DeactivateNPC, MissionStart, MissionComplete, SetLighting): complete immediately, no wait even if waitForCompletion is true.

Timeout safety: if a callback-based step hasn't fired after `duration + 5s`, auto-advance with a warning log. Prevents sequence hangs from missing callbacks.

### Pause / Resume

```csharp
public void Pause()  { if (_isPlaying) _isPaused = true; }
public void Resume() { if (_isPaused) _isPaused = false; }
```

Coroutine checks `_isPaused` at the top of each step iteration.

### Skip / SkipToStep

```csharp
public void Skip()
{
    StopAllCoroutines();
    // Apply terminal states
    if (_screenEffects != null) { _screenEffects.ResetAll(); }
    if (_dialogueManager != null) { _dialogueManager.Hide(); }
    if (_playerMovement != null) { _playerMovement.enabled = true; }
    _isPlaying = false;
    _isPaused = false;
    OnSequenceComplete?.Invoke();
}

public void SkipToStep(int stepIndex)
{
    StopAllCoroutines();
    _startIndex = stepIndex;
    StartCoroutine(RunSequence());
}
```

`SkipToStep` is used by INT-012's SkipPrompt to jump to Panel 5's gameplay transition.

### Edge Cases

- `Play()` while playing: restarts from step 0.
- `Pause()` when not playing: no-op.
- `Resume()` when not paused: no-op.
- `Skip()` when not playing: fires OnSequenceComplete immediately.
- Zero-step sequence: fires OnSequenceComplete immediately on Play().
- Null subsystem reference: logs warning, skips step.
- Bad registry key: logged during Validate(), skipped during execution.

## Subsystem References

CinematicSequencer holds direct serialized references (not singletons) for testability:

```csharp
[Header("Subsystems")]
[SerializeField] private ScreenEffects _screenEffects;
[SerializeField] private SimpleAudioManager _audioManager;
[SerializeField] private DialogueManager _dialogueManager;
[SerializeField] private CinematicCamera _cinematicCamera;
[SerializeField] private MissionManager _missionManager;
[SerializeField] private PlayerMovement _playerMovement;
```

Falls back to singleton Instance if serialized reference is null.

## Testing Strategy

### EditMode Tests (`CinematicSequencerTests.cs`)

1. `StepType_HasAll17Values` — enum count matches expected
2. `CinematicStep_IsSerializable` — struct round-trips
3. `CinematicSequence_CreatesWithEmptySteps` — default asset has empty array
4. `Validate_WithBadKey_ReturnsFalse` — catches typos
5. `Validate_WithGoodKeys_ReturnsTrue` — passes clean sequences
6. `MissionStart_ParsesPipeDelimiter` — "POLLO LOCO|Capture El Pollo" splits correctly

### PlayMode Tests (`CinematicSequencerPlayTests.cs`)

1. `Play_WithPlayerControlSteps_TogglesMovement` — DisablePlayer → Wait → EnablePlayer
2. `Play_WaitForCompletion_BlocksNextStep` — timed verification
3. `Play_WithoutWaitForCompletion_RunsConcurrently` — both steps start same frame
4. `Pause_HaltsExecution_ResumeResumes` — step count check before/after
5. `Skip_FiresOnSequenceComplete` — event listener verification
6. `Skip_EnablesPlayerControl` — terminal state check
7. `Play_EmptySequence_FiresCompleteImmediately` — zero steps edge case
8. `Play_NullSubsystem_SkipsStepWithWarning` — null reference handling
