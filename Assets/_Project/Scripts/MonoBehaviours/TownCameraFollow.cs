using UnityEngine;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Smooth third-person camera that follows a target with a fixed offset.
    /// Used in the town interaction scene.
    /// </summary>
    public class TownCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 3.5f, -6f);
        [SerializeField] private float smoothSpeed = 4f;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
            transform.LookAt(target.position + Vector3.up);
        }
    }
}
