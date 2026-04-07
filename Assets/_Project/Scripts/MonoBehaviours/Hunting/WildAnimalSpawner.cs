using UnityEngine;
using FarmSimVR.Core.Hunting;
using System.Collections.Generic;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    public class WildAnimalSpawner : MonoBehaviour
    {
        [SerializeField] private HuntingConfig config;
        [SerializeField] private GameObject[] animalPrefabs;
        [SerializeField] private Transform playerTransform;

        private readonly List<GameObject> _activeAnimals = new();
        private IPlayerInput _playerInput;
        private CaughtAnimalTracker _tracker;
        private float _spawnTimer;

        public int ActiveCount => _activeAnimals.Count;

        public void Initialize(IPlayerInput playerInput, CaughtAnimalTracker tracker)
        {
            _playerInput = playerInput;
            _tracker = tracker;
        }

        private void Update()
        {
            // Clean up destroyed references
            _activeAnimals.RemoveAll(a => a == null);

            if (_activeAnimals.Count >= config.maxWildAnimals) return;

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                SpawnAnimal();
                _spawnTimer = config.spawnInterval;
            }
        }

        private void SpawnAnimal()
        {
            if (animalPrefabs == null || animalPrefabs.Length == 0) return;

            // Pick random point on perimeter
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 spawnPos = new Vector3(
                Mathf.Cos(angle) * config.spawnRadius,
                0f,
                Mathf.Sin(angle) * config.spawnRadius
            );

            int prefabIndex = Random.Range(0, animalPrefabs.Length);
            GameObject animal = Instantiate(animalPrefabs[prefabIndex], spawnPos, Quaternion.identity, transform);

            // Determine animal type from prefab name
            AnimalType type = GuessAnimalType(animalPrefabs[prefabIndex].name);

            // Ensure AnimalWander
            var wander = animal.GetComponent<AnimalWander>();
            if (wander == null)
                wander = animal.AddComponent<AnimalWander>();

            // Add flee behavior
            var flee = animal.GetComponent<AnimalFleeBehavior>();
            if (flee == null)
                flee = animal.AddComponent<AnimalFleeBehavior>();
            flee.Initialize(playerTransform, config.detectionRadius, config.fleeSpeedMultiplier, config.fleeCooldown);

            // Add catch zone
            var catchZone = animal.GetComponent<CatchZone>();
            if (catchZone == null)
                catchZone = animal.AddComponent<CatchZone>();
            catchZone.Initialize(_playerInput, config.catchRadius, type);
            catchZone.OnCaught += HandleAnimalCaught;

            _activeAnimals.Add(animal);
        }

        private void HandleAnimalCaught(CatchZone zone)
        {
            var record = new CaughtAnimalRecord(zone.AnimalType, Time.time);
            _tracker.Catch(record);
            zone.OnCaught -= HandleAnimalCaught;
            _activeAnimals.Remove(zone.gameObject);
            Destroy(zone.gameObject);
            Debug.Log($"[Hunting] Caught a {zone.AnimalType}! Carrying: {_tracker.CarriedCount}");
        }

        private AnimalType GuessAnimalType(string prefabName)
        {
            string lower = prefabName.ToLower();
            if (lower.Contains("chicken")) return AnimalType.Chicken;
            if (lower.Contains("cow")) return AnimalType.Cow;
            if (lower.Contains("horse")) return AnimalType.Horse;
            if (lower.Contains("pig")) return AnimalType.Pig;
            return AnimalType.Sheep;
        }
    }
}
