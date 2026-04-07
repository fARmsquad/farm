using UnityEngine;
using FarmSimVR.Core.Hunting;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    [RequireComponent(typeof(Collider))]
    public class BarnDropOff : MonoBehaviour
    {
        private CaughtAnimalTracker _tracker;

        public event System.Action<int> OnDeposit;

        public void Initialize(CaughtAnimalTracker tracker)
        {
            _tracker = tracker;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_tracker == null || _tracker.CarriedCount == 0) return;

            // Check if it's the player
            if (other.GetComponent<IPlayerInput>() == null &&
                other.GetComponentInParent<IPlayerInput>() == null) return;

            int deposited = _tracker.DepositAll();
            Debug.Log($"[Hunting] Deposited {deposited} animals at barn! Total in barn: {_tracker.DepositedCount}");
            OnDeposit?.Invoke(deposited);
        }
    }
}
