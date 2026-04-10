using UnityEngine;
using UnityEngine.Events;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    public enum ElPolloPhase { Normal, Dodge, Tired }

    /// <summary>
    /// Controls El Pollo Loco's 3-phase chase behaviour.
    /// Phases: Normal → Dodge (chaos >= 0.4) → Tired (chaos >= 0.8 or timeout).
    /// </summary>
    public class ElPolloController : MonoBehaviour
    {
        private const float NormalSpeed  = 3.5f;
        private const float DodgeSpeed   = 6.0f;
        private const float TiredSpeed   = 1.0f;
        private const float DodgeDistance = 4.0f;
        private const float CatchThreshold = 0.8f;
        private const float ChaosDodgeThreshold = 0.4f;
        private const float ChaosTiredThreshold = 0.8f;

        public ElPolloPhase CurrentPhase { get; private set; } = ElPolloPhase.Normal;
        public bool IsCatchable => CurrentPhase == ElPolloPhase.Tired;

        public UnityEvent OnCaught = new UnityEvent();

        private bool caught;

        /// <summary>Updates phase based on the current chaos meter fill (0–1).</summary>
        public void UpdateFromChaosMeter(float fill)
        {
            if (caught) return;

            if (fill >= ChaosTiredThreshold)
                CurrentPhase = ElPolloPhase.Tired;
            else if (fill >= ChaosDodgeThreshold)
                CurrentPhase = ElPolloPhase.Dodge;
            else
                CurrentPhase = ElPolloPhase.Normal;

            float speed = CurrentPhase switch
            {
                ElPolloPhase.Dodge => DodgeSpeed,
                ElPolloPhase.Tired => TiredSpeed,
                _                  => NormalSpeed
            };

            // Idle wander — replace with proper pathfinding when NavMesh is available
            transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.Self);
        }

        /// <summary>Dodges away from a threat position. Call during Dodge phase.</summary>
        public void DodgeAwayFrom(Vector3 threatPosition)
        {
            if (caught) return;

            Vector3 away = (transform.position - threatPosition).normalized;
            away.y = 0f;
            transform.position += away * DodgeDistance * Time.deltaTime;
            transform.forward   = away;
        }

        /// <summary>Triggers the catch — fires OnCaught and disables movement.</summary>
        public void Catch()
        {
            if (caught) return;
            caught = true;
            CurrentPhase = ElPolloPhase.Tired;
            OnCaught.Invoke();
        }

        /// <summary>Immediately forces the Tired phase (e.g. on timeout).</summary>
        public void ForceTired()
        {
            if (caught) return;
            CurrentPhase = ElPolloPhase.Tired;
        }
    }
}
