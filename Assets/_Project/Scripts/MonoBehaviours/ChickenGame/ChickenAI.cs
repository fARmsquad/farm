using UnityEngine;

namespace FarmSimVR.MonoBehaviours.ChickenGame
{
    [RequireComponent(typeof(CharacterController))]
    public class ChickenAI : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] public float fleeRadius = 7f;
        [SerializeField] public float panicRadius = 3f;
        [SerializeField] public float catchRadius = 1.5f;

        [Header("Speed")]
        [SerializeField] public float wanderSpeed = 1.65f;
        [SerializeField] public float fleeSpeed = 4.62f;
        [SerializeField] public float panicSpeed = 6.38f;

        [Header("Hop (visual child)")]
        [SerializeField] private float hopAmplitude = 0.1f;
        [SerializeField] private float hopFrequencyPerUnitSpeed = 18f;
        [SerializeField] private float hopSpeedThreshold = 0.08f;

        [Header("Behaviour")]
        [SerializeField] public float arenaRadius = 10f;
        [SerializeField] public float erraticnessDegrees = 25f;
        [SerializeField] public float panicErraticnessDegrees = 50f;

        [Header("Stun")]
        [SerializeField] public float stunnedDuration = 0.8f;

        [Header("Physics")]
        [SerializeField] public float gravity = -20f;

        /// <summary>True when the chicken is stunned after being cornered against the arena boundary while being chased.</summary>
        public bool IsStunned => _stunnedTimer > 0f;

        /// <summary>True while the player is holding the chicken.</summary>
        public bool IsCaught { get; private set; }

        private CharacterController _cc;
        private Transform _player;
        private Transform _carriedBy;
        private Vector3 _wanderTarget;
        private float _wanderTimer;
        private float _verticalVelocity;
        private float _stunnedTimer;
        private float _currentSpeed;           // Smoothed speed — avoids snapping between wander/flee/panic
        private Vector3 _currentMoveDir;       // Smoothed direction — removes erratic jitter on direction changes
        private const float WanderIntervalBase = 2.5f;
        private const float RotationSpeed = 15f;
        private const float SpeedSmoothTime = 0.15f;
        private const float DirectionSmoothTime = 0.08f;
        private const float CarryLerpSpeed = 18f;

        private Transform _visual;
        private Vector3 _visualBaseLocalPos;
        private float _hopPhase;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (transform.childCount > 0)
            {
                _visual = transform.GetChild(0);
                _visualBaseLocalPos = _visual.localPosition;
            }
        }

        private void Start()
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) _player = playerGO.transform;
            PickNewWanderTarget();
        }

        private void LateUpdate()
        {
            if (_visual == null)
                return;
            if (IsCaught || IsStunned || _player == null)
            {
                ResetVisualBob();
                return;
            }

            if (_currentSpeed < hopSpeedThreshold)
            {
                ResetVisualBob();
                return;
            }

            _hopPhase += _currentSpeed * hopFrequencyPerUnitSpeed * Time.deltaTime;
            float y = Mathf.Sin(_hopPhase) * hopAmplitude;
            _visual.localPosition = _visualBaseLocalPos + new Vector3(0f, y, 0f);
        }

        private void Update()
        {
            if (IsCaught)
            {
                UpdateCarriedPosition();
                return;
            }

            if (_player == null) return;

            // While stunned, only apply gravity and count down the timer
            if (_stunnedTimer > 0f)
            {
                _stunnedTimer -= Time.deltaTime;
                ApplyGravityOnly();
                return;
            }

            float dx     = transform.position.x - _player.position.x;
            float dz     = transform.position.z - _player.position.z;
            float sqDist = dx * dx + dz * dz;

            Vector3 moveDir;
            float speed;
            bool isChased;

            if (sqDist < panicRadius * panicRadius)
            {
                moveDir  = PanicDirection();
                speed    = panicSpeed;
                isChased = true;
            }
            else if (sqDist < fleeRadius * fleeRadius)
            {
                moveDir  = FleeDirection();
                speed    = fleeSpeed;
                isChased = true;
            }
            else
            {
                moveDir  = WanderDirection();
                speed    = wanderSpeed;
                isChased = false;
            }

            ApplyMovement(moveDir, speed);
            ClampToArena(isChased);
        }

        /// <summary>Resets transient runtime state. Call this when the game restarts.</summary>
        public void ResetState()
        {
            IsCaught          = false;
            _carriedBy        = null;
            _cc.enabled       = true;
            _stunnedTimer     = 0f;
            _verticalVelocity = 0f;
            _currentSpeed     = 0f;
            _currentMoveDir   = Vector3.zero;
            PickNewWanderTarget();
            ResetVisualBob();
        }

        /// <summary>Attaches the chicken to the player's carry position and disables physics movement.</summary>
        public void Catch(Transform carrier)
        {
            IsCaught      = true;
            _carriedBy    = carrier;
            _stunnedTimer = 0f;
            _cc.enabled   = false;
            ResetVisualBob();
        }

        /// <summary>Releases the chicken and triggers a frightened flee burst.</summary>
        public void Escape()
        {
            IsCaught      = false;
            _carriedBy    = null;
            _cc.enabled   = true;
            _stunnedTimer = 0.3f;
            PickNewWanderTarget();
        }

        /// <summary>Drops the chicken silently in place. Used when the game ends so the CC is properly re-enabled.</summary>
        public void Drop()
        {
            IsCaught   = false;
            _carriedBy = null;
            _cc.enabled = true;
        }

        /// <summary>Returns the flee direction with erratic angle offset applied.</summary>
        private Vector3 FleeDirection()
        {
            Vector3 away = FlatDir(transform.position - _player.position);
            return Quaternion.AngleAxis(
                Random.Range(-erraticnessDegrees, erraticnessDegrees), Vector3.up
            ) * away;
        }

        /// <summary>Returns the panic direction with a wider erratic angle offset applied.</summary>
        private Vector3 PanicDirection()
        {
            Vector3 away = FlatDir(transform.position - _player.position);
            return Quaternion.AngleAxis(
                Random.Range(-panicErraticnessDegrees, panicErraticnessDegrees), Vector3.up
            ) * away;
        }

        /// <summary>Returns the wander direction toward the current wander target, picking a new one as needed.</summary>
        private Vector3 WanderDirection()
        {
            _wanderTimer -= Time.deltaTime;
            if (_wanderTimer <= 0f) PickNewWanderTarget();
            return FlatDir(_wanderTarget - transform.position);
        }

        /// <summary>Moves the chicken in the given direction at the given speed using the CharacterController.</summary>
        private void ApplyMovement(Vector3 dir, float targetSpeed)
        {
            if (dir.sqrMagnitude < 0.001f) return;

            // Smooth speed transitions to avoid jarring jumps between wander/flee/panic
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, (targetSpeed / SpeedSmoothTime) * Time.deltaTime);

            // Smooth direction to reduce erratic jitter from per-frame random angle offsets
            _currentMoveDir = Vector3.MoveTowards(_currentMoveDir, dir, (1f / DirectionSmoothTime) * Time.deltaTime);
            if (_currentMoveDir.sqrMagnitude < 0.001f) _currentMoveDir = dir;

            if (_cc.isGrounded)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = _currentMoveDir * _currentSpeed;
            velocity.y = _verticalVelocity;
            _cc.Move(velocity * Time.deltaTime);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(_currentMoveDir),
                RotationSpeed * Time.deltaTime
            );
        }

        /// <summary>Applies gravity only, used when the chicken is stunned and shouldn't move horizontally.</summary>
        private void ApplyGravityOnly()
        {
            if (_cc.isGrounded)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += gravity * Time.deltaTime;

            _cc.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
        }

        private void PickNewWanderTarget()
        {
            float r = Random.Range(0f, arenaRadius - 1.5f);
            float a = Random.Range(0f, Mathf.PI * 2f);
            _wanderTarget = new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r);
            _wanderTimer  = WanderIntervalBase + Random.Range(-0.8f, 0.8f);
        }

        /// <summary>Clamps the chicken inside the arena boundary. Triggers a stun when the boundary is hit while being chased.</summary>
        private void ClampToArena(bool isChased)
        {
            Vector3 pos   = transform.position;
            float flatX   = pos.x;
            float flatZ   = pos.z;
            float sqMag   = flatX * flatX + flatZ * flatZ;

            if (sqMag > arenaRadius * arenaRadius)
            {
                float invMag = arenaRadius / Mathf.Sqrt(sqMag);
                pos.x = flatX * invMag;
                pos.z = flatZ * invMag;

                // Disable the CharacterController before writing transform.position directly.
                // Writing position while the CC is active desyncs its internal state and causes
                // it to behave as a solid invisible wall on the next frame.
                _cc.enabled = false;
                transform.position = pos;
                _cc.enabled = true;

                if (isChased)
                    _stunnedTimer = stunnedDuration;
            }
        }

        private static Vector3 FlatDir(Vector3 v)
        {
            v.y = 0;
            return v.sqrMagnitude > 0.001f ? v.normalized : Vector3.forward;
        }

        /// <summary>Smoothly moves the chicken to the carry position in front of and above the player.</summary>
        private void UpdateCarriedPosition()
        {
            if (_carriedBy == null) return;
            Vector3 target = _carriedBy.position + _carriedBy.forward * 0.5f + Vector3.up * 0.7f;
            transform.position = Vector3.Lerp(transform.position, target, CarryLerpSpeed * Time.deltaTime);
        }

        private void ResetVisualBob()
        {
            if (_visual == null)
                return;
            _hopPhase = 0f;
            _visual.localPosition = _visualBaseLocalPos;
        }
    }
}
