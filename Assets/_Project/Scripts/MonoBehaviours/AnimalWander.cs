using UnityEngine;

namespace FarmSimVR.MonoBehaviours
{
    public class AnimalWander : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 0.8f;
        [SerializeField] private float rotationSpeed = 2f;
        [SerializeField] private float wanderRadius = 5f;

        [Header("Timing")]
        [SerializeField] private float minIdleTime = 1f;
        [SerializeField] private float maxIdleTime = 4f;
        [SerializeField] private float minWalkTime = 1f;
        [SerializeField] private float maxWalkTime = 3f;

        [Header("Bounds")]
        [SerializeField] private Vector3 boundsCenter = Vector3.zero;
        [SerializeField] private float boundsRadius = 8f;

        private Vector3 _targetPoint;
        private float _stateTimer;
        private bool _isWalking;
        private Vector3 _startPosition;

        private void Start()
        {
            _startPosition = transform.position;
            PickNewTarget();
            _stateTimer = Random.Range(0f, maxIdleTime); // stagger start
        }

        private void Update()
        {
            _stateTimer -= Time.deltaTime;

            if (_stateTimer <= 0f)
            {
                _isWalking = !_isWalking;
                if (_isWalking)
                {
                    PickNewTarget();
                    _stateTimer = Random.Range(minWalkTime, maxWalkTime);
                }
                else
                {
                    _stateTimer = Random.Range(minIdleTime, maxIdleTime);
                }
            }

            if (_isWalking)
            {
                // Rotate toward target
                Vector3 direction = (_targetPoint - transform.position).normalized;
                direction.y = 0;
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                }

                // Move forward
                transform.position += transform.forward * moveSpeed * Time.deltaTime;

                // Stay on ground
                var pos = transform.position;
                pos.y = 0f;
                transform.position = pos;

                // Clamp to bounds
                Vector3 offset = transform.position - boundsCenter;
                offset.y = 0;
                if (offset.magnitude > boundsRadius)
                {
                    transform.position = boundsCenter + offset.normalized * boundsRadius;
                    PickNewTarget(); // bounce back
                }
            }
        }

        private void PickNewTarget()
        {
            Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
            _targetPoint = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Keep target within bounds
            Vector3 offset = _targetPoint - boundsCenter;
            offset.y = 0;
            if (offset.magnitude > boundsRadius)
                _targetPoint = boundsCenter + offset.normalized * boundsRadius * 0.8f;
        }
    }
}
