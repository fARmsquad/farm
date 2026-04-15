using System.Collections.Generic;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Interaction;
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
    ///   E        → interact with nearest NPC or interactable object in range.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class TownPlayerController : MonoBehaviour
    {
        private const float MOVE_SPEED      = 4f;
        private const float RUN_SPEED       = 7f;
        private const float GRAVITY         = -18f;
        private const float LOOK_SPEED      = 2f;
        private const float MIN_PITCH       = -35f;
        private const float MAX_PITCH       = 55f;

        [Header("References")]
        [SerializeField] private Transform cameraRig;
        [SerializeField] private LLMConversationController conversationController;

        [Header("Interaction UI")]
        [SerializeField] private TextMeshProUGUI interactPromptLabel;

        private CharacterController _cc;
        private TownCameraFollow _cameraFollow;
        private float _verticalVelocity;
        private float _pitch;
        private bool _controlEnabled;
        private bool _suspended;
        private bool _wasInConversation;

        private List<NPCController> _npcs = new();
        private List<InteractableObject> _interactables = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _npcs.AddRange(FindObjectsByType<NPCController>(FindObjectsSortMode.None));
            _interactables.AddRange(FindObjectsByType<InteractableObject>(FindObjectsSortMode.None));

            if (cameraRig != null)
                _cameraFollow = cameraRig.GetComponent<TownCameraFollow>();

            if (interactPromptLabel != null)
                interactPromptLabel.gameObject.SetActive(false);
        }

        private void Start()
        {
            RefreshInteractables();
        }

        private void Update()
        {
            if (!_controlEnabled
                && Keyboard.current != null
                && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                EnableControl();
            }

            if (!_controlEnabled || _suspended) return;

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

        /// <summary>
        /// Temporarily freezes movement and releases the cursor. Called by UI panels.
        /// </summary>
        public void SuspendControl()
        {
            _suspended       = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        /// <summary>
        /// Restores movement after a UI panel closes.
        /// </summary>
        public void ResumeControl()
        {
            _suspended = false;
            if (_controlEnabled)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }

        /// <summary>
        /// Re-populates NPC and interactable lists. Call after scene transitions
        /// so newly loaded objects are detected.
        /// </summary>
        public void RefreshInteractables()
        {
            _npcs.Clear();
            _npcs.AddRange(FindObjectsByType<NPCController>(FindObjectsSortMode.None));

            _interactables.Clear();
            _interactables.AddRange(FindObjectsByType<InteractableObject>(FindObjectsSortMode.None));
        }

        // ── Interaction prompt ────────────────────────────────────────────────

        /// <summary>
        /// Shows "Press E to talk/interact" when the player is within range of any NPC or interactable.
        /// NPCs take priority over interactable objects when both are in range.
        /// </summary>
        private void UpdateInteractPrompt()
        {
            if (interactPromptLabel == null) return;

            // Check NPCs first (they take priority)
            NPCController nearestNpc = GetNearestNPCInRange();
            if (nearestNpc != null)
            {
                interactPromptLabel.text = $"Press E to talk to {nearestNpc.NpcName}";
                interactPromptLabel.gameObject.SetActive(true);

                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                    nearestNpc.TriggerInteraction();
                return;
            }

            // Check interactable objects as fallback
            InteractableObject nearestInteractable = GetNearestInteractableInRange();
            if (nearestInteractable != null)
            {
                interactPromptLabel.text = nearestInteractable.InteractionPrompt;
                interactPromptLabel.gameObject.SetActive(true);

                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                    nearestInteractable.OnInteract();
                return;
            }

            if (interactPromptLabel.gameObject.activeSelf)
                interactPromptLabel.gameObject.SetActive(false);
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

        private InteractableObject GetNearestInteractableInRange()
        {
            InteractableObject nearest = null;
            float nearestSqDist = float.MaxValue;

            foreach (var interactable in _interactables)
            {
                if (interactable == null || !interactable.gameObject.activeInHierarchy) continue;
                if (!interactable.IsPlayerInRange) continue;

                float sqDist = (interactable.transform.position - transform.position).sqrMagnitude;
                if (sqDist < nearestSqDist)
                {
                    nearestSqDist = sqDist;
                    nearest       = interactable;
                }
            }

            return nearest;
        }

        // ── Mouse look (yaw + pitch) ──────────────────────────────────────────

        private void HandleMouseLook()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 delta = mouse.delta.ReadValue();
            float mouseX = delta.x * LOOK_SPEED * 0.1f;
            float mouseY = delta.y * LOOK_SPEED * 0.1f;

            transform.Rotate(Vector3.up * mouseX);

            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, MIN_PITCH, MAX_PITCH);

            if (_cameraFollow != null)
                _cameraFollow.SetPitch(_pitch);
        }

        // ── WASD movement (player-relative) ───────────────────────────────────

        private void HandleMovement()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            float speed = kb.leftShiftKey.isPressed ? RUN_SPEED : MOVE_SPEED;

            Vector2 wasd = Vector2.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    wasd.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  wasd.y -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  wasd.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) wasd.x += 1f;
            wasd = Vector2.ClampMagnitude(wasd, 1f);

            Vector3 move = (transform.forward * wasd.y + transform.right * wasd.x) * speed;

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
