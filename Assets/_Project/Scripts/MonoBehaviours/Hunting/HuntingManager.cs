using UnityEngine;
using FarmSimVR.Core.Hunting;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    public class HuntingManager : MonoBehaviour
    {
        [SerializeField] private WildAnimalSpawner spawner;
        [SerializeField] private BarnDropOff barnDropOff;
        [SerializeField] private HuntingHUD hud;
        [SerializeField] private KeyboardPlayerInput playerInput;
        [SerializeField] private AnimalPen animalPen;

        private CaughtAnimalTracker _tracker;
        public CaughtAnimalTracker Tracker => _tracker;

        private void Awake()
        {
            _tracker = new CaughtAnimalTracker();
            spawner.Initialize(playerInput, _tracker);
            barnDropOff.Initialize(_tracker);
            hud.Initialize(_tracker, spawner);
            if (animalPen != null)
                animalPen.Initialize(barnDropOff);
        }
    }
}
