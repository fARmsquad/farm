using UnityEngine;
using FarmSimVR.Core.Hunting;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    public class PenAnimal : MonoBehaviour
    {
        public AnimalType AnimalType { get; private set; }
        public float DepositTime { get; private set; }

        public void Initialize(AnimalType type, float depositTime)
        {
            AnimalType = type;
            DepositTime = depositTime;
        }
    }
}
