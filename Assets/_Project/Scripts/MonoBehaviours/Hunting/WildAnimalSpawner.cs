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
        [SerializeField] private Transform spawnCenter;

        private readonly List<GameObject> _activeAnimals = new();
        private IPlayerInput _playerInput;
        private CaughtAnimalTracker _tracker;
        private float _spawnTimer;
        private float _catchRadiusMultiplier = 1f;
        private float _fleeSpeedScale = 1f;

        public int ActiveCount => _activeAnimals.Count;

        public void Initialize(IPlayerInput playerInput, CaughtAnimalTracker tracker)
        {
            _playerInput = playerInput;
            _tracker = tracker;
            _spawnTimer = 0f;
        }

        public void Configure(
            HuntingConfig huntingConfig,
            GameObject[] prefabs,
            Transform player,
            Transform center,
            float catchRadiusMultiplier = 1f,
            float fleeSpeedScale = 1f)
        {
            config = huntingConfig;
            animalPrefabs = prefabs;
            playerTransform = player;
            spawnCenter = center;
            _catchRadiusMultiplier = catchRadiusMultiplier <= 0f ? 1f : catchRadiusMultiplier;
            _fleeSpeedScale = fleeSpeedScale <= 0f ? 1f : fleeSpeedScale;
        }

        public GameObject TrySpawnNow()
        {
            if (config == null || animalPrefabs == null || animalPrefabs.Length == 0)
                return null;

            if (_activeAnimals.Count >= config.maxWildAnimals)
                return null;

            return SpawnAnimal();
        }

        public void ClearActiveAnimals()
        {
            for (var i = 0; i < _activeAnimals.Count; i++)
            {
                if (_activeAnimals[i] != null)
                    DestroyAnimal(_activeAnimals[i]);
            }

            _activeAnimals.Clear();
        }

        private void Update()
        {
            // Clean up destroyed references
            _activeAnimals.RemoveAll(a => a == null);

            if (config == null || animalPrefabs == null || animalPrefabs.Length == 0)
                return;

            if (_activeAnimals.Count >= config.maxWildAnimals) return;

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                TrySpawnNow();
                _spawnTimer = config.spawnInterval;
            }
        }

        private GameObject SpawnAnimal()
        {
            // Pick random point on perimeter, avoiding the pen area
            Vector3 spawnPos;
            int attempts = 0;
            var origin = ResolveSpawnOrigin();
            do
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                spawnPos = origin + new Vector3(
                    Mathf.Cos(angle) * config.spawnRadius,
                    0f,
                    Mathf.Sin(angle) * config.spawnRadius
                );
                attempts++;
            } while (IsInsidePen(spawnPos) && attempts < 10);

            int prefabIndex = Random.Range(0, animalPrefabs.Length);
            GameObject animal = Instantiate(animalPrefabs[prefabIndex], spawnPos, Quaternion.identity, transform);
            animal.SetActive(true);

            // Determine animal type from prefab name
            AnimalType type = GuessAnimalType(animalPrefabs[prefabIndex].name);

            // Ensure AnimalWander — keep wild animals out of the pen
            var wander = animal.GetComponent<AnimalWander>();
            if (wander == null)
                wander = animal.AddComponent<AnimalWander>();
            var pen = FindAnyObjectByType<AnimalPen>();
            if (pen != null)
                wander.SetExclusionZone(pen.PenCenter, pen.PenRadius + 1f);

            // Add flee behavior
            var flee = animal.GetComponent<AnimalFleeBehavior>();
            if (flee == null)
                flee = animal.AddComponent<AnimalFleeBehavior>();
            flee.Initialize(playerTransform, config.detectionRadius, config.fleeSpeedMultiplier * _fleeSpeedScale, config.fleeCooldown);

            // Add catch zone
            var catchZone = animal.GetComponent<CatchZone>();
            if (catchZone == null)
                catchZone = animal.AddComponent<CatchZone>();
            catchZone.Initialize(_playerInput, config.catchRadius * _catchRadiusMultiplier, type);
            catchZone.OnCaught += HandleAnimalCaught;

            _activeAnimals.Add(animal);
            FarmSimVR.MonoBehaviours.Diagnostics.GameStateLogger.Instance?.LogEvent($"Spawned wild {type} at ({spawnPos.x:F1}, {spawnPos.z:F1})");
            return animal;
        }

        private void HandleAnimalCaught(CatchZone zone)
        {
            var record = new CaughtAnimalRecord(zone.AnimalType, Time.time);
            _tracker.Catch(record);
            zone.OnCaught -= HandleAnimalCaught;
            _activeAnimals.Remove(zone.gameObject);
            Destroy(zone.gameObject);
            Debug.Log($"[Hunting] Caught a {zone.AnimalType}! Carrying: {_tracker.CarriedCount}");
            FarmSimVR.MonoBehaviours.Diagnostics.GameStateLogger.Instance?.LogEvent($"Caught {zone.AnimalType}! Now carrying: {_tracker.CarriedCount}");
        }

        private bool IsInsidePen(Vector3 pos)
        {
            var pen = FindAnyObjectByType<AnimalPen>();
            if (pen == null) return false;
            float dist = Vector3.Distance(
                new Vector3(pos.x, 0, pos.z),
                new Vector3(pen.PenCenter.x, 0, pen.PenCenter.z));
            return dist < pen.PenRadius + 1f; // 1m buffer
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

        private Vector3 ResolveSpawnOrigin()
        {
            return spawnCenter != null ? spawnCenter.position : transform.position;
        }

        private static void DestroyAnimal(GameObject animal)
        {
            if (Application.isPlaying)
            {
                Destroy(animal);
                return;
            }

            DestroyImmediate(animal);
        }
    }
}
