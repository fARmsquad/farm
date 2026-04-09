using UnityEngine;

namespace FarmSimVR.MonoBehaviours.ChickenGame
{
    public class ChickenCameraFollow : MonoBehaviour
    {
        [SerializeField] public Transform target;
        [SerializeField] public Vector3 offset = new Vector3(0f, 14f, -8f);
        [SerializeField] public float smoothSpeed = 6f;
        [SerializeField] public Vector3 lookAtOffset = new Vector3(0f, 1f, 2f);

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
            transform.LookAt(target.position + lookAtOffset);
        }
    }
}
