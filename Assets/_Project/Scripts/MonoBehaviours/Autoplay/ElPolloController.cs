using UnityEngine;
using UnityEngine.Events;

namespace FarmSimVR.MonoBehaviours.Autoplay
{
    public class ElPolloController : MonoBehaviour
    {
        private static readonly Vector3 DefaultPenCenter = new(72f, 0.5f, 24f);

        [SerializeField] private Vector3 penCenter = DefaultPenCenter;
        [SerializeField] private float penRadius = 8f;
        [SerializeField] private float wanderSpeed = 1.2f;
        [SerializeField] private float alertSpeed = 2.2f;
        [SerializeField] private float dodgeSpeed = 4f;
        [SerializeField] private float dodgeDistance = 3f;
        [SerializeField] private float targetTolerance = 0.35f;

        public UnityEvent OnCaught = new();

        public ElPolloPhase CurrentPhase { get; private set; } = ElPolloPhase.Wander;

        public bool IsCatchable => !_isCaught && CurrentPhase == ElPolloPhase.Tired;

        private Vector3 _moveTarget;
        private bool _hasMoveTarget;
        private bool _isCaught;

        private void Start()
        {
            PickRandomTarget();
            SnapToTerrain();
        }

        private void Update()
        {
            if (_isCaught)
                return;

            if (IsCatchable)
            {
                SnapToTerrain();
                return;
            }

            if (!_hasMoveTarget || HasReachedTarget())
                PickRandomTarget();

            MoveTowardsTarget(GetSpeedForPhase());
        }

        public void UpdateFromChaosMeter(float fill)
        {
            if (_isCaught)
                return;

            CurrentPhase = fill switch
            {
                >= 0.85f => ElPolloPhase.Tired,
                >= 0.55f => ElPolloPhase.Dodge,
                >= 0.25f => ElPolloPhase.Alert,
                _ => ElPolloPhase.Wander
            };

            if (CurrentPhase == ElPolloPhase.Tired)
                _hasMoveTarget = false;
        }

        public void DodgeAwayFrom(Vector3 fromPosition)
        {
            if (_isCaught || CurrentPhase != ElPolloPhase.Dodge)
                return;

            Vector3 away = transform.position - fromPosition;
            away.y = 0f;

            if (away.sqrMagnitude < 0.001f)
                away = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));

            _moveTarget = ClampToPen(transform.position + (away.normalized * dodgeDistance));
            _hasMoveTarget = true;
        }

        public void Catch()
        {
            if (!IsCatchable || _isCaught)
                return;

            _isCaught = true;
            OnCaught?.Invoke();
        }

        private float GetSpeedForPhase()
        {
            return CurrentPhase switch
            {
                ElPolloPhase.Alert => alertSpeed,
                ElPolloPhase.Dodge => dodgeSpeed,
                _ => wanderSpeed
            };
        }

        private bool HasReachedTarget()
        {
            Vector3 current = transform.position;
            current.y = 0f;

            Vector3 target = _moveTarget;
            target.y = 0f;

            return Vector3.Distance(current, target) <= targetTolerance;
        }

        private void PickRandomTarget()
        {
            Vector2 offset = Random.insideUnitCircle * penRadius;
            _moveTarget = ClampToPen(penCenter + new Vector3(offset.x, 0f, offset.y));
            _hasMoveTarget = true;
        }

        private void MoveTowardsTarget(float speed)
        {
            Vector3 current = transform.position;
            Vector3 target = _moveTarget;
            target.y = current.y;

            Vector3 next = Vector3.MoveTowards(current, target, speed * Time.deltaTime);
            next.y = SampleTerrainHeight(next, current.y);
            transform.position = next;

            Vector3 facing = target - current;
            facing.y = 0f;

            if (facing.sqrMagnitude > 0.001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(facing.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, 10f * Time.deltaTime);
            }

            if (HasReachedTarget())
                _hasMoveTarget = false;
        }

        private void SnapToTerrain()
        {
            Vector3 position = transform.position;
            position.y = SampleTerrainHeight(position, position.y);
            transform.position = position;
        }

        private Vector3 ClampToPen(Vector3 candidate)
        {
            Vector3 offset = candidate - penCenter;
            offset.y = 0f;

            if (offset.magnitude > penRadius)
                offset = offset.normalized * penRadius;

            Vector3 clamped = penCenter + offset;
            clamped.y = SampleTerrainHeight(clamped, transform.position.y);
            return clamped;
        }

        private static float SampleTerrainHeight(Vector3 position, float fallbackY)
        {
            if (Terrain.activeTerrain == null)
                return fallbackY;

            return Terrain.activeTerrain.SampleHeight(position) + 0.5f;
        }
    }
}
