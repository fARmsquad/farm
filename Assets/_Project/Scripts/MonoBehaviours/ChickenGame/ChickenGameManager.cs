using System.Collections;
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

        private ChickenPlayerController _playerController;
        private float _timeRemaining;
        private bool  _gameOver;
        private bool  _isHoldingChicken;
        private float _gripMeter;
        private Vector3    _playerStartPos;
        private Quaternion _playerStartRot;
        private Vector3    _chickenStartPos;
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
                _chickenStartPos = chicken.transform.position;

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

        /// <summary>Current grip level, 0–1. Drains without clicking, refills on click.</summary>
        public float GripFraction => _gripMeter;

        /// <summary>True when the chicken is currently stunned.</summary>
        public bool IsChickenStunned => chicken != null && chicken.IsStunned;

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

            EndGame(won: true);
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
                chicken.enabled            = true;
                chicken.ResetState();
            }

            if (_playerController != null)
                _playerController.ResetState();

            if (_ui != null)
                _ui.ResetUI();
        }
    }
}
