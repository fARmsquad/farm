using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    [RequireComponent(typeof(AnimalWander))]
    public class AnimalFleeBehavior : MonoBehaviour
    {
        [SerializeField] private float detectionRadius = 5f;
        [SerializeField] private float fleeSpeedMultiplier = 2.5f;
        [SerializeField] private float fleeCooldown = 3f;

        private AnimalWander _wander;
        private Transform _playerTransform;
        private float _originalSpeed;
        private float _fleeCooldownTimer;
        private bool _isFleeing;

        public bool IsFleeing => _isFleeing;

        public void Initialize(Transform player, float detection, float speedMult, float cooldown)
        {
            _playerTransform = player;
            detectionRadius = detection;
            fleeSpeedMultiplier = speedMult;
            fleeCooldown = cooldown;
        }

        private void Awake()
        {
            _wander = GetComponent<AnimalWander>();
        }

        private void Update()
        {
            if (_playerTransform == null) return;

            float dist = Vector3.Distance(transform.position, _playerTransform.position);

            if (dist <= detectionRadius)
            {
                if (!_isFleeing)
                {
                    _isFleeing = true;
                    // Store and boost speed via reflection-free approach:
                    // We override movement directly in flee mode
                }
                _fleeCooldownTimer = fleeCooldown;
                Flee();
            }
            else if (_isFleeing)
            {
                _fleeCooldownTimer -= Time.deltaTime;
                if (_fleeCooldownTimer <= 0f)
                {
                    _isFleeing = false;
                }
                else
                {
                    Flee();
                }
            }
        }

        private void Flee()
        {
            // Move directly away from player
            Vector3 fleeDir = (transform.position - _playerTransform.position).normalized;
            fleeDir.y = 0;

            if (fleeDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(fleeDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * Time.deltaTime);
            }

            float fleeSpeed = 0.8f * fleeSpeedMultiplier; // base wander speed * multiplier
            transform.position += transform.forward * fleeSpeed * Time.deltaTime;

            // Stay on ground
            var pos = transform.position;
            pos.y = 0f;
            transform.position = pos;
        }
    }
}
