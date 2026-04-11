using UnityEngine;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.MonoBehaviours.Farming
{
    /// <summary>
    /// Owns FarmSeasonProvider, subscribes to FarmDayClock.OnNewDay,
    /// and notifies FarmLightingController of season changes.
    ///
    /// Attach to the same GameObject as FarmDayClockDriver.
    /// </summary>
    public sealed class FarmSeasonDriver : MonoBehaviour
    {
        [Header("Season Config")]
        [Tooltip("How many in-game days each season lasts.")]
        [SerializeField] private int daysPerSeason = 7;
        [SerializeField] private FarmSeason startSeason = FarmSeason.Spring;

        public static FarmSeasonDriver Instance { get; private set; }
        public FarmSeasonProvider Provider { get; private set; }

        private FarmLightingController _lighting;

        private void Awake()
        {
            Instance = this;
            Provider = new FarmSeasonProvider(daysPerSeason, startSeason);

            Provider.OnSeasonChanged += (prev, next) =>
            {
                Debug.Log($"[FarmSeason] {prev} → {next}");
                _lighting?.ApplySeason(next);
            };
        }

        private void Start()
        {
            _lighting = FindFirstObjectByType<FarmLightingController>();

            // Subscribe to the clock's OnNewDay event.
            if (FarmDayClockDriver.Instance != null)
                FarmDayClockDriver.Instance.Clock.OnNewDay += _ => Provider.OnDayElapsed();
            else
                Debug.LogWarning("[FarmSeasonDriver] FarmDayClockDriver not found — seasons won't advance automatically.");

            // Apply starting season immediately.
            _lighting?.ApplySeason(Provider.Current);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
