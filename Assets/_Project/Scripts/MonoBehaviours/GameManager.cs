using System.Collections.Generic;
using FarmSimVR.Core.GameState;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours
{
    [DefaultExecutionOrder(-1000)]
    public class GameManager : MonoBehaviour
    {
        [Header("Managed Systems (initialization order)")]
        [SerializeField] private MonoBehaviour[] managedSystems;

        [Header("Player Spawn")]
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Transform playerSpawnPoint;

        [Header("Debug Controls")]
        [SerializeField] private bool allowKeyboardPauseToggle = true;

        private readonly List<IGameManagedSystem> _cachedSystems = new();
        private GameEventBus _eventBus;
        private GameStateMachine _stateMachine;
        private float _defaultTimeScale;

        public static GameManager Instance { get; private set; }

        public GameEventBus EventBus => _eventBus;
        public GameSessionState CurrentState => _stateMachine.CurrentState;
        public bool IsPaused => _stateMachine.IsPaused;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _defaultTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            _eventBus = new GameEventBus();
            _stateMachine = new GameStateMachine(_eventBus);
            CacheManagedSystems();
        }

        private void Start()
        {
            if (Instance != this)
                return;

            InitializeSession();
        }

        private void Update()
        {
            if (!allowKeyboardPauseToggle)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
                return;

            TogglePause();
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            if (_stateMachine != null && _stateMachine.IsPaused)
                ApplyPause(false);

            Instance = null;
        }

        public void InitializeSession()
        {
            if (_stateMachine.CurrentState != GameSessionState.Loading)
                return;

            PositionPlayerAtSpawnPoint();

            for (var index = 0; index < _cachedSystems.Count; index++)
            {
                var system = _cachedSystems[index];
                system.InitializeSystem();
                _eventBus.Publish(new GameSystemInitializedEvent(system.SystemName, index));
            }

            _stateMachine.CompleteLoading();
            ApplyPause(false);
        }

        public void PauseGame()
        {
            _stateMachine.Pause();
            ApplyPause(true);
        }

        public void ResumeGame()
        {
            _stateMachine.Resume();
            ApplyPause(false);
        }

        public void TogglePause()
        {
            if (_stateMachine.CurrentState == GameSessionState.Playing)
            {
                PauseGame();
                return;
            }

            if (_stateMachine.CurrentState == GameSessionState.Paused)
                ResumeGame();
        }

        public void EndGame()
        {
            _stateMachine.EnterGameOver();
            ApplyPause(true);
        }

        private void CacheManagedSystems()
        {
            _cachedSystems.Clear();
            if (managedSystems == null)
                return;

            foreach (var behaviour in managedSystems)
            {
                if (behaviour is IGameManagedSystem managedSystem)
                    _cachedSystems.Add(managedSystem);
            }
        }

        private void ApplyPause(bool isPaused)
        {
            Time.timeScale = isPaused ? 0f : _defaultTimeScale;

            foreach (var system in _cachedSystems)
                system.SetPaused(isPaused);
        }

        private void PositionPlayerAtSpawnPoint()
        {
            if (playerRoot == null || playerSpawnPoint == null)
                return;

            playerRoot.SetPositionAndRotation(
                playerSpawnPoint.position,
                playerSpawnPoint.rotation);
        }
    }
}
