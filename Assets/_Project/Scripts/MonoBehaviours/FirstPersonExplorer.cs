using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Simple first-person controller for world exploration.
    /// WASD to move, mouse to look. No jumping.
    /// Uses the new Input System package.
    /// </summary>
    public class FirstPersonExplorer : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float lookSpeed = 2f;
        [SerializeField] private float gravity = -15f;
        [SerializeField] private float jumpForce = 7f;

        private CharacterController _controller;
        private Transform _cameraTransform;
        private float _pitch;
        private float _yVelocity;

        private void Awake()
        {
            TryResolveReferences();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            TryResolveReferences();
            HandleLook();
            HandleMove();
        }

        private void HandleLook()
        {
            if (_cameraTransform == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 delta = mouse.delta.ReadValue();
            float mouseX = delta.x * lookSpeed * 0.1f;
            float mouseY = delta.y * lookSpeed * 0.1f;

            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, -80f, 80f);

            _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        private void HandleMove()
        {
            if (_controller == null) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            float h = 0f;
            float v = 0f;

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v -= 1f;

            Vector3 move = transform.right * h + transform.forward * v;
            move *= moveSpeed;

            if (_controller.isGrounded)
            {
                _yVelocity = -2f;
                if (keyboard.spaceKey.wasPressedThisFrame)
                    _yVelocity = jumpForce;
            }
            else
            {
                _yVelocity += gravity * Time.deltaTime;
            }

            move.y = _yVelocity;
            _controller.Move(move * Time.deltaTime);
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void TryResolveReferences()
        {
            if (_controller == null)
                _controller = GetComponent<CharacterController>();

            if (_cameraTransform == null)
            {
                var cameraComponent = GetComponentInChildren<Camera>();
                if (cameraComponent != null)
                    _cameraTransform = cameraComponent.transform;
            }
        }
    }
}
