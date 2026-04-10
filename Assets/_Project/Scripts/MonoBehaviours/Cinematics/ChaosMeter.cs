using UnityEngine;
using UnityEngine.Events;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Tracks the chaos fill value (0–1) during the El Pollo chase.
    /// Fires threshold events at 0.4 (Dodge) and 0.8 (Tired).
    /// Auto-fills to the Tired threshold after a 60s timeout.
    /// </summary>
    public class ChaosMeter : MonoBehaviour
    {
        private const float DodgeThreshold = 0.4f;
        private const float TiredThreshold = 0.8f;
        private const float TimeoutSeconds = 60f;

        public float CurrentFill { get; private set; }

        public UnityEvent OnDodgeThresholdReached = new UnityEvent();
        public UnityEvent OnTiredThresholdReached  = new UnityEvent();

        private bool dodgeEventFired;
        private bool tiredEventFired;
        private float elapsed;

        private void Update()
        {
            elapsed += Time.deltaTime;

            // Force Tired threshold after timeout
            if (elapsed >= TimeoutSeconds && CurrentFill < TiredThreshold)
            {
                CurrentFill = TiredThreshold;
                FireThresholdEvents();
            }
        }

        /// <summary>Adds fill amount per frame (already multiplied by Time.deltaTime by caller).</summary>
        public void AddFill(float amount)
        {
            CurrentFill = Mathf.Clamp01(CurrentFill + amount);
            FireThresholdEvents();
        }

        private void FireThresholdEvents()
        {
            if (!dodgeEventFired && CurrentFill >= DodgeThreshold)
            {
                dodgeEventFired = true;
                OnDodgeThresholdReached.Invoke();
            }

            if (!tiredEventFired && CurrentFill >= TiredThreshold)
            {
                tiredEventFired = true;
                OnTiredThresholdReached.Invoke();
            }
        }
    }
}
