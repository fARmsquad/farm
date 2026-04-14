using System.Collections;
using FarmSimVR.Core.Story;
using FarmSimVR.Core.Tutorial;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours.ChickenGame
{
    /// <summary>
    /// Core game loop for the chicken-catching minigame.
    /// Phase 1 — Chase: press E when in range to catch the chicken.
    /// Phase 2 — Hold:  left-click continuously to maintain grip; walk to the coop to win.
    ///                  Stop clicking and the chicken escapes back to Phase 1.
    /// </summary>
    public class ChickenGameManager : MonoBehaviour
    {
        private const string DefaultChickenObjective = "Catch the chicken and drop it in the coop.";
        private const string DefaultArenaPresetId = "tutorial_pen_small";
        private const string DefaultGuidanceLevel = "high";

        [Header("References")]
        [SerializeField] public ChickenAI chicken;
        [SerializeField] public Transform player;
        [SerializeField] private Transform _coopTransform;
        [SerializeField] private ChickenGameUI _ui;

        private ChickenGameSceneAudio _sceneAudio;

        [Header("Timer")]
        [SerializeField] public float timeLimit = 45f;

        [Header("Grip")]
        [SerializeField] private float gripDrainRate   = 0.08f;  // Grip lost per second — empties in ~12s without clicking
        [SerializeField] private float gripClickRefill = 0.30f;  // Grip gained per click — ~4 clicks to refill from empty

        [Header("Coop")]
        [SerializeField] private float coopDropRadius = 1.5f;

        [Header("Win celebration")]
        [SerializeField] private float winOverheadHeight   = 16f;
        [SerializeField] private float winSwoopDuration    = 1.15f;
        [SerializeField] private float winHoldAfterSwoop   = 0.4f;
        [SerializeField] private AnimationCurve winSwoopEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private readonly PackageChickenChaseMissionService _packageMission = new();

        private ChickenPlayerController _playerController;
        private float _timeRemaining;
        private bool  _gameOver;
        private bool  _isHoldingChicken;
        private bool  _usePackageMode;
        private bool  _chickenDefaultsCaptured;
        private float _gripMeter;
        private Vector3    _playerStartPos;
        private Quaternion _playerStartRot;
        private Vector3    _chickenStartPos;
        private Quaternion _chickenStartRot;
        private float _defaultChickenArenaRadius;
        private float _defaultChickenFleeRadius;
        private float _defaultChickenPanicRadius;
        private Keyboard _keyboard;
        private Mouse    _mouse;

        private bool _winCelebrationActive;
        private bool _cameraDetached;
        private Transform   _camStoredParent;
        private Vector3     _camStoredLocalPos;
        private Quaternion  _camStoredLocalRot;

        private void Awake()
        {
            TryGetComponent(out _sceneAudio);
            if (winSwoopEase == null || winSwoopEase.length == 0)
                winSwoopEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            CaptureChickenDefaults();
        }

        private void Start()
        {
            _keyboard = Keyboard.current;
            _mouse    = Mouse.current;

            if (player != null)
            {
                _playerController = player.GetComponent<ChickenPlayerController>();
                _playerStartPos   = player.position;
                _playerStartRot   = player.rotation;
            }

            if (chicken != null)
            {
                _chickenStartPos = chicken.transform.position;
                _chickenStartRot = chicken.transform.rotation;
            }

            _timeRemaining = timeLimit;
        }

        private void Update()
        {
            if (_gameOver)
            {
                if (_keyboard != null && _keyboard.spaceKey.wasPressedThisFrame)
                    RestartGame();
                return;
            }

            if (_winCelebrationActive)
                return;

            _timeRemaining -= Time.deltaTime;

            if (_isHoldingChicken)
            {
                HandleGripMechanic();
                CheckCoopDrop();
            }
            else
            {
                HandleCatchInteraction();
            }

            if (_timeRemaining <= 0f)
                EndGame(won: false);
        }

        // ── Public state for UI ──────────────────────────────────────────────

        /// <summary>Seconds remaining on the clock, clamped to zero.</summary>
        public float TimeRemaining => Mathf.Max(0f, _timeRemaining);

        /// <summary>True once the game has ended (win or lose).</summary>
        public bool IsGameOver => _gameOver;

        /// <summary>True while the overhead win sequence is playing (before <see cref="IsGameOver"/>).</summary>
        public bool IsWinCelebration => _winCelebrationActive;

        /// <summary>True if the last game ended with the chicken dropped in the coop.</summary>
        public bool IsWon { get; private set; }

        /// <summary>True while the player is holding the chicken.</summary>
        public bool IsHoldingChicken => _isHoldingChicken;

        public string CurrentObjectiveText => _usePackageMode
            ? _packageMission.CurrentObjective
            : DefaultChickenObjective;
        public int RequiredCaptureCount => _usePackageMode ? _packageMission.RequiredCaptureCount : 1;
        public int CapturedCount => _usePackageMode ? _packageMission.CapturedCount : (IsWon ? 1 : 0);
        public int ConfiguredChickenCount => _usePackageMode ? _packageMission.ConfiguredChickenCount : 1;
        public string GuidanceLevel => _usePackageMode ? _packageMission.GuidanceLevel : DefaultGuidanceLevel;
        public string ArenaPresetId => _usePackageMode ? _packageMission.ArenaPresetId : DefaultArenaPresetId;

        /// <summary>Current grip level, 0–1. Drains without clicking, refills on click.</summary>
        public float GripFraction => _gripMeter;

        /// <summary>True when the chicken is currently stunned.</summary>
        public bool IsChickenStunned => chicken != null && chicken.IsStunned;

        public void ApplyPackageConfig(StoryMinigameConfigSnapshot minigame)
        {
            if (minigame == null || !string.Equals(minigame.AdapterId, "tutorial.chicken_chase", System.StringComparison.Ordinal))
                return;

            CaptureChickenDefaults();

            var requiredCaptureCount = ResolveRequiredCaptureCount(minigame);
            var chickenCount = ResolveChickenCount(minigame, requiredCaptureCount);
            var arenaPresetId = ResolveStringParameter(minigame, "arenaPresetId", DefaultArenaPresetId);
            var guidanceLevel = ResolveStringParameter(minigame, "guidanceLevel", DefaultGuidanceLevel);

            _packageMission.Configure(
                minigame.ObjectiveText,
                requiredCaptureCount,
                chickenCount,
                arenaPresetId,
                guidanceLevel);
            _usePackageMode = true;

            if (minigame.TimeLimitSeconds > 0f)
                timeLimit = minigame.TimeLimitSeconds;

            _timeRemaining = timeLimit;
            ApplyArenaPreset(_packageMission.ArenaPresetId);
        }

        /// <summary>True if the player is currently within catch range of the chicken.</summary>
        public bool IsInCatchRange()
        {
            if (chicken == null || player == null) return false;
            float dx = player.position.x - chicken.transform.position.x;
            float dz = player.position.z - chicken.transform.position.z;
            return (dx * dx + dz * dz) <= chicken.catchRadius * chicken.catchRadius;
        }

        /// <summary>True when the player is holding the chicken and is close enough to the coop to drop it.</summary>
        public bool IsNearCoop()
        {
            if (!_isHoldingChicken || _coopTransform == null || player == null) return false;
            Vector3 delta = player.position - _coopTransform.position;
            delta.y = 0f;
            return delta.sqrMagnitude <= coopDropRadius * coopDropRadius;
        }

        // ── Private game logic ───────────────────────────────────────────────

        private void HandleCatchInteraction()
        {
            if (chicken == null || player == null || _keyboard == null) return;

            if (_keyboard.eKey.wasPressedThisFrame)
            {
                if (IsInCatchRange())
                    CatchChicken();
                else if (_playerController != null)
                    _playerController.TriggerLungeMiss();
            }
        }

        private void HandleGripMechanic()
        {
            _gripMeter -= gripDrainRate * Time.deltaTime;

            if (_mouse != null && _mouse.leftButton.wasPressedThisFrame)
                _gripMeter = Mathf.Min(1f, _gripMeter + gripClickRefill);

            if (_gripMeter <= 0f)
                ReleaseChicken();
        }

        private void CheckCoopDrop()
        {
            if (!IsNearCoop())
                return;

            _winCelebrationActive = true;
            StartCoroutine(WinCelebrationSequence());
        }

        private IEnumerator WinCelebrationSequence()
        {
            if (_isHoldingChicken && chicken != null)
            {
                _isHoldingChicken = false;
                _gripMeter        = 0f;
                chicken.Drop();
                chicken.enabled = false;
            }

            if (_playerController != null)
                _playerController.SetCelebrationFrozen(true);

            if (_sceneAudio != null)
                _sceneAudio.PlayVictory();

            Camera fpCam = _playerController != null ? _playerController.fpCamera : null;
            if (fpCam != null && _coopTransform != null)
            {
                Transform camTf = fpCam.transform;
                _camStoredParent     = camTf.parent;
                _camStoredLocalPos   = camTf.localPosition;
                _camStoredLocalRot   = camTf.localRotation;
                _cameraDetached      = true;

                Vector3 startWorldPos = camTf.position;
                Quaternion startWorldRot = camTf.rotation;

                Vector3 focus = _coopTransform.position + Vector3.up * 0.6f;
                Vector3 endPos = focus + Vector3.up * winOverheadHeight;
                Vector3 lookDir = focus - endPos;
                Quaternion endRot = lookDir.sqrMagnitude > 0.0001f
                    ? Quaternion.LookRotation(lookDir.normalized, Vector3.up)
                    : Quaternion.Euler(90f, 0f, 0f);

                camTf.SetParent(null);

                float swoop = Mathf.Max(0.05f, winSwoopDuration);
                float elapsed = 0f;
                while (elapsed < swoop)
                {
                    elapsed += Time.deltaTime;
                    float u = winSwoopEase.Evaluate(Mathf.Clamp01(elapsed / swoop));
                    camTf.position = Vector3.Lerp(startWorldPos, endPos, u);
                    camTf.rotation = Quaternion.Slerp(startWorldRot, endRot, u);
                    yield return null;
                }

                camTf.SetPositionAndRotation(endPos, endRot);
            }

            if (_ui != null)
                yield return StartCoroutine(_ui.PlayVictoryCatchPhrase());
            else
                yield return new WaitForSeconds(0.55f);

            float hold = Mathf.Max(0f, winHoldAfterSwoop);
            if (hold > 0f)
                yield return new WaitForSeconds(hold);

            RestoreCelebrationCamera();
            _winCelebrationActive = false;

            ResolveSuccessfulCaptureOutcome();
        }

        private void RestoreCelebrationCamera()
        {
            if (!_cameraDetached || _playerController == null || _playerController.fpCamera == null)
                return;

            Transform camTf = _playerController.fpCamera.transform;
            if (_camStoredParent != null)
            {
                camTf.SetParent(_camStoredParent);
                camTf.localPosition = _camStoredLocalPos;
                camTf.localRotation = _camStoredLocalRot;
            }

            _cameraDetached = false;
            _camStoredParent = null;
        }

        private void CatchChicken()
        {
            _isHoldingChicken = true;
            _gripMeter        = 1f;
            chicken.Catch(player);
            if (_sceneAudio != null)
                _sceneAudio.PlayGrabLine();
        }

        private void ReleaseChicken()
        {
            _isHoldingChicken = false;
            _gripMeter        = 0f;
            chicken.Escape();
            if (_sceneAudio != null)
                _sceneAudio.PlayDropLine();
        }

        private void EndGame(bool won)
        {
            bool wasHolding = _isHoldingChicken;

            if (_isHoldingChicken)
            {
                _isHoldingChicken = false;
                _gripMeter        = 0f;
                chicken.Drop();
            }

            _gameOver = true;
            IsWon     = won;

            if (!won && wasHolding && _sceneAudio != null)
                _sceneAudio.PlayDropLine();

            if (chicken != null)
            {
                chicken.enabled = false;
                if (won) chicken.gameObject.SetActive(false);
            }

            Debug.Log(won
                ? $"[ChickenGame] Dropped in coop after {Mathf.CeilToInt(timeLimit - _timeRemaining)}s!"
                : "[ChickenGame] Time's up!");
        }

        private void RestartGame()
        {
            RestoreCelebrationCamera();
            _winCelebrationActive = false;

            if (_usePackageMode)
            {
                _packageMission.ResetProgress();
                ApplyArenaPreset(_packageMission.ArenaPresetId);
            }

            _gameOver         = false;
            IsWon             = false;
            _isHoldingChicken = false;
            _gripMeter        = 0f;
            _timeRemaining    = timeLimit;

            if (player != null)
            {
                player.position = _playerStartPos;
                player.rotation = _playerStartRot;
            }

            if (chicken != null)
            {
                chicken.gameObject.SetActive(true);
                chicken.transform.position = _chickenStartPos;
                chicken.transform.rotation = _chickenStartRot;
                chicken.enabled            = true;
                chicken.ResetState();
            }

            if (_playerController != null)
                _playerController.ResetState();

            if (_ui != null)
                _ui.ResetUI();
        }

        private void ResolveSuccessfulCaptureOutcome()
        {
            if (_usePackageMode && !_packageMission.RegisterSuccessfulCapture())
            {
                RearmConfiguredCaptureLoop();
                return;
            }

            EndGame(won: true);
        }

        private void RearmConfiguredCaptureLoop()
        {
            if (player != null)
            {
                player.position = _playerStartPos;
                player.rotation = _playerStartRot;
            }

            if (chicken != null)
            {
                chicken.gameObject.SetActive(true);
                chicken.transform.position = _chickenStartPos;
                chicken.transform.rotation = _chickenStartRot;
                chicken.enabled = true;
                chicken.ResetState();
            }

            if (_playerController != null)
            {
                _playerController.SetCelebrationFrozen(false);
                _playerController.ResetState();
                _playerController.SetCursorLocked(true);
            }
        }

        private void CaptureChickenDefaults()
        {
            if (_chickenDefaultsCaptured || chicken == null)
                return;

            _defaultChickenArenaRadius = chicken.arenaRadius;
            _defaultChickenFleeRadius = chicken.fleeRadius;
            _defaultChickenPanicRadius = chicken.panicRadius;
            _chickenDefaultsCaptured = true;
        }

        private void ApplyArenaPreset(string arenaPresetId)
        {
            if (chicken == null)
                return;

            CaptureChickenDefaults();
            if (string.Equals(arenaPresetId, "tutorial_pen_medium", System.StringComparison.Ordinal))
            {
                chicken.arenaRadius = _defaultChickenArenaRadius + 3f;
                chicken.fleeRadius = _defaultChickenFleeRadius + 1f;
                chicken.panicRadius = _defaultChickenPanicRadius + 0.5f;
                return;
            }

            chicken.arenaRadius = _defaultChickenArenaRadius;
            chicken.fleeRadius = _defaultChickenFleeRadius;
            chicken.panicRadius = _defaultChickenPanicRadius;
        }

        private static int ResolveRequiredCaptureCount(StoryMinigameConfigSnapshot minigame)
        {
            var requiredCaptureCount = minigame.RequiredCount < 1 ? 1 : minigame.RequiredCount;
            if (minigame.TryGetIntParameter("targetCaptureCount", out var configuredCaptureCount) && configuredCaptureCount > 0)
                requiredCaptureCount = configuredCaptureCount;

            return requiredCaptureCount;
        }

        private static int ResolveChickenCount(StoryMinigameConfigSnapshot minigame, int requiredCaptureCount)
        {
            if (!minigame.TryGetIntParameter("chickenCount", out var chickenCount))
                return requiredCaptureCount;

            return chickenCount < requiredCaptureCount ? requiredCaptureCount : chickenCount;
        }

        private static string ResolveStringParameter(StoryMinigameConfigSnapshot minigame, string parameterName, string fallback)
        {
            return minigame.TryGetStringParameter(parameterName, out var value)
                ? value
                : fallback;
        }
    }
}
