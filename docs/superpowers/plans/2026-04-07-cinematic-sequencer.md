# INT-005: Cinematic Sequencer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the central orchestrator that drives the intro cinematic by dispatching ordered steps to camera, dialogue, audio, effects, NPC, and mission subsystems.

**Architecture:** Data-driven step list (ScriptableObject) executed by a coroutine-based MonoBehaviour. Hybrid string-key + serialized reference registries for type-safe, Inspector-friendly subsystem resolution. 4 source files + 2 test files.

**Tech Stack:** Unity 6, C#, NUnit, Coroutines, ScriptableObjects, UnityEvents

---

## File Map

| File | Action | Responsibility |
|------|--------|---------------|
| `Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicStepType.cs` | Create | Enum with 17 step types |
| `Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicStep.cs` | Create | Serializable struct: type, params, duration, waitForCompletion |
| `Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicSequence.cs` | Create | ScriptableObject holding ordered CinematicStep[] |
| `Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicSequencer.cs` | Create | MonoBehaviour: registries, Validate, Play/Pause/Resume/Skip, ExecuteStep |
| `Assets/Tests/EditMode/CinematicSequencerTests.cs` | Create | EditMode tests for enum, struct, validation, parsing |
| `Assets/Tests/PlayMode/CinematicSequencerPlayTests.cs` | Create | PlayMode tests for execution flow, pause/resume, skip |

---

### Task 1: CinematicStepType Enum

**Files:**
- Create: `Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicStepType.cs`
- Create: `Assets/Tests/EditMode/CinematicSequencerTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// Assets/Tests/EditMode/CinematicSequencerTests.cs
using System;
using NUnit.Framework;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class CinematicStepTypeTests
    {
        [Test]
        public void StepType_Has17Values()
        {
            var values = Enum.GetValues(typeof(CinematicStepType));
            Assert.AreEqual(17, values.Length);
        }

        [Test]
        [TestCase(CinematicStepType.CameraMove)]
        [TestCase(CinematicStepType.Dialogue)]
        [TestCase(CinematicStepType.Wait)]
        [TestCase(CinematicStepType.PlaySFX)]
        [TestCase(CinematicStepType.PlayMusic)]
        [TestCase(CinematicStepType.StopMusic)]
        [TestCase(CinematicStepType.Fade)]
        [TestCase(CinematicStepType.Shake)]
        [TestCase(CinematicStepType.Letterbox)]
        [TestCase(CinematicStepType.ObjectivePopup)]
        [TestCase(CinematicStepType.MissionStart)]
        [TestCase(CinematicStepType.MissionComplete)]
        [TestCase(CinematicStepType.EnablePlayerControl)]
        [TestCase(CinematicStepType.DisablePlayerControl)]
        [TestCase(CinematicStepType.ActivateNPC)]
        [TestCase(CinematicStepType.DeactivateNPC)]
        [TestCase(CinematicStepType.SetLighting)]
        public void StepType_ContainsExpectedValue(CinematicStepType type)
        {
            Assert.IsTrue(Enum.IsDefined(typeof(CinematicStepType), type));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `.ai/scripts/run-tests.sh editmode`
Expected: FAIL — `CinematicStepType` does not exist.

- [ ] **Step 3: Write the enum**

```csharp
// Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicStepType.cs
namespace FarmSimVR.MonoBehaviours.Cinematics
{
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
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `.ai/scripts/run-tests.sh editmode`
Expected: PASS — both tests green.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicStepType.cs Assets/Tests/EditMode/CinematicSequencerTests.cs
git commit -m "feat(INT-005): add CinematicStepType enum with 17 step types"
```

---

### Task 2: CinematicStep Struct

**Files:**
- Create: `Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicStep.cs`
- Modify: `Assets/Tests/EditMode/CinematicSequencerTests.cs`

- [ ] **Step 1: Write the failing test**

Append to `CinematicSequencerTests.cs`:

```csharp
[TestFixture]
public class CinematicStepTests
{
    [Test]
    public void Step_DefaultValues_AreZeroAndFalse()
    {
        var step = new CinematicStep();

        Assert.AreEqual(CinematicStepType.CameraMove, step.type);
        Assert.AreEqual("", step.stringParam ?? "");
        Assert.AreEqual(0f, step.floatParam);
        Assert.AreEqual(0, step.intParam);
        Assert.AreEqual(0f, step.duration);
        Assert.IsFalse(step.waitForCompletion);
    }

