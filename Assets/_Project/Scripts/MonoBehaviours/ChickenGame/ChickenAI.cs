using UnityEngine;

namespace FarmSimVR.MonoBehaviours.ChickenGame
{
    public class ChickenAI : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] public float fleeRadius = 7f;
        [SerializeField] public float panicRadius = 3f;
        [SerializeField] public float catchRadius = 1.5f;

        [Header("Speed")]
        [SerializeField] public float wanderSpeed = 1.5f;
        [SerializeField] public float fleeSpeed = 4.2f;
        [SerializeField] public float panicSpeed = 5.8f;

        [Header("Behaviour")]
        [SerializeField] public float arenaRadius = 10f;
        [SerializeField] public float erraticnessDegrees = 25f;
        [SerializeField] public float panicErraticnessDegrees = 50f;

        [Header("Physics")]
        [SerializeField] public float gravity = -20f;

        private Transform _player;
        private Vector3 _wanderTarget;
        private float _wanderTimer;
        private float _verticalVelocity;
        private const float WanderIntervalBase = 2.5f;

        private void Start()
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) _player = playerGO.transform;
            PickNewWanderTarget();
        }

        private void Update()
        {
            if (_player == null) return;

            float dist = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(_player.position.x, 0, _player.position.z)
            );

            if (dist < panicRadius)
                RunPanic();
            else if (dist < fleeRadius)
                RunFlee();
            else
                Wander();

            ApplyGravity();
            ClampToArena();
        }

        private void RunFlee()
        {
            Vector3 away = FlatDir(transform.position - _player.position);
            away = Quaternion.AngleAxis(
                Random.Range(-erraticnessDegrees, erraticnessDegrees), Vector3.up
            ) * away;
            MoveInDirection(away, fleeSpeed);
        }

        private void RunPanic()
        {
            Vector3 away = FlatDir(transform.position - _player.position);
            away = Quaternion.AngleAxis(
                Random.Range(-panicErraticnessDegrees, panicErraticnessDegrees), Vector3.up
            ) * away;
            MoveInDirection(away, panicSpeed);
        }

        private void Wander()
        {
            _wanderTimer -= Time.deltaTime;
            if (_wanderTimer <= 0f) PickNewWanderTarget();

            Vector3 dir = FlatDir(_wanderTarget - transform.position);
            if (dir.sqrMagnitude > 0.01f)
                MoveInDirection(dir, wanderSpeed);
        }

        private void MoveInDirection(Vector3 dir, float speed)
        {
            dir.y = 0;
            if (dir.sqrMagnitude < 0.001f) return;
            dir.Normalize();
            transform.position += dir * speed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                15f * Time.deltaTime
            );
        }

        private void PickNewWanderTarget()
        {
            float r = Random.Range(0f, arenaRadius - 1.5f);
            float a = Random.Range(0f, Mathf.PI * 2f);
            _wanderTarget = new Vector3(
                Mathf.Sin(a) * r,
                transform.position.y,
                Mathf.Cos(a) * r
            );
            _wanderTimer = WanderIntervalBase + Random.Range(-0.8f, 0.8f);
        }

        private void ApplyGravity()
        {
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2f))
            {
                if (transform.position.y > hit.point.y + 0.02f)
                {
                    _verticalVelocity += gravity * Time.deltaTime;
                    transform.position += Vector3.up * _verticalVelocity * Time.deltaTime;
                }
                else
                {
                    _verticalVelocity = 0f;
                    var p = transform.position;
                    p.y = hit.point.y;
                    transform.position = p;
                }
            }
        }

        private void ClampToArena()
        {
            Vector3 pos = transform.position;
            Vector2 flat = new Vector2(pos.x, pos.z);
            if (flat.magnitude > arenaRadius)
            {
                flat = flat.normalized * arenaRadius;
                pos.x = flat.x;
                pos.z = flat.y;
            }
            transform.position = pos;
        }

        private static Vector3 FlatDir(Vector3 v)
        {
            v.y = 0;
            return v.sqrMagnitude > 0.001f ? v.normalized : Vector3.forward;
        }
    }
}
