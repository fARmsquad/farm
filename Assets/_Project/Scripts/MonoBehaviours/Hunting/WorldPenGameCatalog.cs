using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    [CreateAssetMenu(fileName = "WorldPenGameCatalog", menuName = "FarmSimVR/World Pen Game Catalog")]
    public sealed class WorldPenGameCatalog : ScriptableObject
    {
        [SerializeField] private HuntingConfig huntingConfig;
        [SerializeField] private GameObject[] wildAnimalPrefabs;
        [SerializeField] private PenAnimalEntry[] penAnimalPrefabs;

        public HuntingConfig HuntingConfig => huntingConfig;
        public GameObject[] WildAnimalPrefabs => wildAnimalPrefabs;
        public PenAnimalEntry[] PenAnimalPrefabs => penAnimalPrefabs;
    }
}
