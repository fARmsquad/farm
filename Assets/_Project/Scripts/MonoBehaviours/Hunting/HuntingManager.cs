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

        private void Awake()
        {
            Debug.Log($"[HuntingManager] Awake — spawner={spawner!=null} barn={barnDropOff!=null} hud={hud!=null} input={playerInput!=null} pen={animalPen!=null}");
            _tracker = new CaughtAnimalTracker();
            spawner.Initialize(playerInput, _tracker);
            barnDropOff.Initialize(_tracker);
            hud.Initialize(_tracker, spawner);
            if (animalPen != null)
                animalPen.Initialize(barnDropOff);
            else
                Debug.LogWarning("[HuntingManager] animalPen is NULL — pen won't work!");
        }
    }
}
