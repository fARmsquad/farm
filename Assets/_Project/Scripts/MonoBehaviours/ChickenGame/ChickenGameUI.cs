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

        [Header("HUD — Prompt")]
        [SerializeField] private TextMeshProUGUI _catchPromptText;

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

        private bool _wasGameOver;

        private void Update()
        {
            if (_manager == null) return;

            UpdateTimer();
            UpdateCatchPrompt();
            UpdateGripMeter();
            UpdateResultPanel();
        }

        /// <summary>Resets all HUD elements to play state.</summary>
        public void ResetUI()
        {
            if (_timerContainer     != null) _timerContainer.SetActive(true);
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
                _catchPromptText.gameObject.SetActive(true);
                if (_manager.IsNearCoop())
                {
                    _catchPromptText.text  = "Drop in the coop!";
                    _catchPromptText.color = ColorWin;
                }
                else
                {
                    _catchPromptText.text  = "Keep clicking!  Walk to the coop";
                    _catchPromptText.color = ColorWarning;
                }
                return;
            }

            bool inRange = _manager.IsInCatchRange();
            _catchPromptText.gameObject.SetActive(inRange);
            if (!inRange) return;

            _catchPromptText.text  = _manager.IsChickenStunned ? "It's stunned!  Press E!" : "Press E to catch!";
            _catchPromptText.color = ColorWarning;
        }

        private void UpdateGripMeter()
        {
            if (_gripMeterContainer == null) return;
            bool holding = _manager.IsHoldingChicken && !_manager.IsGameOver;
            _gripMeterContainer.SetActive(holding);
            if (!holding || _gripMeterFill == null) return;

            float grip                = _manager.GripFraction;
            _gripMeterFill.fillAmount = grip;
            _gripMeterFill.color      = grip > 0.5f  ? ColorWin
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
