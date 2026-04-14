using UnityEngine;
using UnityEngine.InputSystem;
using FarmSimVR.Core.Farming;
using FarmSimVR.Core.Inventory;

namespace FarmSimVR.MonoBehaviours.Farming
{
    /// <summary>
    /// Handles player proximity detection at the Well and a timed hold-E interaction
    /// to refill the watering can. Uses IMGUI for prompt and progress feedback.
    /// </summary>
    public sealed class WellInteractionController : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private float interactRadius = 3.0f;
        [SerializeField] private float fillDuration = 1.5f;

        [Header("Audio")]
        [SerializeField] private AudioClip fillStartClip;
        [SerializeField] private AudioClip fillCompleteClip;

        private WateringCanState _canState;
        private ToolEquipState _toolEquip;
        private AudioSource _audioSource;
        private Transform _playerTransform;
        private Camera _camera;

        private float _fillProgress;
        private bool _isInRange;
        private bool _isHolding;
        private bool _initialized;

        private string _promptMessage;
        private string _feedbackMessage;
        private float _feedbackUntil;

        // GUI styles
        private GUIStyle _promptStyle;
        private GUIStyle _feedbackStyle;
        private bool _stylesReady;

        /// <summary>True while the fill timer is actively progressing.</summary>
        public bool IsRefilling => _isHolding && _fillProgress > 0f;

        /// <summary>
        /// Initializes the controller with shared state references. Called from FarmSimDriver.Start().
        /// </summary>
        public void Initialize(WateringCanState canState, ToolEquipState toolEquip)
        {
            _canState = canState;
            _toolEquip = toolEquip;
            _initialized = true;
        }

        private void Start()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                _playerTransform = playerObj.transform;

            _camera = Camera.main;
        }

        /// <summary>
        /// Returns the best available player position — tagged Player object first, then main camera fallback.
        /// </summary>
        private Vector3 GetPlayerPosition()
        {
            if (_playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                    _playerTransform = player.transform;
            }

            if (_playerTransform != null)
                return _playerTransform.position;

            if (_camera == null)
                _camera = Camera.main;

            return _camera != null ? _camera.transform.position : transform.position;
        }

        private void Update()
        {
            if (!_initialized)
                return;

            float distance = Vector3.Distance(GetPlayerPosition(), transform.position);
            _isInRange = distance <= interactRadius;

            _promptMessage = null;

            if (!_isInRange)
            {
                ResetFill();
                return;
            }

            bool hasWateringCan = _toolEquip != null
                && _toolEquip.EquippedTool == FarmToolId.WateringCan;

            if (!hasWateringCan)
            {
                _promptMessage = "Equip Watering Can to fill";
                ResetFill();
                return;
            }

            if (_canState.IsFull)
            {
                _promptMessage = "Watering can is full";
                ResetFill();
                return;
            }

            int waterPercent = Mathf.RoundToInt(_canState.WaterLevel * 100f);
            _promptMessage = $"Hold [E] to fill Watering Can ({waterPercent}%)";

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.eKey.isPressed)
            {
                if (!_isHolding)
                {
                    _isHolding = true;
                    if (fillStartClip != null && _audioSource != null)
                    {
                        _audioSource.clip = fillStartClip;
                        _audioSource.loop = true;
                        _audioSource.Play();
                    }
                }

                _fillProgress += Time.deltaTime / fillDuration;

                if (_fillProgress >= 1.0f)
                {
                    _canState.FillToMax();
                    StopAudio();

                    if (fillCompleteClip != null && _audioSource != null)
                        _audioSource.PlayOneShot(fillCompleteClip);

                    _feedbackMessage = "Watering can filled!";
                    _feedbackUntil = Time.time + 2f;
                    _fillProgress = 0f;
                    _isHolding = false;
                }
            }
            else if (_isHolding)
            {
                ResetFill();
            }
        }

        private void ResetFill()
        {
            if (_isHolding)
            {
                StopAudio();
                _isHolding = false;
            }

            _fillProgress = 0f;
        }

        private void StopAudio()
        {
            if (_audioSource != null && _audioSource.isPlaying)
                _audioSource.Stop();
        }

        private void OnGUI()
        {
            if (!_initialized)
                return;

            BuildStyles();

            // Draw feedback message
            if (!string.IsNullOrEmpty(_feedbackMessage) && Time.time < _feedbackUntil)
            {
                float alpha = Mathf.Clamp01((_feedbackUntil - Time.time) / 0.5f);
                const float fbWidth = 300f;
                const float fbHeight = 34f;
                float fbX = (Screen.width - fbWidth) * 0.5f;
                float fbY = Screen.height * 0.35f;

                GUI.color = new Color(0.05f, 0.3f, 0.05f, 0.88f * alpha);
                GUI.DrawTexture(new Rect(fbX, fbY, fbWidth, fbHeight), Texture2D.whiteTexture);
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Label(new Rect(fbX + 12f, fbY + 7f, fbWidth - 24f, 22f), _feedbackMessage, _feedbackStyle);
                GUI.color = Color.white;
            }

            if (!_isInRange || string.IsNullOrEmpty(_promptMessage))
                return;

            // Draw prompt panel
            const float panelWidth = 320f;
            float panelHeight = _isHolding ? 60f : 36f;
            float panelX = (Screen.width - panelWidth) * 0.5f;
            float panelY = Screen.height * 0.55f;

            GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
            GUI.DrawTexture(new Rect(panelX, panelY, panelWidth, panelHeight), Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(new Rect(panelX + 12f, panelY + 8f, panelWidth - 24f, 22f), _promptMessage, _promptStyle);

            // Draw progress bar while holding
            if (_isHolding && _fillProgress > 0f)
            {
                const float barMargin = 12f;
                float barY = panelY + 32f;
                float barWidth = panelWidth - barMargin * 2f;
                const float barHeight = 16f;

                // Background
                GUI.color = new Color(0.1f, 0.1f, 0.3f, 0.8f);
                GUI.DrawTexture(new Rect(panelX + barMargin, barY, barWidth, barHeight), Texture2D.whiteTexture);

                // Fill
                GUI.color = new Color(0.2f, 0.7f, 1.0f, 0.9f);
                GUI.DrawTexture(
                    new Rect(panelX + barMargin, barY, barWidth * Mathf.Clamp01(_fillProgress), barHeight),
                    Texture2D.whiteTexture);

                GUI.color = Color.white;
            }
        }

        private void BuildStyles()
        {
            if (_stylesReady)
                return;

            _promptStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _promptStyle.normal.textColor = Color.white;

            _feedbackStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _feedbackStyle.normal.textColor = Color.white;

            _stylesReady = true;
        }
    }
}
