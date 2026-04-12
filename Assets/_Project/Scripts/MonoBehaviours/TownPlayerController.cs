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
    ///   Mouse X  → turns the character (yaw), steering movement direction.
    ///   Mouse Y  → pushes forward / pulls back (velocity along facing direction).
    ///   WASD     → camera-relative movement; W always = "into the screen".
    ///   Shift    → run.
    ///   E        → interact with nearby NPC.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class TownPlayerController : MonoBehaviour
    {
        private const float MoveSpeed      = 4f;
        private const float RunSpeed       = 7f;
        private const float Gravity        = -18f;
        private const float MouseTurnSensX = 120f;  // degrees/sec per unit of normalised mouse delta
        private const float MouseVelSensY  = 6f;    // forward speed added per unit of mouse Y delta
        private const float MouseVelDecay  = 8f;    // how fast mouse-driven velocity bleeds to zero
        private const float MaxMouseVel    = RunSpeed;

        [Header("References")]
        [SerializeField] private Transform cameraRig;

        [Header("Interaction UI")]
        [SerializeField] private TextMeshProUGUI interactPromptLabel;

        private CharacterController _cc;
        private float _verticalVelocity;
        private float _mouseForwardVel;
        private bool _controlEnabled;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();

            if (interactPromptLabel != null)
                interactPromptLabel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_controlEnabled) return;

            HandleMouseSteering();
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

            ShowHint("WASD to walk  |  Mouse to steer & accelerate  |  E to talk to vendors");
        }

        // ── Mouse steering ────────────────────────────────────────────────────

        private void HandleMouseSteering()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 delta = mouse.delta.ReadValue();

            // Mouse X → yaw the character so it turns left/right
            float yawDelta = delta.x * MouseTurnSensX * Time.deltaTime;
            transform.Rotate(0f, yawDelta, 0f, Space.Self);

            // Mouse Y → accelerate forward (up) or backward (down)
            // Screen Y is inverted relative to world forward, hence the negation
            _mouseForwardVel += -delta.y * MouseVelSensY * Time.deltaTime;
            _mouseForwardVel  = Mathf.Clamp(_mouseForwardVel, -MaxMouseVel, MaxMouseVel);
        }

        // ── WASD + mouse velocity movement ────────────────────────────────────

        private void HandleMovement()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // Bleed off mouse-driven velocity while the mouse is idle
            _mouseForwardVel = Mathf.MoveTowards(_mouseForwardVel, 0f, MouseVelDecay * Time.deltaTime);

            float speed = kb.leftShiftKey.isPressed ? RunSpeed : MoveSpeed;

            // Project the camera's axes onto XZ so W always means
            // "toward what the camera is looking at", regardless of how
            // the player prefab root is oriented in the asset.
            Transform reference = cameraRig != null ? cameraRig : transform;
            Vector3 camForward  = Vector3.ProjectOnPlane(reference.forward, Vector3.up).normalized;
            Vector3 camRight    = Vector3.ProjectOnPlane(reference.right,   Vector3.up).normalized;

            Vector2 wasd = Vector2.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    wasd.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  wasd.y -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  wasd.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) wasd.x += 1f;
            wasd = Vector2.ClampMagnitude(wasd, 1f);

            Vector3 wasdMove  = (camForward * wasd.y + camRight * wasd.x) * speed;
            Vector3 mouseMove = transform.forward * _mouseForwardVel;

            // WASD takes full priority when a key is held; mouse velocity fills in when idle
            Vector3 horizontal = wasd.magnitude > 0.01f ? wasdMove : mouseMove;
            horizontal.y = 0f;

            // Gravity
            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += Gravity * Time.deltaTime;

            horizontal.y = _verticalVelocity;
            _cc.Move(horizontal * Time.deltaTime);
        }

        // ── Interaction hint ──────────────────────────────────────────────────

        private void ShowHint(string message)
        {
            if (interactPromptLabel == null) return;

            interactPromptLabel.text = message;
            interactPromptLabel.gameObject.SetActive(true);
            Invoke(nameof(HideHint), 5f);
        }

        private void HideHint()
        {
            if (interactPromptLabel != null)
                interactPromptLabel.gameObject.SetActive(false);
        }
    }
}
