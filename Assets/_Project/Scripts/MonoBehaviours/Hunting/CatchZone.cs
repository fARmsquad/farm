using UnityEngine;
using FarmSimVR.Core.Hunting;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    public class CatchZone : MonoBehaviour
    {
        [SerializeField] private float catchRadius = 2f;
        [SerializeField] private AnimalType animalType;

        private IPlayerInput _playerInput;

        public event System.Action<CatchZone> OnCaught;

        public AnimalType AnimalType => animalType;

        public void Initialize(IPlayerInput playerInput, float radius, AnimalType type)
        {
            _playerInput = playerInput;
            catchRadius = radius;
            animalType = type;
        }

        private void Update()
        {
            if (_playerInput == null) return;

            float dist = Vector3.Distance(transform.position, _playerInput.Position);

            if (dist <= catchRadius && _playerInput.CatchPressed)
            {
                OnCaught?.Invoke(this);
            }
        }
    }
}
