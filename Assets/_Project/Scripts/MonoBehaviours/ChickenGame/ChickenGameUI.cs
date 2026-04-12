using System.Collections;
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

        [Header("Win celebration phrase")]
        [SerializeField] private GameObject      _victoryPhraseRoot;
        [SerializeField] private TextMeshProUGUI _victoryCatchPhrase;
        [SerializeField] private RectTransform   _victoryCatchPhraseScaleRoot;
        [SerializeField] private float             _victoryPhraseAnimDuration = 0.55f;

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

        // Cached HUD state — avoids TMP mesh rebuilds every frame (Editor play is sensitive to this).
        private int  _lastTimerSec = -1;
        private bool _lastTimerUrgent;
        private bool? _lastHintWasHolding;

        private enum CatchPromptKind
        {
            Invalid,
            Hidden,
            DropCoop,
            CatchStunned,
            CatchNormal,
        }

        private CatchPromptKind _catchPromptKind = CatchPromptKind.Invalid;
        private float           _lastGripAnchor  = -1f;
        private int             _lastGripBucket    = -1;

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
            if (_victoryPhraseRoot  != null) _victoryPhraseRoot.SetActive(false);
            if (_catchPromptText    != null) _catchPromptText.gameObject.SetActive(false);
            _wasGameOver = false;
            _lastTimerSec        = -1;
            _lastHintWasHolding  = null;
            _catchPromptKind     = CatchPromptKind.Invalid;
            _lastGripAnchor      = -1f;
            _lastGripBucket      = -1;
        }

        /// <summary>Elastic scale-in for the victory line; driven by the manager coroutine.</summary>
        public IEnumerator PlayVictoryCatchPhrase()
        {
            if (_victoryCatchPhrase == null)
                yield break;

            if (_victoryPhraseRoot != null)
                _victoryPhraseRoot.SetActive(true);

            _victoryCatchPhrase.text = "CHICKEN CAUGHT!";
            _victoryCatchPhrase.color = ColorWin;

            Transform scaleTf = _victoryCatchPhraseScaleRoot != null
                ? _victoryCatchPhraseScaleRoot
                : _victoryCatchPhrase.transform;

            float dur = Mathf.Max(0.05f, _victoryPhraseAnimDuration);
            float t   = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                // Overshoot ease-out (elastic-ish without extra deps)
                float s = 1f + 0.22f * Mathf.Sin(u * Mathf.PI) * (1f - u);
                float k = Mathf.SmoothStep(0.15f, 1f, u) * s;
                scaleTf.localScale = new Vector3(k, k, 1f);
                yield return null;
            }

            scaleTf.localScale = Vector3.one;
        }

        private void UpdateTimer()
        {
            if (_timerContainer == null || _timerText == null) return;
            bool active = !_manager.IsGameOver && !_manager.IsWinCelebration;
            if (_timerContainer.activeSelf != active)
                _timerContainer.SetActive(active);
            if (!active) return;

            int  secs   = Mathf.CeilToInt(_manager.TimeRemaining);
            bool urgent = _manager.TimeRemaining <= 10f;
            if (secs == _lastTimerSec && urgent == _lastTimerUrgent)
                return;

            _lastTimerSec    = secs;
            _lastTimerUrgent = urgent;
            _timerText.text  = secs.ToString();
            _timerText.color = urgent ? ColorUrgent : ColorNormal;
        }

        private void UpdateCatchPrompt()
        {
            if (_catchPromptText == null) return;

            if (_manager.IsGameOver || _manager.IsWinCelebration)
            {
                if (_catchPromptText.gameObject.activeSelf)
                    _catchPromptText.gameObject.SetActive(false);
                _catchPromptKind = CatchPromptKind.Hidden;
                return;
            }

            if (_manager.IsHoldingChicken)
            {
                bool nearCoop = _manager.IsNearCoop();
                if (!nearCoop)
                {
                    if (_catchPromptText.gameObject.activeSelf)
                        _catchPromptText.gameObject.SetActive(false);
                    _catchPromptKind = CatchPromptKind.Hidden;
                    return;
                }

                if (_catchPromptKind != CatchPromptKind.DropCoop)
                {
                    _catchPromptText.gameObject.SetActive(true);
                    _catchPromptText.text  = "Drop in the coop!";
                    _catchPromptText.color = ColorWin;
                    _catchPromptKind       = CatchPromptKind.DropCoop;
                }

                return;
            }

            bool inRange = _manager.IsInCatchRange();
            if (!inRange)
            {
                if (_catchPromptText.gameObject.activeSelf)
                    _catchPromptText.gameObject.SetActive(false);
                _catchPromptKind = CatchPromptKind.Hidden;
                return;
            }

            bool stunned = _manager.IsChickenStunned;
            var  kind    = stunned ? CatchPromptKind.CatchStunned : CatchPromptKind.CatchNormal;
            if (_catchPromptKind == kind && _catchPromptText.gameObject.activeSelf)
                return;

            _catchPromptText.gameObject.SetActive(true);
            _catchPromptText.text  = stunned ? "It's stunned!  Press E!" : "Press E to catch!";
            _catchPromptText.color = ColorWarning;
            _catchPromptKind       = kind;
        }

        private void UpdateHintBar()
        {
            if (_hintBar == null || _hintText == null) return;
            bool show = !_manager.IsGameOver && !_manager.IsWinCelebration;
            if (_hintBar.activeSelf != show)
                _hintBar.SetActive(show);
            if (!show) return;

            bool holding = _manager.IsHoldingChicken;
            if (_lastHintWasHolding.HasValue && _lastHintWasHolding.Value == holding)
                return;

            _lastHintWasHolding = holding;
            _hintText.text      = holding ? HintHolding : HintChase;
        }

        private void UpdateGripMeter()
        {
            if (_gripMeterContainer == null) return;
            bool holding = _manager.IsHoldingChicken && !_manager.IsGameOver && !_manager.IsWinCelebration;
            if (_gripMeterContainer.activeSelf != holding)
                _gripMeterContainer.SetActive(holding);
            if (!holding || _gripMeterFill == null)
            {
                _lastGripAnchor = -1f;
                _lastGripBucket = -1;
                return;
            }

            float grip = _manager.GripFraction;

            // Drive width via anchorMax so the rect physically shrinks left-to-right in real time.
            RectTransform fillRect = _gripMeterFill.rectTransform;
            if (!Mathf.Approximately(grip, _lastGripAnchor))
            {
                _lastGripAnchor = grip;
                fillRect.anchorMax = new Vector2(grip, fillRect.anchorMax.y);
            }

            int bucket = grip > 0.5f ? 2 : grip > 0.25f ? 1 : 0;
            if (_lastGripBucket != bucket)
            {
                _lastGripBucket = bucket;
                _gripMeterFill.color = bucket == 2 ? ColorWin : bucket == 1 ? ColorWarning : ColorUrgent;
            }
        }

        private void UpdateResultPanel()
        {
            if (_resultPanel == null) return;
            bool gameOver = _manager.IsGameOver;
            if (_resultPanel.activeSelf != gameOver)
                _resultPanel.SetActive(gameOver);
            if (_dimOverlay != null && _dimOverlay.activeSelf != gameOver)
                _dimOverlay.SetActive(gameOver);

            if (gameOver && !_wasGameOver)
            {
                if (_victoryPhraseRoot != null)
                    _victoryPhraseRoot.SetActive(false);

                if (_manager.IsWon)
                {
                    int elapsed = Mathf.CeilToInt(_manager.timeLimit - _manager.TimeRemaining);
                    if (_resultText    != null) { _resultText.text = "CHICKEN CAUGHT!"; _resultText.color = ColorWin; }
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
