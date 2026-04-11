using UnityEngine;
using UnityEngine.Events;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Tracks chaos fill (0–1) driven by player proximity to El Pollo Loco.
    /// Fires threshold events at 0.4 (Dodge) and 0.8 (Tired).
    /// Auto-fills to 1.0 after a configurable timeout (default 60s).
    /// </summary>
    public class ChaosMeter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float autoFillTimeout = 60f;

        [Header("Events")]
        public UnityEvent OnDodgeThreshold = new UnityEvent();
        public UnityEvent OnTiredThreshold = new UnityEvent();
        public UnityEvent OnFull = new UnityEvent();

        // ── Public State ──
        public float CurrentFill { get; private set; }

        // ── Internals ──
        private float elapsedTime;
        private bool dodgeFired;
        private bool tiredFired;
        private bool fullFired;

        private const float DodgeThreshold = 0.4f;
        private const float TiredThreshold = 0.8f;

        private void Update()
        {
            if (CurrentFill >= 1f) return;

            // Auto-fill over time so the gameplay can't stall forever
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= autoFillTimeout && CurrentFill < TiredThreshold)
            {
                AddFill((TiredThreshold - CurrentFill) + 0.01f);
            }

            CheckThresholds();
        }

        /// <summary>
        /// Adds to the fill meter. Clamped to [0, 1].
        /// Called externally by AutoplayIntroScene based on player-rooster proximity.
        /// </summary>
        public void AddFill(float amount)
        {
            if (amount <= 0f) return;
            CurrentFill = Mathf.Clamp01(CurrentFill + amount);
            CheckThresholds();
        }

        /// <summary>
        /// Resets the meter to zero.
        /// </summary>
        public void Reset()
        {
            CurrentFill = 0f;
            elapsedTime = 0f;
            dodgeFired = false;
            tiredFired = false;
            fullFired = false;
        }

        private void CheckThresholds()
        {
            if (!dodgeFired && CurrentFill >= DodgeThreshold)
            {
                dodgeFired = true;
                Debug.Log("[ChaosMeter] Dodge threshold reached (0.4).");
                OnDodgeThreshold?.Invoke();
            }

            if (!tiredFired && CurrentFill >= TiredThreshold)
            {
                tiredFired = true;
                Debug.Log("[ChaosMeter] Tired threshold reached (0.8).");
                OnTiredThreshold?.Invoke();
            }

            if (!fullFired && CurrentFill >= 1f)
            {
                fullFired = true;
                Debug.Log("[ChaosMeter] Meter full.");
                OnFull?.Invoke();
            }
        }
    }
}
