using UnityEngine;
using UnityEngine.Events;
using FarmSimVR.Core.GameState;
using FarmSimVR.MonoBehaviours.Audio;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Singleton that tracks the current mission lifecycle: start, update objective, complete.
    /// Delegates visual feedback to ScreenEffects and audio to SimpleAudioManager.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        [Header("Dependencies")]
        [SerializeField] private ScreenEffects screenEffects;
        [SerializeField] private SimpleAudioManager audioManager;

        [Header("Events")]
        public UnityEvent OnMissionStarted;
        public UnityEvent OnMissionCompleted;

        // Internal state
        private MissionState currentState = MissionState.None;
        private string currentMissionName;
        private string currentObjectiveText;

        // Public properties
        public MissionState CurrentMissionState => currentState;
        public string CurrentMissionName => currentMissionName;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Begins a new mission. If a mission is already active, silently resets state first.
        /// </summary>
        public void StartMission(string name, string objectiveText)
        {
            if (currentState == MissionState.Active)
                currentState = MissionState.None;

            currentMissionName = string.IsNullOrEmpty(name) ? "Unnamed Mission" : name;
            currentObjectiveText = string.IsNullOrEmpty(objectiveText) ? "No objective" : objectiveText;
            currentState = MissionState.Active;

            GetScreenEffects()?.ShowObjective(currentObjectiveText);
            OnMissionStarted?.Invoke();
        }

        /// <summary>
        /// Updates the objective text for the currently active mission.
        /// </summary>
        public void UpdateObjective(string newText)
        {
            if (currentState != MissionState.Active)
            {
                Debug.LogWarning("[MissionManager] UpdateObjective called but no mission is active.");
                return;
            }

            currentObjectiveText = string.IsNullOrEmpty(newText) ? "No objective" : newText;
            GetScreenEffects()?.ShowObjective(currentObjectiveText);
        }

        /// <summary>
        /// Completes the current mission, showing a banner and playing a sound.
        /// </summary>
        public void CompleteMission()
        {
            if (currentState == MissionState.None)
            {
                Debug.LogWarning("[MissionManager] CompleteMission called but no mission is active.");
                return;
            }

            currentState = MissionState.Complete;

            GetScreenEffects()?.ShowMissionPassed(currentMissionName);
            GetAudioManager()?.PlaySFXByKey("mission_complete");
            OnMissionCompleted?.Invoke();

            currentState = MissionState.None;
            currentMissionName = null;
            currentObjectiveText = null;
        }

        private ScreenEffects GetScreenEffects()
        {
            if (screenEffects != null)
                return screenEffects;
            return ScreenEffects.Instance;
        }

        private SimpleAudioManager GetAudioManager()
        {
            if (audioManager != null)
                return audioManager;
            return SimpleAudioManager.Instance;
        }
    }
}
