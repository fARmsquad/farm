using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using FarmSimVR.MonoBehaviours.Audio;
using FarmSimVR.MonoBehaviours.Hunting;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Orchestrates cinematic sequences by resolving named registries (dialogue assets,
    /// camera paths, NPCs) and dispatching steps to subsystems. This class handles
    /// registry management and validation; playback logic is added in later tasks.
    /// </summary>
    public class CinematicSequencer : MonoBehaviour
    {
        #region Named Registry Structs

        [Serializable]
        public struct NamedDialogue
        {
            public string key;
            public DialogueData dialogue;
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
            public NPCController npc;
        }

        #endregion

        #region Serialized Fields — Subsystems

        [Header("Subsystem References")]
        [SerializeField] private ScreenEffects _screenEffects;
        [SerializeField] private SimpleAudioManager _audioManager;
        [SerializeField] private DialogueManager _dialogueManager;
        [SerializeField] private CinematicCamera _cinematicCamera;
        [SerializeField] private MissionManager _missionManager;
        [SerializeField] private PlayerMovement _playerMovement;

        #endregion

        #region Serialized Fields — Registries

        [Header("Registries")]
        [SerializeField] private NamedDialogue[] _dialogueAssets;
        [SerializeField] private NamedCameraPath[] _cameraPaths;
        [SerializeField] private NamedNPC[] _npcs;

        #endregion

        #region Events

        [Header("Events")]
        public UnityEvent OnSequenceComplete;

        #endregion

        #region Public Properties

        /// <summary>
        /// True while a cinematic sequence is actively playing.
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// True while a cinematic sequence is paused.
        /// </summary>
        public bool IsPaused { get; private set; }

        #endregion

        #region Lookup Dictionaries

        private Dictionary<string, DialogueData> _dialogueLookup;
        private Dictionary<string, CameraPath> _cameraPathLookup;
        private Dictionary<string, NPCController> _npcLookup;

        #endregion

        #region BuildLookups

        /// <summary>
        /// Builds internal Dictionary lookups from the serialized named arrays.
        /// Call this before Validate or playback if registries have changed at runtime.
        /// Also called automatically in Awake.
        /// </summary>
        public void BuildLookups()
        {
            _dialogueLookup = new Dictionary<string, DialogueData>();
            if (_dialogueAssets != null)
            {
                foreach (var entry in _dialogueAssets)
                {
                    if (string.IsNullOrEmpty(entry.key)) continue;
                    if (_dialogueLookup.ContainsKey(entry.key))
                    {
                        Debug.LogWarning($"[CinematicSequencer] Duplicate dialogue key: '{entry.key}'");
                        continue;
                    }
                    _dialogueLookup[entry.key] = entry.dialogue;
                }
            }

            _cameraPathLookup = new Dictionary<string, CameraPath>();
            if (_cameraPaths != null)
            {
                foreach (var entry in _cameraPaths)
                {
                    if (string.IsNullOrEmpty(entry.key)) continue;
                    if (_cameraPathLookup.ContainsKey(entry.key))
                    {
                        Debug.LogWarning($"[CinematicSequencer] Duplicate camera path key: '{entry.key}'");
                        continue;
                    }
                    _cameraPathLookup[entry.key] = entry.path;
                }
            }

            _npcLookup = new Dictionary<string, NPCController>();
            if (_npcs != null)
            {
                foreach (var entry in _npcs)
                {
                    if (string.IsNullOrEmpty(entry.key)) continue;
                    if (_npcLookup.ContainsKey(entry.key))
                    {
                        Debug.LogWarning($"[CinematicSequencer] Duplicate NPC key: '{entry.key}'");
                        continue;
                    }
                    _npcLookup[entry.key] = entry.npc;
                }
            }
        }

        #endregion

        #region Validate

        /// <summary>
        /// Validates that all key-based steps in the given sequence can be resolved
        /// against the current registries. Returns true if all keys resolve, false otherwise.
        /// Logs warnings for each unresolved key.
        /// </summary>
        public bool Validate(CinematicSequence sequence)
        {
            if (sequence == null)
            {
                Debug.LogWarning("[CinematicSequencer] Validate called with null sequence.");
                return false;
            }

            // Ensure lookups are built
            if (_dialogueLookup == null || _cameraPathLookup == null || _npcLookup == null)
                BuildLookups();

            if (sequence.steps == null || sequence.steps.Length == 0)
                return true;

            bool valid = true;

            for (int i = 0; i < sequence.steps.Length; i++)
            {
                CinematicStep step = sequence.steps[i];
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
                        if (!string.IsNullOrEmpty(key) && !_cameraPathLookup.ContainsKey(key))
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

                    // Step types that don't require key resolution
                    default:
                        break;
                }
            }

            return valid;
        }

        #endregion

        #region Playback State

        private int _startIndex;
        private Coroutine _runCoroutine;

        #endregion

        #region CompletionFlag

        private class CompletionFlag
        {
            public bool Done;
        }

        #endregion

        #region Public Playback API

        /// <summary>
        /// Starts playing a cinematic sequence from the beginning.
        /// Stops any currently running sequence first.
        /// </summary>
        public void Play(CinematicSequence sequence)
        {
            if (_runCoroutine != null)
            {
                StopCoroutine(_runCoroutine);
                _runCoroutine = null;
            }

            BuildLookups();
            Validate(sequence);

            IsPlaying = true;
            IsPaused = false;
            _startIndex = 0;

            _runCoroutine = StartCoroutine(RunSequence(sequence));
        }

        /// <summary>
        /// Pauses the currently playing sequence.
        /// </summary>
        public void Pause()
        {
            if (IsPlaying)
                IsPaused = true;
        }

        /// <summary>
        /// Resumes a paused sequence.
        /// </summary>
        public void Resume()
        {
            if (IsPaused)
                IsPaused = false;
        }

        /// <summary>
        /// Immediately stops the current sequence, applies terminal states, and fires OnSequenceComplete.
        /// </summary>
        public void Skip()
        {
            if (_runCoroutine != null) StopCoroutine(_runCoroutine);
            _runCoroutine = null;

            // Clean up listeners that ExecuteStep may have added
            if (_dialogueManager != null) _dialogueManager.OnDialogueComplete.RemoveAllListeners();
            if (_cinematicCamera != null) _cinematicCamera.OnWaypointReached.RemoveAllListeners();

            // Apply terminal states
            if (_screenEffects != null)
                _screenEffects.ResetAll();
            if (_dialogueManager != null)
                _dialogueManager.Hide();
            if (_playerMovement != null)
                _playerMovement.enabled = true;

            IsPlaying = false;
            IsPaused = false;

            OnSequenceComplete?.Invoke();
        }

        /// <summary>
        /// Stops the current sequence and restarts from the given step index.
        /// </summary>
        public void SkipToStep(CinematicSequence sequence, int stepIndex)
        {
            if (_runCoroutine != null) StopCoroutine(_runCoroutine);
            _runCoroutine = null;

            _startIndex = stepIndex;
            _runCoroutine = StartCoroutine(RunSequence(sequence));
        }

        #endregion

        #region RunSequence Coroutine

        private IEnumerator RunSequence(CinematicSequence sequence)
        {
            if (sequence == null || sequence.steps == null || sequence.steps.Length == 0)
            {
                IsPlaying = false;
                OnSequenceComplete?.Invoke();
                yield break;
            }

            for (int i = _startIndex; i < sequence.steps.Length; i++)
            {
                // Wait while paused
                while (IsPaused)
                    yield return null;

                CinematicStep step = sequence.steps[i];
                CompletionFlag flag = new CompletionFlag();

                ExecuteStep(step, flag);

                if (step.waitForCompletion)
                {
                    switch (step.type)
                    {
                        // Wait type: manual elapsed loop so Pause can interrupt
                        case CinematicStepType.Wait:
                            float waitElapsed = 0f;
                            while (waitElapsed < step.duration)
                            {
                                while (IsPaused) yield return null;
                                waitElapsed += Time.unscaledDeltaTime;
                                yield return null;
                            }
                            break;

                        // Callback-based types: wait for flag with timeout
                        case CinematicStepType.Fade:
                        case CinematicStepType.Shake:
                        case CinematicStepType.Letterbox:
                        case CinematicStepType.Dialogue:
                        case CinematicStepType.CameraMove:
                        case CinematicStepType.OrbitMove:
                            float timeout = step.duration + 5f;
                            float elapsed = 0f;
                            while (!flag.Done && elapsed < timeout)
                            {
                                while (IsPaused) yield return null;
                                elapsed += Time.unscaledDeltaTime;
                                yield return null;
                            }
                            if (!flag.Done)
                            {
                                Debug.LogWarning($"[CinematicSequencer] Step {i} ({step.type}) timed out after {timeout:F1}s — auto-advancing.");
                            }
                            break;

                        // Duration-based types: use WaitForSecondsRealtime(duration)
                        case CinematicStepType.PlaySFX:
                        case CinematicStepType.PlayMusic:
                        case CinematicStepType.StopMusic:
                            yield return new WaitForSecondsRealtime(step.duration);
                            break;

                        // Instant types: no wait
                        default:
                            break;
                    }
                }
            }

            IsPlaying = false;
            _runCoroutine = null;
            OnSequenceComplete?.Invoke();
        }

        #endregion

        #region ExecuteStep

        /// <summary>
        /// Dispatches a single step to the appropriate subsystem.
        /// For callback-based steps, wires the completion callback to set flag.Done.
        /// </summary>
        private void ExecuteStep(CinematicStep step, CompletionFlag flag)
        {
            switch (step.type)
            {
                case CinematicStepType.Fade:
                    if (_screenEffects == null)
                    {
                        Debug.LogWarning("[CinematicSequencer] ScreenEffects is null — skipping Fade step.");
                        flag.Done = true;
                        break;
                    }
                    if (step.floatParam > 0)
                        _screenEffects.FadeToBlack(step.duration, () => flag.Done = true);
                    else
                        _screenEffects.FadeFromBlack(step.duration, () => flag.Done = true);
                    break;

                case CinematicStepType.Shake:
                    if (_screenEffects == null)
                    {
                        Debug.LogWarning("[CinematicSequencer] ScreenEffects is null — skipping Shake step.");
                        flag.Done = true;
                        break;
                    }
                    _screenEffects.ScreenShake(step.floatParam, step.duration, () => flag.Done = true);
                    break;

                case CinematicStepType.Letterbox:
                    if (_screenEffects == null)
                    {
                        Debug.LogWarning("[CinematicSequencer] ScreenEffects is null — skipping Letterbox step.");
                        flag.Done = true;
                        break;
                    }
                    if (step.floatParam > 0)
                        _screenEffects.ShowLetterbox(step.floatParam, step.duration, () => flag.Done = true);
                    else
                        _screenEffects.HideLetterbox(step.duration, () => flag.Done = true);
                    break;

                case CinematicStepType.ObjectivePopup:
                    if (_screenEffects == null)
                    {
                        Debug.LogWarning("[CinematicSequencer] ScreenEffects is null — skipping ObjectivePopup step.");
                        break;
                    }
                    _screenEffects.ShowObjective(step.stringParam);
                    break;

                case CinematicStepType.Dialogue:
                    if (_dialogueManager == null)
                    {
                        Debug.LogWarning("[CinematicSequencer] DialogueManager is null — skipping Dialogue step.");
                        flag.Done = true;
                        break;
                    }
                    if (_dialogueLookup != null && _dialogueLookup.TryGetValue(step.stringParam ?? "", out DialogueData dialogueData))
                    {
                        _dialogueManager.OnDialogueComplete.AddListener(OnDialogueCompletedForFlag);
                        _dialogueManager.StartDialogue(dialogueData);

                        void OnDialogueCompletedForFlag()
                        {
                            flag.Done = true;
                            _dialogueManager.OnDialogueComplete.RemoveListener(OnDialogueCompletedForFlag);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[CinematicSequencer] Dialogue key '{step.stringParam}' not found — skipping.");
                        flag.Done = true;
                    }
                    break;

                case CinematicStepType.CameraMove:
                    if (_cinematicCamera == null)
                    {
                        Debug.LogWarning("[CinematicSequencer] CinematicCamera is null — skipping CameraMove step.");
                        flag.Done = true;
                        break;
                    }
                    if (!string.IsNullOrEmpty(step.stringParam))
                    {
                        if (_cameraPathLookup != null && _cameraPathLookup.TryGetValue(step.stringParam, out CameraPath path))
                        {
                            _cinematicCamera.OnWaypointReached.AddListener(OnWaypointForFlag);
                            _cinematicCamera.PlayPath(path);

                            void OnWaypointForFlag()
                            {
                                flag.Done = true;
                                _cinematicCamera.OnWaypointReached.RemoveListener(OnWaypointForFlag);
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[CinematicSequencer] CameraPath key '{step.stringParam}' not found — skipping.");
                            flag.Done = true;
                        }
                    }
                    else
                    {
                        _cinematicCamera.OnWaypointReached.AddListener(OnWaypointIndexForFlag);
                        _cinematicCamera.MoveToWaypoint(step.intParam);

                        void OnWaypointIndexForFlag()
                        {
                            flag.Done = true;
                            _cinematicCamera.OnWaypointReached.RemoveListener(OnWaypointIndexForFlag);
                        }
                    }
                    break;

                case CinematicStepType.OrbitMove:
                    if (_cinematicCamera == null)
                    {
                        Debug.LogWarning("[CinematicSequencer] CinematicCamera is null — skipping OrbitMove step.");
                        flag.Done = true;
                        break;
                    }
                    {
                        // stringParam encodes orbit center and height as "centerX,centerY,centerZ,height,startAngleDeg"
                        // floatParam = radius, intParam = totalDegrees, duration = total seconds
                        var parts = (step.stringParam ?? "0,0,0,0,0").Split(',');
                        float cx = parts.Length > 0 ? float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture) : 0f;
                        float cy = parts.Length > 1 ? float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : 0f;
                        float cz = parts.Length > 2 ? float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture) : 0f;
                        float orbitHeight    = parts.Length > 3 ? float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture) : 0f;
                        float startAngleDeg  = parts.Length > 4 ? float.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture) : 0f;

                        Vector3 orbitCenter = new Vector3(cx, cy, cz);
                        float orbitRadius   = step.floatParam;
                        int totalDegrees    = step.intParam;

                        _cinematicCamera.OnWaypointReached.AddListener(OnOrbitCompleteForFlag);
                        _cinematicCamera.OrbitAround(orbitCenter, orbitRadius, orbitHeight, startAngleDeg, totalDegrees, step.duration);

                        void OnOrbitCompleteForFlag()
                        {
                            flag.Done = true;
                            _cinematicCamera.OnWaypointReached.RemoveListener(OnOrbitCompleteForFlag);
                        }
                    }
                    break;

                case CinematicStepType.Wait:
                    // Wait is handled by RunSequence via WaitForSecondsRealtime
                    break;

                case CinematicStepType.PlaySFX:
                    if (_audioManager == null)
                    {
                        Debug.LogWarning("[CinematicSequencer] AudioManager is null — skipping PlaySFX step.");
                        break;
                    }
                    _audioManager.PlaySFXByKey(step.stringParam ?? "", step.floatParam > 0 ? step.floatParam : 1f);
                    break;

                case CinematicStepType.PlayMusic:
                    if (_audioManager == null)
                    {
                        Debug.LogWarning("[CinematicSequencer] AudioManager is null — skipping PlayMusic step.");
                        break;
                    }
                    _audioManager.PlayMusicByKey(step.stringParam ?? "", step.duration);
                    break;

                case CinematicStepType.StopMusic:
                    if (_audioManager == null)
                    {
                        Debug.LogWarning("[CinematicSequencer] AudioManager is null — skipping StopMusic step.");
                        break;
                    }
                    _audioManager.StopMusic(step.duration);
                    break;

                case CinematicStepType.EnablePlayerControl:
                    if (_playerMovement == null)
                    {
                        Debug.LogWarning("[CinematicSequencer] PlayerMovement is null — skipping EnablePlayerControl step.");
                        break;
                    }
                    _playerMovement.enabled = true;
                    break;

                case CinematicStepType.DisablePlayerControl:
                    if (_playerMovement == null)
                    {
                        Debug.LogWarning("[CinematicSequencer] PlayerMovement is null — skipping DisablePlayerControl step.");
                        break;
                    }
                    _playerMovement.enabled = false;
                    break;

                case CinematicStepType.ActivateNPC:
                    if (_npcLookup != null && _npcLookup.TryGetValue(step.stringParam ?? "", out NPCController npcToActivate))
                    {
                        npcToActivate.Activate();
                    }
                    else
                    {
                        Debug.LogWarning($"[CinematicSequencer] NPC key '{step.stringParam}' not found — skipping ActivateNPC.");
                    }
                    break;

                case CinematicStepType.DeactivateNPC:
                    if (_npcLookup != null && _npcLookup.TryGetValue(step.stringParam ?? "", out NPCController npcToDeactivate))
                    {
                        npcToDeactivate.Deactivate();
                    }
                    else
                    {
                        Debug.LogWarning($"[CinematicSequencer] NPC key '{step.stringParam}' not found — skipping DeactivateNPC.");
                    }
                    break;

                case CinematicStepType.MissionStart:
                    if (_missionManager == null)
                    {
                        Debug.LogWarning("[CinematicSequencer] MissionManager is null — skipping MissionStart step.");
                        break;
                    }
                    string[] missionParts = (step.stringParam ?? "").Split('|');
                    string missionName = missionParts.Length > 0 ? missionParts[0] : "";
                    string objectiveText = missionParts.Length > 1 ? missionParts[1] : "";
                    _missionManager.StartMission(missionName, objectiveText);
                    break;

                case CinematicStepType.MissionComplete:
                    if (_missionManager == null)
                    {
                        Debug.LogWarning("[CinematicSequencer] MissionManager is null — skipping MissionComplete step.");
                        break;
                    }
                    _missionManager.CompleteMission();
                    break;

                case CinematicStepType.SetLighting:
                    Debug.Log($"[CinematicSequencer] SetLighting step (future INT-009 integration): stringParam='{step.stringParam}', floatParam={step.floatParam}");
                    break;

                default:
                    Debug.LogWarning($"[CinematicSequencer] Unknown step type: {step.type}");
                    break;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            BuildLookups();
        }

        #endregion
    }
}
