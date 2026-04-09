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

        [Header("Timer")]
        [SerializeField] public float timeLimit = 45f;

        [Header("Grip")]
        [SerializeField] private float gripDrainRate   = 0.08f;  // Grip lost per second — empties in ~12s without clicking
        [SerializeField] private float gripClickRefill = 0.30f;  // Grip gained per click — ~4 clicks to refill from empty

        [Header("Coop")]
        [SerializeField] private float coopDropRadius = 1.5f;

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
            if (IsNearCoop())
                EndGame(won: true);
        }

        private void CatchChicken()
        {
            _isHoldingChicken = true;
            _gripMeter        = 1f;
            chicken.Catch(player);
        }

        private void ReleaseChicken()
        {
            _isHoldingChicken = false;
            _gripMeter        = 0f;
            chicken.Escape();
        }

        private void EndGame(bool won)
        {
            if (_isHoldingChicken)
            {
                _isHoldingChicken = false;
                _gripMeter        = 0f;
                chicken.Drop();
            }

            _gameOver = true;
            IsWon     = won;

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
        }
    }
}
