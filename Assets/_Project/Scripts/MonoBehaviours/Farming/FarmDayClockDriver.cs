using UnityEngine;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.MonoBehaviours.Farming
{
    /// <summary>
    /// Owns the FarmDayClock, ticks it every frame, and broadcasts phase
    /// changes to FarmLightingController. FarmSimDriver reads the clock via
    /// the static Instance accessor to feed GrowthConditions.
    /// </summary>
    public sealed class FarmDayClockDriver : MonoBehaviour
    {
        [Header("Time")]
        [Tooltip("Real seconds per full in-game day.")]
        [SerializeField] private float realSecondsPerDay  = 120f;
        [Tooltip("Starting normalised time. 0.35 = morning sun, 0.5 = noon.")]
        [SerializeField] [Range(0f, 1f)] private float startTime = 0.35f;

        [Header("References")]
        [SerializeField] private FarmLightingController lighting;

        public static FarmDayClockDriver Instance { get; private set; }
        public FarmDayClock Clock { get; private set; }

        private void Awake()
        {
            TryResolveLighting();
            Instance = this;
            Clock    = new FarmDayClock(realSecondsPerDay, startTime);

            Clock.OnPhaseChanged += (_, next) =>
                Debug.Log($"[FarmDayClock] Phase → {next}  (Day {Clock.DayCount + 1})");

            Clock.OnNewDay += day =>
                Debug.Log($"[FarmDayClock] New day — Day {day + 1}");

            // Apply starting time immediately
            lighting?.ApplyTime(Clock.NormalisedTime);
        }

        private void Update()
        {
            TryResolveLighting();
            Clock.Tick(Time.deltaTime);
            lighting?.ApplyTime(Clock.NormalisedTime);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void TryResolveLighting()
        {
            if (lighting == null)
                lighting = FindAnyObjectByType<FarmLightingController>();
        }
    }
}
