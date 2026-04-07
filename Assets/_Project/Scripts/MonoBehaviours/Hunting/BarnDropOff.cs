using UnityEngine;
using System.Collections.Generic;
using FarmSimVR.Core.Hunting;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    [RequireComponent(typeof(Collider))]
    public class BarnDropOff : MonoBehaviour
    {
        private CaughtAnimalTracker _tracker;

        public event System.Action<int> OnDeposit;
        public event System.Action<IReadOnlyList<CaughtAnimalRecord>> OnAnimalsDeposited;

        public void Initialize(CaughtAnimalTracker tracker)
        {
            _tracker = tracker;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_tracker == null || _tracker.CarriedCount == 0) return;

            if (other.GetComponent<IPlayerInput>() == null &&
                other.GetComponentInParent<IPlayerInput>() == null) return;

            var deposited = _tracker.DepositAll();
            Debug.Log($"[BarnDropOff] Deposited {deposited.Count} animals.");
            FarmSimVR.MonoBehaviours.Diagnostics.GameStateLogger.Instance?.LogEvent($"Deposited {deposited.Count} animals at barn");
            OnDeposit?.Invoke(deposited.Count);
            OnAnimalsDeposited?.Invoke(deposited);
        }
    }
}
