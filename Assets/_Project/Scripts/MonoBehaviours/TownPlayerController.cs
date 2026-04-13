using System.Collections.Generic;
using FarmSimVR.MonoBehaviours.Cinematics;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Free-roam player controller for the Town scene.
    /// Starts disabled and is activated by TownInteractionAutoplay.OnDemoComplete.
    ///
    /// Controls:
    ///   Mouse X  → turns the character (yaw).
    ///   WASD     → camera-relative movement.
    ///   Shift    → run.
    ///   E        → interact with nearest NPC in range.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class TownPlayerController : MonoBehaviour
    {
        private const float MOVE_SPEED      = 4f;
        private const float RUN_SPEED       = 7f;
        private const float GRAVITY         = -18f;
        private const float MOUSE_TURN_SENS = 120f;

        [Header("References")]
        [SerializeField] private Transform cameraRig;
        [SerializeField] private LLMConversationController conversationController;

        [Header("Interaction UI")]
        [SerializeField] private TextMeshProUGUI interactPromptLabel;

        private CharacterController _cc;
        private float _verticalVelocity;
        private bool _controlEnabled;
        private bool _wasInConversation;

        private List<NPCController> _npcs = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _npcs.AddRange(FindObjectsByType<NPCController>(FindObjectsSortMode.None));

            if (interactPromptLabel != null)
                interactPromptLabel.gameObject.SetActive(false);
        }

        private void Start()
        {
            _npcs.Clear();
            _npcs.AddRange(FindObjectsByType<NPCController>(FindObjectsSortMode.None));
        }

        private void Update()
        {
            if (!_controlEnabled
                && Keyboard.current != null
                && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                EnableControl();
            }

            if (!_controlEnabled) return;

            bool inConversation = conversationController != null && conversationController.IsInConversation;

            if (inConversation != _wasInConversation)
            {
                _wasInConversation = inConversation;
                if (inConversation)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible   = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible   = false;
                }
            }

            if (inConversation) return;

            UpdateInteractPrompt();
            HandleMouseLook();
            HandleMovement();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Enables player movement and mouse steering. Called by TownInteractionAutoplay.
        /// </summary>
        public void EnableControl()
        {
            _controlEnabled  = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            ShowHint("WASD to move  |  Mouse to look  |  E to talk");
        }

        // ── Interaction prompt ────────────────────────────────────────────────

        /// <summary>
        /// Shows "Press E to talk" when the player is within range of any NPC.
        /// </summary>
        private void UpdateInteractPrompt()
        {
            if (interactPromptLabel == null) return;

            NPCController nearest = GetNearestNPCInRange();
            bool inRange = nearest != null;

            if (inRange)
            {
                interactPromptLabel.text = $"Press E to talk to {nearest.NpcName}";
                interactPromptLabel.gameObject.SetActive(true);

                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                    nearest.TriggerInteraction();
            }
            else if (interactPromptLabel.gameObject.activeSelf)
            {
                interactPromptLabel.gameObject.SetActive(false);
            }
        }

        private NPCController GetNearestNPCInRange()
        {
            NPCController nearest  = null;
            float nearestSqDist    = float.MaxValue;

            foreach (var npc in _npcs)
            {
                if (npc == null || !npc.gameObject.activeInHierarchy) continue;
                if (!npc.IsPlayerInRange) continue;

                float sqDist = (npc.transform.position - transform.position).sqrMagnitude;
                if (sqDist < nearestSqDist)
                {
                    nearestSqDist = sqDist;
                    nearest       = npc;
                }
            }

            return nearest;
        }

        // ── Mouse look (yaw only) ─────────────────────────────────────────────

        private void HandleMouseLook()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            float deltaX = mouse.delta.x.ReadValue();
            float yawDelta = deltaX * MOUSE_TURN_SENS * Time.deltaTime;
            transform.Rotate(0f, yawDelta, 0f, Space.Self);
        }

        // ── WASD movement ─────────────────────────────────────────────────────

        private void HandleMovement()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            float speed = kb.leftShiftKey.isPressed ? RUN_SPEED : MOVE_SPEED;

            Transform reference = cameraRig != null ? cameraRig : transform;
            Vector3 camForward  = Vector3.ProjectOnPlane(reference.forward, Vector3.up).normalized;
            Vector3 camRight    = Vector3.ProjectOnPlane(reference.right,   Vector3.up).normalized;

            Vector2 wasd = Vector2.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    wasd.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  wasd.y -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  wasd.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) wasd.x += 1f;
            wasd = Vector2.ClampMagnitude(wasd, 1f);

            Vector3 move = (camForward * wasd.y + camRight * wasd.x) * speed;

            if (_cc.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity += GRAVITY * Time.deltaTime;
            }

            move.y = _verticalVelocity;
            _cc.Move(move * Time.deltaTime);
        }

        // ── Hint (one-shot startup message) ──────────────────────────────────

        private void ShowHint(string message)
        {
            if (interactPromptLabel == null) return;
            interactPromptLabel.text = message;
            interactPromptLabel.gameObject.SetActive(true);
            Invoke(nameof(HideHint), 4f);
        }

        private void HideHint()
        {
            if (interactPromptLabel != null)
                interactPromptLabel.gameObject.SetActive(false);
        }
    }
}
