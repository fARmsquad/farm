using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float jumpForce = 7f;

        private CharacterController _controller;
        private float _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            float h = 0f;
            float v = 0f;
            if (kb.aKey.isPressed) h -= 1f;
            if (kb.dKey.isPressed) h += 1f;
            if (kb.sKey.isPressed) v -= 1f;
            if (kb.wKey.isPressed) v += 1f;

            Vector3 move = new Vector3(h, 0, v).normalized;

            // Gravity & jump
            if (_controller != null && _controller.isGrounded)
            {
                _verticalVelocity = -2f;
                if (kb.spaceKey.wasPressedThisFrame)
                    _verticalVelocity = jumpForce;
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }

            // Rotate toward movement direction
            if (move.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(move);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            // Apply movement + gravity
            Vector3 velocity = move * moveSpeed;
            velocity.y = _verticalVelocity;

            if (_controller != null)
                _controller.Move(velocity * Time.deltaTime);
            else
                transform.position += velocity * Time.deltaTime;
        }
    }
}
