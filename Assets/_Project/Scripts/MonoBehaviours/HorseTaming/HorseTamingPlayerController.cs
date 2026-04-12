using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours.HorseTaming
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class HorseTamingPlayerController : MonoBehaviour
    {
        [Header("Movement (XZ, top-down)")]
        [SerializeField] private float walkSpeed = 2.8f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float gravity = -20f;

        private CharacterController _cc;
        private Keyboard _keyboard;
        private float _verticalVelocity;
        private Vector3 _horizontalVelocity;

        public bool MovementEnabled { get; set; } = true;

        /// <summary>XZ speed in units/sec (ignoring vertical).</summary>
        public float HorizontalSpeed => new Vector3(_horizontalVelocity.x, 0f, _horizontalVelocity.z).magnitude;

        public bool SprintHeld { get; private set; }
        public bool HasMoveInput { get; private set; }

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            _keyboard = Keyboard.current;
        }

        private void Update()
        {
            if (!MovementEnabled)
                return;

            if (_keyboard == null)
                _keyboard = Keyboard.current;
            if (_keyboard == null)
                return;

            float x = 0f;
            if (_keyboard.aKey.isPressed) x -= 1f;
            if (_keyboard.dKey.isPressed) x += 1f;
            float z = 0f;
            if (_keyboard.sKey.isPressed) z -= 1f;
            if (_keyboard.wKey.isPressed) z += 1f;

            var dir = new Vector3(x, 0f, z);
            if (dir.sqrMagnitude > 1f)
                dir.Normalize();

            HasMoveInput = dir.sqrMagnitude > 0.01f;
            SprintHeld = _keyboard.leftShiftKey.isPressed;

            float speed = SprintHeld ? sprintSpeed : walkSpeed;
            _horizontalVelocity = dir * speed;

            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            _verticalVelocity += gravity * Time.deltaTime;

            var motion = _horizontalVelocity * Time.deltaTime;
            motion.y = _verticalVelocity * Time.deltaTime;
            _cc.Move(motion);
        }
    }
}
