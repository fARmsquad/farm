using UnityEngine;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Simple first-person controller for world exploration.
    /// WASD to move, mouse to look. No jumping.
    /// </summary>
    public class FirstPersonExplorer : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float lookSpeed = 2f;
        [SerializeField] private float gravity = -15f;

        private CharacterController _controller;
        private Transform _cameraTransform;
        private float _pitch;
        private float _yVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _cameraTransform = GetComponentInChildren<Camera>().transform;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            HandleLook();
            HandleMove();
        }

        private void HandleLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, -80f, 80f);

            _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        private void HandleMove()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Vector3 move = transform.right * h + transform.forward * v;
            move *= moveSpeed;

            if (_controller.isGrounded)
                _yVelocity = -2f;
            else
                _yVelocity += gravity * Time.deltaTime;

            move.y = _yVelocity;
            _controller.Move(move * Time.deltaTime);
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
