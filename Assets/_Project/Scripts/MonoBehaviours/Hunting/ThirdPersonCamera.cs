using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    /// <summary>
    /// Fixed-angle follow camera. Position tracks the player smoothly,
    /// but the camera angle never changes — no rotation coupling to the player.
    /// Works like Stardew Valley / Animal Crossing style top-down-ish view.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0, 10, -7);
        [SerializeField] private float smoothTime = 0.2f;

        private Vector3 _velocity;

        private void Start()
        {
            if (target == null) return;

            // Snap to correct position immediately on start (no lerp on first frame)
            transform.position = target.position + offset;
            transform.LookAt(target.position + Vector3.up * 1f);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Follow player position with fixed offset — never rotates with player
            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPosition, ref _velocity, smoothTime);

            // Always look at the player (fixed angle, only adjusts for position changes)
            transform.LookAt(target.position + Vector3.up * 1f);
        }
    }
}
