using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0, 8, -6);
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private float lookAheadDistance = 2f;

        private void LateUpdate()
        {
            if (target == null) return;

            // Position: follow behind and above the player
            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            // Look at a point slightly ahead of the player
            Vector3 lookTarget = target.position + target.forward * lookAheadDistance + Vector3.up * 1f;
            transform.LookAt(lookTarget);
        }
    }
}
