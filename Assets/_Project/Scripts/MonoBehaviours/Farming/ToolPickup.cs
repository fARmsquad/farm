using System;
using UnityEngine;
using UnityEngine.InputSystem;
using FarmSimVR.Core.Inventory;
using FarmSimVR.MonoBehaviours.UI;

namespace FarmSimVR.MonoBehaviours.Farming
{
    /// <summary>
    /// MonoBehaviour placed on each spawned tool. Handles proximity detection
    /// and interact-to-collect via Input System.
    /// </summary>
    public sealed class ToolPickup : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private float _interactRadius = 2.5f;

        private const string PlayerTag = "Player";
        private const string PromptFormat = "Press [E] to pick up {0}";
        private const string InventoryFullMessage = "Inventory full";
        private const float FeedbackDuration = 2f;

        private Transform _playerTransform;
        private IInventorySystem _inventory;
        private bool _collected;
        private bool _inRange;
        private string _feedbackMessage;
        private float _feedbackUntil;

        // ── GUI styles (built once) ──
        private GUIStyle _promptStyle;
        private GUIStyle _feedbackStyle;
        private bool _stylesReady;

        /// <summary>The tool's item ID for display purposes.</summary>
        public string ItemId { get; private set; }

        /// <summary>Fired after the tool is added to inventory and destroyed.</summary>
        public event Action OnCollected;

        /// <summary>Called by the spawner after instantiation.</summary>
        public void Initialize(string itemId, IInventorySystem inventory)
        {
            ItemId = itemId;
            _inventory = inventory;

            if (_inventory == null)
            {
                Debug.LogWarning($"[ToolPickup] Inventory reference is null for '{itemId}'. Disabling pickup.");
                enabled = false;
            }
        }

        private void Start()
        {
            if (_playerTransform == null)
            {
                var player = GameObject.FindWithTag(PlayerTag);
                if (player != null)
                    _playerTransform = player.transform;
                else
                    Debug.LogWarning("[ToolPickup] No GameObject found with tag 'Player'.");
            }
        }

        private void Update()
        {
            if (_collected || _playerTransform == null || _inventory == null)
                return;

            float distance = Vector3.Distance(_playerTransform.position, transform.position);
            _inRange = distance <= _interactRadius;

            if (_inRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                TryCollect();
            }
        }

        private void TryCollect()
        {
            if (_collected)
                return;

            _collected = true;

            var result = _inventory.AddItem(ItemId, 1);
            if (!result.Success)
            {
                _feedbackMessage = InventoryFullMessage;
                _feedbackUntil = Time.time + FeedbackDuration;
                _collected = false; // allow retry
                return;
            }

            Debug.Log($"[ToolPickup] Collected '{ItemId}'.");

            // Refresh inventory UI so the new item shows up
            var inventoryUI = FindAnyObjectByType<InventoryUIController>();
            if (inventoryUI != null)
                inventoryUI.RefreshAll();

            var hotbarUI = FindAnyObjectByType<HotbarUIController>();
            if (hotbarUI != null)
                hotbarUI.Refresh();

            OnCollected?.Invoke();
            Destroy(gameObject);
        }

        private void OnGUI()
        {
            if (_playerTransform == null)
                return;

            EnsureStyles();

            // Show feedback message (inventory full)
            if (!string.IsNullOrEmpty(_feedbackMessage) && Time.time < _feedbackUntil)
            {
                var feedbackRect = new Rect(Screen.width * 0.5f - 150f, Screen.height * 0.6f, 300f, 40f);
                GUI.Label(feedbackRect, _feedbackMessage, _feedbackStyle);
                return;
            }

            if (_collected || !_inRange)
                return;

            string displayName = FormatDisplayName(ItemId);
            string prompt = string.Format(PromptFormat, displayName);
            var promptRect = new Rect(Screen.width * 0.5f - 200f, Screen.height * 0.55f, 400f, 40f);
            GUI.Label(promptRect, prompt, _promptStyle);
        }

        private void EnsureStyles()
        {
            if (_stylesReady)
                return;

            _promptStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _feedbackStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.3f, 0.3f) }
            };

            _stylesReady = true;
        }

        /// <summary>Converts an item ID like "tool_hoe" into "Hoe".</summary>
        private static string FormatDisplayName(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return "Unknown";

            // Strip "tool_" prefix
            const string prefix = "tool_";
            string raw = itemId.StartsWith(prefix) ? itemId.Substring(prefix.Length) : itemId;

            // Replace underscores with spaces and title-case each word
            var parts = raw.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            }

            return string.Join(" ", parts);
        }
    }
}
