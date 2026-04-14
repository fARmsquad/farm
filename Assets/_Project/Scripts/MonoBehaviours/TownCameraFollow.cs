using UnityEngine;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Third-person orbit camera that follows a target and rotates behind it
    /// based on the target's yaw and an externally driven pitch angle.
    /// </summary>
    public class TownCameraFollow : MonoBehaviour
    {
        private const float DEFAULT_DISTANCE = 6f;
        private const float DEFAULT_SHOULDER_HEIGHT = 1.4f;
        private const float DEFAULT_LOOK_AT_HEIGHT = 1.2f;
        private const float DEFAULT_SMOOTH_SPEED = 8f;

        [SerializeField] private Transform target;
        [SerializeField] private float distance = DEFAULT_DISTANCE;
        [SerializeField] private float shoulderHeight = DEFAULT_SHOULDER_HEIGHT;
        [SerializeField] private float lookAtHeight = DEFAULT_LOOK_AT_HEIGHT;
        [SerializeField] private float smoothSpeed = DEFAULT_SMOOTH_SPEED;

        private float _pitch;
        private bool _snapNextFrame;

        /// <summary>
        /// Sets the camera pitch angle (vertical look). Clamped by the caller.
        /// </summary>
        public void SetPitch(float pitch)
        {
            _pitch = pitch;
        }

        /// <summary>
        /// Schedules an instant snap on the next LateUpdate, skipping all smoothing.
        /// Call after teleporting the player to avoid the camera lerping from the old position.
        /// </summary>
        public void SnapToTarget()
        {
            _pitch = 0f;
            _snapNextFrame = true;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float yaw = target.eulerAngles.y;
            Quaternion rotation = Quaternion.Euler(_pitch, yaw, 0f);
            Vector3 back = rotation * new Vector3(0f, 0f, -distance);

            Vector3 desiredPos = target.position + Vector3.up * shoulderHeight + back;

            if (_snapNextFrame)
            {
                transform.position = desiredPos;
                _snapNextFrame = false;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
            }

            Vector3 focusPoint = target.position + Vector3.up * lookAtHeight;
            transform.LookAt(focusPoint);
        }
    }
}
