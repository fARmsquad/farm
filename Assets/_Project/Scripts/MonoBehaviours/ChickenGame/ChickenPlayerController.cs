using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours.ChickenGame
{
    [RequireComponent(typeof(CharacterController))]
    public class ChickenPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] public float moveSpeed       = 5f;
        [SerializeField] public float sprintSpeed     = 8f;
        [SerializeField] public float acceleration    = 18f;   // How quickly speed builds up
        [SerializeField] public float deceleration    = 24f;   // How quickly speed drops when input stops
        [SerializeField] public float gravity         = -20f;

        [Header("Stamina")]
        [SerializeField] public float maxStamina        = 100f;
        [SerializeField] public float staminaDrainRate  = 30f;
        [SerializeField] public float staminaRegenRate  = 20f;
        [SerializeField] public float staminaRegenDelay = 1f;

        [Header("Lunge Miss")]
        [SerializeField] public float lungeMissDuration        = 0.45f;
        [SerializeField] public float lungeMissSpeedMultiplier = 0.25f;
        [SerializeField] public float lungeMissCameraDipAngle  = 18f;

        [Header("Mouse Look")]
        [SerializeField] public float mouseSensitivity = 0.15f;
        [SerializeField] public float lookSmoothing    = 0f;    // 0 = no smoothing (raw), 0–1 for lag
        [SerializeField] public float pitchMin         = -70f;
        [SerializeField] public float pitchMax         =  70f;
        [SerializeField] public float sprintFovIncrease = 8f;   // Extra FOV degrees while sprinting

        [Header("References")]
        [SerializeField] public Camera fpCamera;

        /// <summary>Stamina as a 0–1 fraction, used by the UI stamina bar.</summary>
        public float StaminaFraction => _stamina / maxStamina;

        private CharacterController _cc;
        private float _yaw;
        private float _pitch;
        private float _verticalVelocity;
        private Vector3 _smoothVelocity;    // Current horizontal velocity, smoothed via acceleration
        private float _baseFov;             // Camera FOV at rest, captured in Start

        private float _stamina;
        private float _staminaRegenDelayTimer;
        private float _lungeMissTimer;
        private bool  _celebrationFrozen;

        // Cached input devices — avoids per-frame property lookups
        private Keyboard _keyboard;
        private Mouse _mouse;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            _keyboard = Keyboard.current;
            _mouse    = Mouse.current;
        }

        private void Start()
        {
            _stamina = maxStamina;
            _baseFov = fpCamera != null ? fpCamera.fieldOfView : 60f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            _yaw = transform.eulerAngles.y;
        }

        private void Update()
        {
            if (_celebrationFrozen)
                return;

            HandleCursorToggle();
            HandleMouseLook();
            HandleMovement();

            if (_lungeMissTimer > 0f)
                _lungeMissTimer -= Time.deltaTime;
        }

        /// <summary>Triggers the lunge-and-miss penalty: a brief speed reduction and camera dip.</summary>
        public void TriggerLungeMiss()
        {
            _lungeMissTimer = lungeMissDuration;
        }

        /// <summary>Resets stamina and any active penalties. Call this on game restart.</summary>
        public void ResetState()
        {
            _celebrationFrozen        = false;
            _stamina                = maxStamina;
            _lungeMissTimer         = 0f;
            _staminaRegenDelayTimer = 0f;
            _smoothVelocity         = Vector3.zero;
            if (fpCamera != null) fpCamera.fieldOfView = _baseFov;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        /// <summary>Freezes movement and look; unlocks cursor for the scripted camera.</summary>
        public void SetCelebrationFrozen(bool frozen)
        {
            _celebrationFrozen = frozen;
            if (frozen)
            {
                _smoothVelocity = Vector3.zero;
                SetCursorLocked(false);
            }
        }

        /// <summary>Called by ChickenGameManager when the game ends or restarts.</summary>
        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible   = !locked;
        }

        private void HandleCursorToggle()
        {
            if (_keyboard != null && _keyboard.escapeKey.wasPressedThisFrame)
            {
                bool locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible   = locked;
            }
        }

        private void HandleMouseLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;
            if (_mouse == null) return;

            Vector2 delta = _mouse.delta.ReadValue();
            if (delta.sqrMagnitude >= 0.0001f)
            {
                _yaw   += delta.x * mouseSensitivity;
                _pitch -= delta.y * mouseSensitivity;
                _pitch  = Mathf.Clamp(_pitch, pitchMin, pitchMax);
                transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            }

            if (fpCamera != null)
            {
                // Apply a smooth camera dip during lunge miss without permanently changing stored pitch
                float displayPitch = _pitch;
                if (_lungeMissTimer > 0f)
                {
                    float t = _lungeMissTimer / lungeMissDuration;
                    displayPitch = Mathf.Clamp(
                        _pitch + Mathf.Sin(t * Mathf.PI) * lungeMissCameraDipAngle,
                        pitchMin, pitchMax
                    );
                }
                fpCamera.transform.localRotation = Quaternion.Euler(displayPitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            if (_keyboard == null) return;

            float h = 0f, v = 0f;
            if (_keyboard.aKey.isPressed || _keyboard.leftArrowKey.isPressed)  h -= 1f;
            if (_keyboard.dKey.isPressed || _keyboard.rightArrowKey.isPressed) h += 1f;
            if (_keyboard.sKey.isPressed || _keyboard.downArrowKey.isPressed)  v -= 1f;
            if (_keyboard.wKey.isPressed || _keyboard.upArrowKey.isPressed)    v += 1f;

            Vector3 inputDir = transform.right * h + transform.forward * v;
            inputDir.y = 0f;
            if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

            bool isMoving    = inputDir.sqrMagnitude > 0.01f;
            bool wantsSprint = _keyboard.leftShiftKey.isPressed && isMoving && _stamina > 0f;

            // Stamina drain while sprinting, regen after a delay when not sprinting
            if (wantsSprint)
            {
                _stamina                = Mathf.Max(0f, _stamina - staminaDrainRate * Time.deltaTime);
                _staminaRegenDelayTimer = staminaRegenDelay;
            }
            else
            {
                if (_staminaRegenDelayTimer > 0f)
                    _staminaRegenDelayTimer -= Time.deltaTime;
                else
                    _stamina = Mathf.Min(maxStamina, _stamina + staminaRegenRate * Time.deltaTime);
            }

            float targetSpeed = wantsSprint ? sprintSpeed : moveSpeed;
            if (_lungeMissTimer > 0f) targetSpeed *= lungeMissSpeedMultiplier;

            // Smoothly accelerate toward target velocity so starts/stops feel weighty
            Vector3 targetVelocity = inputDir * targetSpeed;
            float rate = isMoving ? acceleration : deceleration;
            _smoothVelocity = Vector3.MoveTowards(_smoothVelocity, targetVelocity, rate * Time.deltaTime);

            // Gravity
            if (_cc.isGrounded)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += gravity * Time.deltaTime;

            Vector3 move = _smoothVelocity;
            move.y = _verticalVelocity;
            _cc.Move(move * Time.deltaTime);

            // FOV kick while sprinting — subtle but makes speed feel real
            if (fpCamera != null)
            {
                float sprintFrac  = wantsSprint ? (_smoothVelocity.magnitude / sprintSpeed) : 0f;
                float targetFov   = _baseFov + sprintFovIncrease * sprintFrac;
                fpCamera.fieldOfView = Mathf.Lerp(fpCamera.fieldOfView, targetFov, 10f * Time.deltaTime);
            }
        }
    }
}
