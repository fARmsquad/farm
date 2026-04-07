using System;
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
                        if (!_cameraPathLookup.ContainsKey(key ?? ""))
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

        #region Unity Lifecycle

        private void Awake()
        {
            BuildLookups();
        }

        #endregion
    }
}
