using UnityEngine;
using UnityEngine.Events;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// 3-phase rooster controller for the intro gameplay sequence.
    /// Phase transitions are driven externally by <see cref="ChaosMeter"/> fill level:
    ///   Normal  (fill &lt; 0.4) — wander idly inside the pen
    ///   Dodge   (0.4 ≤ fill &lt; 0.8) — actively dodge away from the player
    ///   Tired   (fill ≥ 0.8) — slowed, catchable
    ///
    /// Attach to the El Pollo Loco GameObject spawned by AutoplayIntroScene.
    /// </summary>
    public class ElPolloController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float wanderSpeed = 2f;
        [SerializeField] private float dodgeSpeed = 5f;
        [SerializeField] private float tiredSpeed = 0.8f;
        [SerializeField] private float wanderRadius = 6f;

        [Header("Dodge")]
        [SerializeField] private float dodgeDistance = 4f;
        [SerializeField] private float dodgeCooldown = 0.6f;

        [Header("Phase Thresholds")]
        [SerializeField] private float dodgeThreshold = 0.4f;
        [SerializeField] private float tiredThreshold = 0.8f;

        [Header("Events")]
        public UnityEvent OnCaught = new UnityEvent();

        // ── Public State ──
        public ElPolloPhase CurrentPhase { get; private set; } = ElPolloPhase.Normal;
        public bool IsCatchable => CurrentPhase == ElPolloPhase.Tired;

        // ── Internals ──
        private Vector3 penCenter;
        private float penRadius;
        private Vector3 wanderTarget;
        private float wanderTimer;
        private float dodgeCooldownTimer;
        private bool caught;

        private const float WanderInterval = 2.5f;

        private void Start()
        {
            penCenter = transform.position;
            penRadius = wanderRadius;
            PickNewWanderTarget();
        }

        private void Update()
        {
            if (caught) return;

            dodgeCooldownTimer -= Time.deltaTime;

            switch (CurrentPhase)
            {
                case ElPolloPhase.Normal:
                    Wander();
                    break;
                case ElPolloPhase.Dodge:
                    Wander(); // still wanders between dodges
                    break;
                case ElPolloPhase.Tired:
                    WanderSlow();
                    break;
            }
        }

        /// <summary>
        /// Called each frame by AutoplayIntroScene to set the phase based on ChaosMeter fill.
        /// </summary>
        public void UpdateFromChaosMeter(float fill)
        {
            if (caught) return;

            if (fill >= tiredThreshold)
                CurrentPhase = ElPolloPhase.Tired;
            else if (fill >= dodgeThreshold)
                CurrentPhase = ElPolloPhase.Dodge;
            else
                CurrentPhase = ElPolloPhase.Normal;
        }

        /// <summary>
        /// Immediately move away from the given world position. Called when the player
        /// gets close during the Dodge phase.
        /// </summary>
        public void DodgeAwayFrom(Vector3 threatPosition)
        {
            if (caught || dodgeCooldownTimer > 0f) return;

            Vector3 away = FlatDirection(transform.position - threatPosition);
            if (away.sqrMagnitude < 0.001f)
                away = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;

            // Add random angle offset for unpredictability
            away = Quaternion.AngleAxis(Random.Range(-30f, 30f), Vector3.up) * away;

            Vector3 target = transform.position + away * dodgeDistance;
            target = ClampToPen(target);
            target.y = GetTerrainHeight(target);

            transform.position = target;
            transform.forward = away;

            dodgeCooldownTimer = dodgeCooldown;
        }

        /// <summary>
        /// Catch El Pollo Loco. Only succeeds when <see cref="IsCatchable"/> is true.
        /// </summary>
        public void Catch()
        {
            if (caught) return;

            caught = true;
            CurrentPhase = ElPolloPhase.Tired;

            Debug.Log("[ElPolloController] El Pollo Loco has been caught!");
            OnCaught?.Invoke();
        }

        // ── Private Helpers ──

        private void Wander()
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f) PickNewWanderTarget();

            MoveToward(wanderTarget, wanderSpeed);
        }

        private void WanderSlow()
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f) PickNewWanderTarget();

            MoveToward(wanderTarget, tiredSpeed);
        }

        private void MoveToward(Vector3 target, float speed)
        {
            Vector3 dir = FlatDirection(target - transform.position);
            if (dir.sqrMagnitude < 0.001f) return;

            Vector3 newPos = transform.position + dir * speed * Time.deltaTime;
            newPos = ClampToPen(newPos);
            newPos.y = GetTerrainHeight(newPos);
            transform.position = newPos;

            if (dir.sqrMagnitude > 0.001f)
                transform.forward = Vector3.Lerp(transform.forward, dir, 10f * Time.deltaTime);
        }

        private void PickNewWanderTarget()
        {
            Vector2 rnd = Random.insideUnitCircle * (penRadius * 0.7f);
            wanderTarget = penCenter + new Vector3(rnd.x, 0f, rnd.y);
            wanderTarget.y = GetTerrainHeight(wanderTarget);
            wanderTimer = WanderInterval + Random.Range(-0.8f, 0.8f);
        }

        private Vector3 ClampToPen(Vector3 pos)
        {
            Vector3 offset = pos - penCenter;
            offset.y = 0f;
            if (offset.magnitude > penRadius)
            {
                offset = offset.normalized * penRadius;
                pos = penCenter + offset;
            }
            return pos;
        }

        private static float GetTerrainHeight(Vector3 pos)
        {
            if (Terrain.activeTerrain != null)
                return Terrain.activeTerrain.SampleHeight(pos) + 0.5f;
            return 0.5f;
        }

        private static Vector3 FlatDirection(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.001f ? v.normalized : Vector3.zero;
        }
    }
}
