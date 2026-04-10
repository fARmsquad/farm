using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FarmSimVR.MonoBehaviours.ChickenGame
{
    /// <summary>
    /// Reads state from ChickenGameManager each frame and drives all HUD elements.
    /// No game logic lives here.
    /// </summary>
    public class ChickenGameUI : MonoBehaviour
    {
        [Header("Game References")]
        [SerializeField] private ChickenGameManager _manager;

        [Header("HUD — Timer")]
        [SerializeField] private GameObject      _timerContainer;
        [SerializeField] private TextMeshProUGUI _timerText;

        [Header("HUD — Catch Prompt")]
        [SerializeField] private TextMeshProUGUI _catchPromptText;

        [Header("HUD — Hint Bar")]
        [SerializeField] private GameObject      _hintBar;
        [SerializeField] private TextMeshProUGUI _hintText;

        [Header("HUD — Grip Meter")]
        [SerializeField] private GameObject _gripMeterContainer;
        [SerializeField] private Image      _gripMeterFill;

        [Header("Result Screen")]
        [SerializeField] private GameObject      _dimOverlay;
        [SerializeField] private GameObject      _resultPanel;
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private TextMeshProUGUI _resultSubText;

        private static readonly Color ColorNormal  = Color.white;
        private static readonly Color ColorUrgent  = new(1f, 0.25f, 0.25f);
        private static readonly Color ColorWin     = new(0.2f, 0.9f, 0.2f);
        private static readonly Color ColorWarning = new(1f, 0.75f, 0.1f);
        private static readonly Color ColorMuted   = new(0.75f, 0.75f, 0.75f, 1f);

        private const string HintChase   = "<color=#FFFFFF>WASD</color>  Move     <color=#FFFFFF>Shift</color>  Sprint     <color=#FFFFFF>E</color>  Catch  <color=#888888>(get close)</color>";
        private const string HintHolding = "<color=#FFFFFF>Left Click</color>  Maintain grip     <color=#FFFFFF>WASD</color>  Walk to the coop";

        private bool _wasGameOver;

        private void Update()
        {
            if (_manager == null) return;

            UpdateTimer();
            UpdateCatchPrompt();
            UpdateHintBar();
            UpdateGripMeter();
            UpdateResultPanel();
        }

        /// <summary>Resets all HUD elements to play state.</summary>
        public void ResetUI()
        {
            if (_timerContainer     != null) _timerContainer.SetActive(true);
            if (_hintBar            != null) _hintBar.SetActive(true);
            if (_gripMeterContainer != null) _gripMeterContainer.SetActive(false);
            if (_dimOverlay         != null) _dimOverlay.SetActive(false);
            if (_resultPanel        != null) _resultPanel.SetActive(false);
            if (_catchPromptText    != null) _catchPromptText.gameObject.SetActive(false);
            _wasGameOver = false;
        }

        private void UpdateTimer()
        {
            if (_timerContainer == null || _timerText == null) return;
            bool active = !_manager.IsGameOver;
            _timerContainer.SetActive(active);
            if (!active) return;

            int secs         = Mathf.CeilToInt(_manager.TimeRemaining);
            _timerText.text  = $"{secs}";
            _timerText.color = _manager.TimeRemaining <= 10f ? ColorUrgent : ColorNormal;
        }

        private void UpdateCatchPrompt()
        {
            if (_catchPromptText == null || _manager.IsGameOver) return;

            if (_manager.IsHoldingChicken)
            {
                // Only show the prominent prompt when near the coop — the hint bar handles the rest
                bool nearCoop = _manager.IsNearCoop();
                _catchPromptText.gameObject.SetActive(nearCoop);
                if (nearCoop)
                {
                    _catchPromptText.text  = "Drop in the coop!";
                    _catchPromptText.color = ColorWin;
                }
                return;
            }

            bool inRange = _manager.IsInCatchRange();
            _catchPromptText.gameObject.SetActive(inRange);
            if (!inRange) return;

            _catchPromptText.text  = _manager.IsChickenStunned ? "It's stunned!  Press E!" : "Press E to catch!";
            _catchPromptText.color = ColorWarning;
        }

        private void UpdateHintBar()
        {
            if (_hintBar == null || _hintText == null) return;
            bool show = !_manager.IsGameOver;
            _hintBar.SetActive(show);
            if (!show) return;

            _hintText.text = _manager.IsHoldingChicken ? HintHolding : HintChase;
        }

        private void UpdateGripMeter()
        {
            if (_gripMeterContainer == null) return;
            bool holding = _manager.IsHoldingChicken && !_manager.IsGameOver;
            _gripMeterContainer.SetActive(holding);
            if (!holding || _gripMeterFill == null) return;

            float grip = _manager.GripFraction;

            // Drive width via anchorMax so the rect physically shrinks left-to-right in real time.
            // This works regardless of Image type or sprite assignment.
            RectTransform fillRect = _gripMeterFill.rectTransform;
            fillRect.anchorMax = new Vector2(grip, fillRect.anchorMax.y);

            _gripMeterFill.color = grip > 0.5f  ? ColorWin
                                 : grip > 0.25f ? ColorWarning
                                 : ColorUrgent;
        }

        private void UpdateResultPanel()
        {
            if (_resultPanel == null) return;
            bool gameOver = _manager.IsGameOver;
            _resultPanel.SetActive(gameOver);
            if (_dimOverlay != null) _dimOverlay.SetActive(gameOver);

            if (gameOver && !_wasGameOver)
            {
                if (_manager.IsWon)
                {
                    int elapsed = Mathf.CeilToInt(_manager.timeLimit - _manager.TimeRemaining);
                    if (_resultText    != null) { _resultText.text = "COOP!"; _resultText.color = ColorWin; }
                    if (_resultSubText != null) _resultSubText.text = $"Dropped in <b>{elapsed}s</b>\n<size=28><color=#AAAAAA>SPACE to play again</color></size>";
                }
                else
                {
                    if (_resultText    != null) { _resultText.text = "TIME'S UP"; _resultText.color = ColorUrgent; }
                    if (_resultSubText != null) _resultSubText.text = "The chicken got away\n<size=28><color=#AAAAAA>SPACE to try again</color></size>";
                }
            }

            _wasGameOver = gameOver;
        }
    }
}
