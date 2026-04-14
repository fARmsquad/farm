using UnityEngine;
using UnityEngine.InputSystem;
using FarmSimVR.Core.Farming;
using FarmSimVR.Core.Inventory;

namespace FarmSimVR.MonoBehaviours.Farming
{
    /// <summary>
    /// Handles watering can juice: looping pour SFX, splash particles at the player's feet,
    /// and an on-screen "empty" message when the player tries to pour with no water.
    /// Attach to any persistent scene object (e.g. the same one as HotbarUIController).
    /// </summary>
    public sealed class WateringCanFeedbackController : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioClip pourLoopClip;
        [SerializeField] private AudioClip emptyClip;
        [SerializeField, Range(0f, 1f)] private float pourVolume = 0.6f;
        [SerializeField, Range(0f, 1f)] private float emptyVolume = 0.8f;

        [Header("Particles")]
        [SerializeField] private GameObject splashPrefab;
        [SerializeField] private Vector3 splashOffset = new(0f, 0.05f, 1.0f);

        private WateringCanState _waterCan;
        private ToolEquipState _toolEquip;

        private AudioSource _pourSource;
        private AudioSource _sfxSource;
        private ParticleSystem _splashInstance;
        private Transform _playerTransform;
        private Camera _camera;
        private bool _initialized;
        private bool _isPouring;

        // Empty-can message
        private string _emptyMessage;
        private float _emptyMessageUntil;
        private GUIStyle _messageStyle;
        private bool _stylesReady;

        private const float EmptyMessageDuration = 1.5f;
        private const float EmptyCooldown = 0.5f;
        private float _lastEmptyTime;

        /// <summary>
        /// Initializes the controller with shared state. Called from FarmSimDriver or ToolSpawnManager.
        /// </summary>
        public void Initialize(WateringCanState waterCan, ToolEquipState toolEquip)
        {
            _waterCan = waterCan;
            _toolEquip = toolEquip;
            _initialized = true;
        }

        private void Start()
        {
            // Dedicated AudioSource for looping pour
            _pourSource = gameObject.AddComponent<AudioSource>();
            _pourSource.playOnAwake = false;
            _pourSource.loop = true;
            _pourSource.spatialBlend = 0f;

            // One-shot AudioSource for empty clunk
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;
            _sfxSource.spatialBlend = 0f;

            _camera = Camera.main;

            // Pre-instantiate splash particle (disabled until needed)
            if (splashPrefab != null)
            {
                var go = Instantiate(splashPrefab);
                go.name = "WateringCanSplash";
                _splashInstance = go.GetComponent<ParticleSystem>();
                if (_splashInstance != null)
                {
                    var emission = _splashInstance.emission;
                    emission.enabled = true;
                    _splashInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                go.SetActive(false);
            }
        }

        private void Update()
        {
            if (!_initialized || _toolEquip == null || _waterCan == null)
                return;

            bool canEquipped = _toolEquip.EquippedTool == FarmToolId.WateringCan;
            var mouse = Mouse.current;
            bool lmbHeld = mouse != null && mouse.leftButton.isPressed;
            bool lmbPressed = mouse != null && mouse.leftButton.wasPressedThisFrame;

            // --- Empty can feedback ---
            if (canEquipped && lmbPressed && _waterCan.IsEmpty
                && Time.time > _lastEmptyTime + EmptyCooldown)
            {
                _emptyMessage = "Watering can is empty! Refill at the well.";
                _emptyMessageUntil = Time.time + EmptyMessageDuration;
                _lastEmptyTime = Time.time;

                if (emptyClip != null && _sfxSource != null)
                    _sfxSource.PlayOneShot(emptyClip, emptyVolume);
            }

            // --- Pour state ---
            bool shouldPour = canEquipped && lmbHeld && !_waterCan.IsEmpty;

            if (shouldPour && !_isPouring)
                StartPour();
            else if (!shouldPour && _isPouring)
                StopPour();

            // Update splash position while pouring
            if (_isPouring && _splashInstance != null)
                _splashInstance.transform.position = GetSplashPosition();
        }

        private void StartPour()
        {
            _isPouring = true;

            if (pourLoopClip != null && _pourSource != null)
            {
                _pourSource.clip = pourLoopClip;
                _pourSource.volume = pourVolume;
                _pourSource.Play();
            }

            if (_splashInstance != null)
            {
                _splashInstance.gameObject.SetActive(true);
                _splashInstance.transform.position = GetSplashPosition();
                _splashInstance.Play(true);
            }
        }

        private void StopPour()
        {
            _isPouring = false;

            if (_pourSource != null && _pourSource.isPlaying)
                _pourSource.Stop();

            if (_splashInstance != null)
            {
                _splashInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        /// <summary>
        /// Returns the world position where splash particles should appear (player feet + forward offset).
        /// </summary>
        private Vector3 GetSplashPosition()
        {
            // Try tagged Player first
            if (_playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                    _playerTransform = player.transform;
            }

            if (_playerTransform != null)
            {
                return _playerTransform.position
                       + _playerTransform.forward * splashOffset.z
                       + Vector3.up * splashOffset.y
                       + _playerTransform.right * splashOffset.x;
            }

            // Fallback: camera-based (first person)
            if (_camera == null)
                _camera = Camera.main;

            if (_camera != null)
            {
                var camT = _camera.transform;
                var forward = camT.forward;
                forward.y = 0f;
                forward.Normalize();

                // Raycast down from camera forward to find ground
                var origin = camT.position + forward * splashOffset.z;
                if (Physics.Raycast(origin, Vector3.down, out var hit, 10f))
                    return hit.point + Vector3.up * splashOffset.y;

                return origin + Vector3.down * 1.5f;
            }

            return transform.position;
        }

        private void OnDisable()
        {
            StopPour();
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_emptyMessage) || Time.time >= _emptyMessageUntil)
                return;

            BuildStyles();

            float alpha = Mathf.Clamp01((_emptyMessageUntil - Time.time) / 0.4f);

            const float panelWidth = 360f;
            const float panelHeight = 36f;
            float panelX = (Screen.width - panelWidth) * 0.5f;
            float panelY = Screen.height * 0.38f;

            // Background
            GUI.color = new Color(0.4f, 0.08f, 0.08f, 0.88f * alpha);
            GUI.DrawTexture(new Rect(panelX, panelY, panelWidth, panelHeight), Texture2D.whiteTexture);

            // Text
            GUI.color = new Color(1f, 0.85f, 0.7f, alpha);
            GUI.Label(new Rect(panelX + 14f, panelY + 7f, panelWidth - 28f, 22f), _emptyMessage, _messageStyle);

            GUI.color = Color.white;
        }

        private void BuildStyles()
        {
            if (_stylesReady)
                return;

            _messageStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _messageStyle.normal.textColor = Color.white;
            _stylesReady = true;
        }
    }
}