    [Test]
    public void MissionStart_ParsesPipeDelimiter()
    {
        var step = new CinematicStep
        {
            type = CinematicStepType.MissionStart,
            stringParam = "POLLO LOCO|Capture El Pollo Loco"
        };

        string[] parts = step.stringParam.Split('|');
        Assert.AreEqual(2, parts.Length);
        Assert.AreEqual("POLLO LOCO", parts[0]);
        Assert.AreEqual("Capture El Pollo Loco", parts[1]);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `.ai/scripts/run-tests.sh editmode`
Expected: FAIL — `CinematicStep` does not exist.

- [ ] **Step 3: Write the struct**

```csharp
// Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicStep.cs
using System;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    [Serializable]
    public struct CinematicStep
    {
        public CinematicStepType type;
        public string stringParam;
        public float floatParam;
        public int intParam;
        public float duration;
        public bool waitForCompletion;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `.ai/scripts/run-tests.sh editmode`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicStep.cs Assets/Tests/EditMode/CinematicSequencerTests.cs
git commit -m "feat(INT-005): add CinematicStep serializable struct"
```

---

### Task 3: CinematicSequence ScriptableObject

**Files:**
- Create: `Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicSequence.cs`
- Modify: `Assets/Tests/EditMode/CinematicSequencerTests.cs`

- [ ] **Step 1: Write the failing test**

Append to `CinematicSequencerTests.cs`:

```csharp
using UnityEngine;

[TestFixture]
public class CinematicSequenceTests
{
    [Test]
    public void Sequence_CreatedWithEmptySteps()
    {
        var sequence = ScriptableObject.CreateInstance<CinematicSequence>();

        Assert.IsNotNull(sequence);
        Assert.IsNotNull(sequence.steps);
        Assert.AreEqual(0, sequence.steps.Length);

        Object.DestroyImmediate(sequence);
    }

    [Test]
    public void Sequence_StepsAreAssignable()
    {
        var sequence = ScriptableObject.CreateInstance<CinematicSequence>();
        sequence.steps = new[]
        {
            new CinematicStep { type = CinematicStepType.Wait, duration = 1f },
            new CinematicStep { type = CinematicStepType.Fade, floatParam = 1f, duration = 0.5f }
        };

        Assert.AreEqual(2, sequence.steps.Length);
        Assert.AreEqual(CinematicStepType.Wait, sequence.steps[0].type);
        Assert.AreEqual(CinematicStepType.Fade, sequence.steps[1].type);

        Object.DestroyImmediate(sequence);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `.ai/scripts/run-tests.sh editmode`
Expected: FAIL — `CinematicSequence` does not exist.

- [ ] **Step 3: Write the ScriptableObject**

```csharp
// Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicSequence.cs
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    [CreateAssetMenu(fileName = "NewCinematicSequence", menuName = "FarmSimVR/Cinematic Sequence")]
    public class CinematicSequence : ScriptableObject
    {
        public CinematicStep[] steps = new CinematicStep[0];
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `.ai/scripts/run-tests.sh editmode`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicSequence.cs Assets/Tests/EditMode/CinematicSequencerTests.cs
git commit -m "feat(INT-005): add CinematicSequence ScriptableObject"
```

---

### Task 4: CinematicSequencer — Registry + Validate

**Files:**
- Create: `Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicSequencer.cs`
- Modify: `Assets/Tests/EditMode/CinematicSequencerTests.cs`

- [ ] **Step 1: Write the failing tests**

Append to `CinematicSequencerTests.cs`:

```csharp
[TestFixture]
public class CinematicSequencerValidationTests
{
    private CinematicSequencer CreateSequencer()
    {
        var go = new GameObject("Sequencer");
        return go.AddComponent<CinematicSequencer>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in Object.FindObjectsByType<CinematicSequencer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(go.gameObject);
    }

    [Test]
    public void Validate_EmptySequence_ReturnsTrue()
    {
        var sequencer = CreateSequencer();
        var sequence = ScriptableObject.CreateInstance<CinematicSequence>();

        bool result = sequencer.Validate(sequence);

        Assert.IsTrue(result);
        Object.DestroyImmediate(sequence);
    }

    [Test]
    public void Validate_WaitStep_NoKeyNeeded_ReturnsTrue()
    {
        var sequencer = CreateSequencer();
        var sequence = ScriptableObject.CreateInstance<CinematicSequence>();
        sequence.steps = new[]
        {
            new CinematicStep { type = CinematicStepType.Wait, duration = 1f }
        };

        bool result = sequencer.Validate(sequence);

        Assert.IsTrue(result);
        Object.DestroyImmediate(sequence);
    }

    [Test]
    public void Validate_DialogueStep_WithBadKey_ReturnsFalse()
    {
        var sequencer = CreateSequencer();
        var sequence = ScriptableObject.CreateInstance<CinematicSequence>();
        sequence.steps = new[]
        {
            new CinematicStep { type = CinematicStepType.Dialogue, stringParam = "NonExistent" }
        };

        bool result = sequencer.Validate(sequence);

        Assert.IsFalse(result);
        Object.DestroyImmediate(sequence);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `.ai/scripts/run-tests.sh editmode`
Expected: FAIL — `CinematicSequencer` does not exist.

- [ ] **Step 3: Write the sequencer with registries and Validate**

```csharp
// Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicSequencer.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using FarmSimVR.MonoBehaviours.Hunting;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
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

    public class CinematicSequencer : MonoBehaviour
    {
        [Header("Subsystems")]
        [SerializeField] private ScreenEffects _screenEffects;
        [SerializeField] private SimpleAudioManager _audioManager;
        [SerializeField] private DialogueManager _dialogueManager;
        [SerializeField] private CinematicCamera _cinematicCamera;
        [SerializeField] private MissionManager _missionManager;
        [SerializeField] private PlayerMovement _playerMovement;

        [Header("Registries")]
        [SerializeField] private NamedDialogue[] _dialogueAssets = new NamedDialogue[0];
        [SerializeField] private NamedCameraPath[] _cameraPaths = new NamedCameraPath[0];
        [SerializeField] private NamedNPC[] _npcs = new NamedNPC[0];

        [Header("Events")]
        public UnityEvent OnSequenceComplete;

        private bool _isPlaying;
        private bool _isPaused;
        private int _startIndex;

        public bool IsPlaying => _isPlaying;
        public bool IsPaused => _isPaused;

        private Dictionary<string, DialogueData> _dialogueLookup;
        private Dictionary<string, CameraPath> _pathLookup;
        private Dictionary<string, NPCController> _npcLookup;

        private void BuildLookups()
        {
            _dialogueLookup = new Dictionary<string, DialogueData>();
            foreach (var entry in _dialogueAssets)
                if (!string.IsNullOrEmpty(entry.key) && entry.data != null)
                    _dialogueLookup[entry.key] = entry.data;

            _pathLookup = new Dictionary<string, CameraPath>();
            foreach (var entry in _cameraPaths)
                if (!string.IsNullOrEmpty(entry.key) && entry.path != null)
                    _pathLookup[entry.key] = entry.path;

            _npcLookup = new Dictionary<string, NPCController>();
            foreach (var entry in _npcs)
                if (!string.IsNullOrEmpty(entry.key) && entry.controller != null)
                    _npcLookup[entry.key] = entry.controller;
        }

        public bool Validate(CinematicSequence sequence)
        {
            BuildLookups();
            if (sequence == null || sequence.steps == null || sequence.steps.Length == 0)
                return true;

            bool valid = true;
            for (int i = 0; i < sequence.steps.Length; i++)
            {
                var step = sequence.steps[i];
                string key = step.stringParam;

                switch (step.type)
                {
                    case CinematicStepType.Dialogue:
                        if (!_dialogueLookup.ContainsKey(key ?? ""))
                        {
                            Debug.LogWarning($"[CinematicSequencer] Step {i}: Dialogue key '{key}' not found in registry.");
                            valid = false;
                        }
                        break;
                    case CinematicStepType.CameraMove:
                        if (!string.IsNullOrEmpty(key) && !_pathLookup.ContainsKey(key))
                        {
                            Debug.LogWarning($"[CinematicSequencer] Step {i}: CameraPath key '{key}' not found in registry.");
                            valid = false;
                        }
                        break;
                    case CinematicStepType.ActivateNPC:
                    case CinematicStepType.DeactivateNPC:
                        if (!_npcLookup.ContainsKey(key ?? ""))
                        {
                            Debug.LogWarning($"[CinematicSequencer] Step {i}: NPC key '{key}' not found in registry.");
                            valid = false;
                        }
                        break;
                }
            }
            return valid;
        }
    }
}
```

Note: `SimpleAudioManager` is in `FarmSimVR.MonoBehaviours.Audio` namespace — you'll need a `using FarmSimVR.MonoBehaviours.Audio;` if it's in a separate namespace. Check the actual file. `PlayerMovement` is in `FarmSimVR.MonoBehaviours.Hunting`.

- [ ] **Step 4: Run test to verify it passes**

Run: `.ai/scripts/run-tests.sh editmode`
Expected: PASS — all 3 validation tests green.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicSequencer.cs Assets/Tests/EditMode/CinematicSequencerTests.cs
git commit -m "feat(INT-005): add CinematicSequencer with registries and Validate"
```

---

### Task 5: CinematicSequencer — ExecuteStep Dispatch

**Files:**
- Modify: `Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicSequencer.cs`

- [ ] **Step 1: Add ExecuteStep method to CinematicSequencer**

Add inside the `CinematicSequencer` class:

```csharp
        private void ExecuteStep(CinematicStep step, out Action onComplete)
        {
            onComplete = null;
            string key = step.stringParam ?? "";

            switch (step.type)
            {
                case CinematicStepType.Wait:
                    // Handled by coroutine yield — no subsystem call
                    break;

                case CinematicStepType.Fade:
                    if (_screenEffects == null) { LogMissing("ScreenEffects", step); break; }
                    Action fadeComplete = null;
                    if (step.waitForCompletion) { var tcs = new CompletionFlag(); fadeComplete = () => tcs.Done = true; onComplete = () => tcs.Done = true; }
                    if (step.floatParam > 0f)
                        _screenEffects.FadeToBlack(step.duration, fadeComplete);
                    else
                        _screenEffects.FadeFromBlack(step.duration, fadeComplete);
                    break;

                case CinematicStepType.Shake:
                    if (_screenEffects == null) { LogMissing("ScreenEffects", step); break; }
                    if (step.waitForCompletion) { var tcs = new CompletionFlag(); onComplete = () => tcs.Done = true; _screenEffects.ScreenShake(step.floatParam, step.duration, () => tcs.Done = true); }
                    else _screenEffects.ScreenShake(step.floatParam, step.duration);
                    break;

                case CinematicStepType.Letterbox:
                    if (_screenEffects == null) { LogMissing("ScreenEffects", step); break; }
                    if (step.floatParam > 0f)
                        _screenEffects.ShowLetterbox(step.floatParam, step.duration, step.waitForCompletion ? () => { } : null);
                    else
                        _screenEffects.HideLetterbox(step.duration, step.waitForCompletion ? () => { } : null);
                    break;

                case CinematicStepType.ObjectivePopup:
                    if (_screenEffects == null) { LogMissing("ScreenEffects", step); break; }
                    _screenEffects.ShowObjective(key);
                    break;

                case CinematicStepType.Dialogue:
                    if (_dialogueManager == null) { LogMissing("DialogueManager", step); break; }
                    if (_dialogueLookup.TryGetValue(key, out var dialogueData))
                    {
                        _dialogueManager.Show();
                        _dialogueManager.StartDialogue(dialogueData);
                    }
                    break;

                case CinematicStepType.CameraMove:
                    if (_cinematicCamera == null) { LogMissing("CinematicCamera", step); break; }
                    if (!string.IsNullOrEmpty(key) && _pathLookup.TryGetValue(key, out var cameraPath))
                        _cinematicCamera.PlayPath(cameraPath);
                    else
                        _cinematicCamera.MoveToWaypoint(step.intParam);
                    break;

                case CinematicStepType.PlaySFX:
                    if (_audioManager == null) { LogMissing("SimpleAudioManager", step); break; }
                    _audioManager.PlaySFXByKey(key);
                    break;

                case CinematicStepType.PlayMusic:
                    if (_audioManager == null) { LogMissing("SimpleAudioManager", step); break; }
                    _audioManager.PlayMusicByKey(key, step.duration);
                    break;

                case CinematicStepType.StopMusic:
                    if (_audioManager == null) { LogMissing("SimpleAudioManager", step); break; }
                    _audioManager.StopMusic(step.duration);
                    break;

                case CinematicStepType.MissionStart:
                    if (_missionManager == null) { LogMissing("MissionManager", step); break; }
                    string[] parts = key.Split('|');
                    string missionName = parts[0];
                    string objective = parts.Length > 1 ? parts[1] : "";
                    _missionManager.StartMission(missionName, objective);
                    break;

                case CinematicStepType.MissionComplete:
                    if (_missionManager == null) { LogMissing("MissionManager", step); break; }
                    _missionManager.CompleteMission();
                    break;

                case CinematicStepType.EnablePlayerControl:
                    if (_playerMovement != null) _playerMovement.enabled = true;
                    break;

                case CinematicStepType.DisablePlayerControl:
                    if (_playerMovement != null) _playerMovement.enabled = false;
                    break;

                case CinematicStepType.ActivateNPC:
                    if (_npcLookup.TryGetValue(key, out var npcToActivate))
                        npcToActivate.Activate();
                    break;

                case CinematicStepType.DeactivateNPC:
                    if (_npcLookup.TryGetValue(key, out var npcToDeactivate))
                        npcToDeactivate.Deactivate();
                    break;

                case CinematicStepType.SetLighting:
                    Debug.Log($"[CinematicSequencer] SetLighting '{key}' — not yet implemented (INT-009).");
                    break;
            }
        }

        private void LogMissing(string subsystem, CinematicStep step)
        {
            Debug.LogWarning($"[CinematicSequencer] {subsystem} not assigned. Skipping {step.type} step.");
        }

        private class CompletionFlag
        {
            public bool Done;
        }
```

- [ ] **Step 2: Verify project compiles**

Run: `.ai/scripts/run-tests.sh editmode`
Expected: PASS — existing tests still green, no compile errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicSequencer.cs
git commit -m "feat(INT-005): add ExecuteStep dispatch for all 17 step types"
```

---

### Task 6: CinematicSequencer — Play / Pause / Resume / Skip

**Files:**
- Modify: `Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicSequencer.cs`
- Create: `Assets/Tests/PlayMode/CinematicSequencerPlayTests.cs`

- [ ] **Step 1: Write the failing PlayMode tests**

```csharp
// Assets/Tests/PlayMode/CinematicSequencerPlayTests.cs
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Hunting;

namespace FarmSimVR.Tests.PlayMode
{
    [TestFixture]
    public class CinematicSequencerPlayTests
    {
        private GameObject _sequencerGo;
        private CinematicSequencer _sequencer;

        [SetUp]
        public void SetUp()
        {
            _sequencerGo = new GameObject("TestSequencer");
            _sequencer = _sequencerGo.AddComponent<CinematicSequencer>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_sequencerGo);
        }

        [UnityTest]
        public IEnumerator Play_EmptySequence_FiresCompleteImmediately()
        {
            var sequence = ScriptableObject.CreateInstance<CinematicSequence>();
            bool completed = false;
            _sequencer.OnSequenceComplete.AddListener(() => completed = true);

            _sequencer.Play(sequence);
            yield return null; // one frame for coroutine start

            Assert.IsTrue(completed);
            Object.Destroy(sequence);
        }

        [UnityTest]
        public IEnumerator Play_WaitStep_CompletesAfterDuration()
        {
            var sequence = ScriptableObject.CreateInstance<CinematicSequence>();
            sequence.steps = new[]
            {
                new CinematicStep { type = CinematicStepType.Wait, duration = 0.1f, waitForCompletion = true }
            };
            bool completed = false;
            _sequencer.OnSequenceComplete.AddListener(() => completed = true);

            _sequencer.Play(sequence);
            Assert.IsFalse(completed);

            yield return new WaitForSecondsRealtime(0.2f);

            Assert.IsTrue(completed);
            Object.Destroy(sequence);
        }

        [UnityTest]
        public IEnumerator Play_PlayerControlSteps_TogglesMovement()
        {
            var playerGo = new GameObject("Player");
            var pm = playerGo.AddComponent<PlayerMovement>();
            var cc = playerGo.AddComponent<CharacterController>();
            pm.enabled = true;

            // Use reflection to set the serialized field
            var field = typeof(CinematicSequencer).GetField("_playerMovement",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_sequencer, pm);

            var sequence = ScriptableObject.CreateInstance<CinematicSequence>();
            sequence.steps = new[]
            {
                new CinematicStep { type = CinematicStepType.DisablePlayerControl },
                new CinematicStep { type = CinematicStepType.Wait, duration = 0.05f, waitForCompletion = true },
                new CinematicStep { type = CinematicStepType.EnablePlayerControl }
            };

            _sequencer.Play(sequence);
            yield return null;

            Assert.IsFalse(pm.enabled, "PlayerMovement should be disabled after DisablePlayerControl step");

            yield return new WaitForSecondsRealtime(0.1f);

            Assert.IsTrue(pm.enabled, "PlayerMovement should be re-enabled after EnablePlayerControl step");

            Object.Destroy(sequence);
            Object.Destroy(playerGo);
        }

        [UnityTest]
        public IEnumerator Skip_FiresOnSequenceComplete()
        {
            var sequence = ScriptableObject.CreateInstance<CinematicSequence>();
            sequence.steps = new[]
            {
                new CinematicStep { type = CinematicStepType.Wait, duration = 10f, waitForCompletion = true }
            };
            bool completed = false;
            _sequencer.OnSequenceComplete.AddListener(() => completed = true);

            _sequencer.Play(sequence);
            yield return null;

            _sequencer.Skip();

            Assert.IsTrue(completed);
            Assert.IsFalse(_sequencer.IsPlaying);
            Object.Destroy(sequence);
        }

        [UnityTest]
        public IEnumerator Pause_HaltsExecution_ResumeResumes()
        {
            var sequence = ScriptableObject.CreateInstance<CinematicSequence>();
            sequence.steps = new[]
            {
                new CinematicStep { type = CinematicStepType.Wait, duration = 0.05f, waitForCompletion = true },
                new CinematicStep { type = CinematicStepType.Wait, duration = 0.05f, waitForCompletion = true }
            };
            bool completed = false;
            _sequencer.OnSequenceComplete.AddListener(() => completed = true);

            _sequencer.Play(sequence);
            yield return null;

            _sequencer.Pause();
            Assert.IsTrue(_sequencer.IsPaused);

            yield return new WaitForSecondsRealtime(0.2f);
            Assert.IsFalse(completed, "Sequence should NOT complete while paused");

            _sequencer.Resume();
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.IsTrue(completed, "Sequence should complete after resume");

            Object.Destroy(sequence);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `.ai/scripts/run-tests.sh playmode`
Expected: FAIL — `Play(CinematicSequence)` method does not exist.

- [ ] **Step 3: Add Play, Pause, Resume, Skip, SkipToStep to CinematicSequencer**

Add inside the `CinematicSequencer` class:

```csharp
        public void Play(CinematicSequence sequence)
        {
            StopAllCoroutines();
            _startIndex = 0;
            BuildLookups();
            Validate(sequence);
            _isPlaying = true;
            _isPaused = false;
            StartCoroutine(RunSequence(sequence));
        }

        public void Pause()
        {
            if (_isPlaying) _isPaused = true;
        }

        public void Resume()
        {
            if (_isPaused) _isPaused = false;
        }

        public void Skip()
        {
            StopAllCoroutines();
            // Apply terminal states
            if (_screenEffects != null) _screenEffects.ResetAll();
            if (_dialogueManager != null) _dialogueManager.Hide();
            if (_playerMovement != null) _playerMovement.enabled = true;
            _isPlaying = false;
            _isPaused = false;
            OnSequenceComplete?.Invoke();
        }

        public void SkipToStep(CinematicSequence sequence, int stepIndex)
        {
            StopAllCoroutines();
            _startIndex = stepIndex;
            _isPlaying = true;
            _isPaused = false;
            StartCoroutine(RunSequence(sequence));
        }

        private IEnumerator RunSequence(CinematicSequence sequence)
        {
            if (sequence == null || sequence.steps == null || sequence.steps.Length == 0)
            {
                _isPlaying = false;
                OnSequenceComplete?.Invoke();
                yield break;
            }

            for (int i = _startIndex; i < sequence.steps.Length; i++)
            {
                // Pause check
                while (_isPaused)
                    yield return null;

                var step = sequence.steps[i];

                ExecuteStep(step, out var onComplete);

                if (step.waitForCompletion)
                {
                    if (step.type == CinematicStepType.Wait)
                    {
                        yield return new WaitForSecondsRealtime(step.duration);
                    }
                    else if (IsCallbackStep(step.type))
                    {
                        var flag = new CompletionFlag();
                        // Re-execute with completion tracking
                        ExecuteStepWithCallback(step, flag);
                        float timeout = step.duration + 5f;
                        float elapsed = 0f;
                        while (!flag.Done && elapsed < timeout)
                        {
                            elapsed += Time.unscaledDeltaTime;
                            yield return null;
                        }
                        if (!flag.Done)
                            Debug.LogWarning($"[CinematicSequencer] Step {i} ({step.type}) timed out after {timeout}s.");
                    }
                    else if (step.duration > 0f)
                    {
                        yield return new WaitForSecondsRealtime(step.duration);
                    }
                }
            }

            _isPlaying = false;
            OnSequenceComplete?.Invoke();
        }

        private bool IsCallbackStep(CinematicStepType type)
        {
            return type == CinematicStepType.Fade
                || type == CinematicStepType.Shake
                || type == CinematicStepType.Letterbox
                || type == CinematicStepType.Dialogue
                || type == CinematicStepType.CameraMove;
        }

        private void ExecuteStepWithCallback(CinematicStep step, CompletionFlag flag)
        {
            string key = step.stringParam ?? "";
            switch (step.type)
            {
                case CinematicStepType.Fade:
                    if (_screenEffects == null) break;
                    if (step.floatParam > 0f) _screenEffects.FadeToBlack(step.duration, () => flag.Done = true);
                    else _screenEffects.FadeFromBlack(step.duration, () => flag.Done = true);
                    break;
                case CinematicStepType.Shake:
                    if (_screenEffects == null) break;
                    _screenEffects.ScreenShake(step.floatParam, step.duration, () => flag.Done = true);
                    break;
                case CinematicStepType.Letterbox:
                    if (_screenEffects == null) break;
                    if (step.floatParam > 0f) _screenEffects.ShowLetterbox(step.floatParam, step.duration, () => flag.Done = true);
                    else _screenEffects.HideLetterbox(step.duration, () => flag.Done = true);
                    break;
                case CinematicStepType.Dialogue:
                    if (_dialogueManager == null) break;
                    if (_dialogueLookup.TryGetValue(key, out var data))
                    {
                        _dialogueManager.Show();
                        _dialogueManager.StartDialogue(data);
                        _dialogueManager.OnDialogueComplete.AddListener(() => flag.Done = true);
                    }
                    break;
                case CinematicStepType.CameraMove:
                    if (_cinematicCamera == null) break;
                    if (!string.IsNullOrEmpty(key) && _pathLookup.TryGetValue(key, out var path))
                    {
                        _cinematicCamera.PlayPath(path);
                        _cinematicCamera.OnWaypointReached.AddListener(() => flag.Done = true);
                    }
                    break;
            }
        }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `.ai/scripts/run-tests.sh all`
Expected: PASS — all EditMode + PlayMode tests green.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicSequencer.cs Assets/Tests/PlayMode/CinematicSequencerPlayTests.cs
git commit -m "feat(INT-005): add Play/Pause/Resume/Skip execution with PlayMode tests"
```

---

### Task 7: Final Integration Test + Cleanup

**Files:**
- Modify: `Assets/Specs/Features/INT-005-cinematic-sequencer.md` (mark acceptance criteria)
- Modify: `SPECS.md` (update status)

- [ ] **Step 1: Run full test suite**

Run: `.ai/scripts/run-tests.sh all`
Expected: ALL PASS

- [ ] **Step 2: Update SPECS.md**

Change INT-005 status from `Open` to `Done`.

- [ ] **Step 3: Final commit**

```bash
git add SPECS.md Assets/Specs/Features/INT-005-cinematic-sequencer.md
git commit -m "chore(INT-005): mark cinematic sequencer as Done"
```

- [ ] **Step 4: Push and create PR**

```bash
git push -u origin feature/INT-005-cinematic-sequencer
gh pr create --title "feat(INT-005): cinematic sequencer" --body "Implements INT-005..."
```
