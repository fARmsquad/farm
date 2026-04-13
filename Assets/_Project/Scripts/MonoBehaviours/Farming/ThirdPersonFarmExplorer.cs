using UnityEngine;
using UnityEngine.InputSystem;
using FarmSimVR.MonoBehaviours.UI;

namespace FarmSimVR.MonoBehaviours.Farming
{
    /// <summary>
    /// Third-person CharacterController for farm scenes: WASD move, mouse X turns the body,
    /// mouse Y adjusts camera pitch. Camera follows behind at a fixed distance.
    /// </summary>
    public sealed class ThirdPersonFarmExplorer : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float lookSpeed = 2f;
        [SerializeField] private float gravity = -15f;
        [SerializeField] private float jumpForce = 7f;
        [SerializeField] private float cameraDistance = 4f;
        [SerializeField] private float shoulderHeight = 1.4f;
        [SerializeField] private float lookAtHeight = 1.2f;
        [SerializeField] private float minPitch = -35f;
        [SerializeField] private float maxPitch = 55f;

        private CharacterController _controller;
        private Transform _cameraTransform;
        private FarmPlotInteractionController _interactionController;
        private InventoryUIController _inventoryUI;
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

            if (_interactionController != null && _interactionController.IsMinigameActive)
                return;

            // Skip look/move input when the backpack inventory is open
            if (_inventoryUI == null)
                _inventoryUI = FindAnyObjectByType<InventoryUIController>();
            if (_inventoryUI != null && _inventoryUI.IsOpen)
                return;

            HandleLook();
            HandleMove();
        }

        private void LateUpdate()
        {
            UpdateCamera();
        }

        private void HandleLook()
        {
            if (_cameraTransform == null)
                return;

            var mouse = Mouse.current;
            if (mouse == null)
                return;

            Vector2 delta = mouse.delta.ReadValue();
            float mouseX = delta.x * lookSpeed * 0.1f;
            float mouseY = delta.y * lookSpeed * 0.1f;

            transform.Rotate(Vector3.up * mouseX);
            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        private void UpdateCamera()
        {
            if (_cameraTransform == null)
                return;

            Vector3 focus = transform.position + Vector3.up * lookAtHeight;
            float yaw = transform.eulerAngles.y;
            Quaternion rot = Quaternion.Euler(_pitch, yaw, 0f);
            Vector3 back = rot * new Vector3(0f, 0f, -cameraDistance);
            _cameraTransform.position = transform.position + Vector3.up * shoulderHeight + back;
            _cameraTransform.LookAt(focus);
        }

        private void HandleMove()
        {
            if (_controller == null)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

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
                var cam = Camera.main;
                if (cam != null)
                    _cameraTransform = cam.transform;
            }

            if (_interactionController == null)
                _interactionController = Object.FindAnyObjectByType<FarmPlotInteractionController>();
        }
    }
}
