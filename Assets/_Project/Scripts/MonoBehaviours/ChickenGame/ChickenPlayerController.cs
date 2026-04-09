using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours.ChickenGame
{
    [RequireComponent(typeof(CharacterController))]
    public class ChickenPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] public float moveSpeed       = 5f;
        [SerializeField] public float gravity         = -20f;

        [Header("Mouse Look")]
        [SerializeField] public float mouseSensitivity = 0.15f;
        [SerializeField] public float pitchMin        = -70f;
        [SerializeField] public float pitchMax        =  70f;

        [Header("References")]
        [SerializeField] public Camera fpCamera;

        private CharacterController _cc;
        private float _yaw;
        private float _pitch;
        private float _verticalVelocity;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            // Initialise yaw from current rotation so we don't snap
            _yaw = transform.eulerAngles.y;
        }

        private void Update()
        {
            HandleCursorToggle();
            HandleMouseLook();
            HandleMovement();
        }

        private void HandleCursorToggle()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                bool locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible   = locked;
            }
        }

        private void HandleMouseLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 delta = mouse.delta.ReadValue();
            _yaw   += delta.x * mouseSensitivity;
            _pitch -= delta.y * mouseSensitivity;
            _pitch  = Mathf.Clamp(_pitch, pitchMin, pitchMax);

            // Player body rotates left/right
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            // Camera pitches up/down only
            if (fpCamera != null)
                fpCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            float h = 0f, v = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    v += 1f;

            // Move relative to where the player is facing
            Vector3 move = (transform.right * h + transform.forward * v);
            move.y = 0f;
            if (move.sqrMagnitude > 1f) move.Normalize();

            if (_cc.isGrounded)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = move * moveSpeed;
            velocity.y = _verticalVelocity;
            _cc.Move(velocity * Time.deltaTime);
        }

        // Called by ChickenGameManager when the game ends/restarts
        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible   = !locked;
        }
    }
}
