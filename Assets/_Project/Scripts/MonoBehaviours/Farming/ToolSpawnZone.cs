using UnityEngine;
using FarmSimVR.Core.Inventory;

namespace FarmSimVR.MonoBehaviours.Farming
{
    /// <summary>
    /// MonoBehaviour on each box volume (Option1–Option12). Responsible for spawning
    /// environmental clutter at startup and optionally spawning a tool when told to by the manager.
    /// </summary>
    public sealed class ToolSpawnZone : MonoBehaviour
    {
        [Header("Clutter Prefabs")]
        [SerializeField] private GameObject[] treePrefabs;
        [SerializeField] private GameObject[] bushPrefabs;
        [SerializeField] private GameObject[] rockPrefabs;
        [SerializeField] private GameObject[] grassPrefabs;

        [Header("Clutter Settings")]
        [SerializeField] private int minClutterCount = 1;
        [SerializeField] private int maxClutterCount = 3;
        [SerializeField] private float clutterPadding = 0.5f;

        private const int MaxTreesPerZone = 1;

        private BoxCollider _zone;

        /// <summary>Reference to the zone's trigger collider.</summary>
        public BoxCollider Zone
        {
            get
            {
                if (_zone == null)
                    _zone = GetComponent<BoxCollider>();
                return _zone;
            }
        }

        /// <summary>Populate the zone with random trees, bushes, rocks, and grass patches.</summary>
        public void SpawnClutter()
        {
            if (Zone == null)
            {
                Debug.LogWarning($"[ToolSpawnZone] No BoxCollider found on '{name}'. Skipping clutter.");
                return;
            }

            int count = Random.Range(minClutterCount, maxClutterCount + 1);
            int treeCount = 0;

            for (int i = 0; i < count; i++)
            {
                GameObject prefab = PickClutterPrefab(ref treeCount);
                if (prefab == null)
                    continue;

                Vector3 position = GetRandomPositionInBounds();
                float yRotation = Random.Range(0f, 360f);

                GameObject instance = Instantiate(prefab, position, Quaternion.Euler(0f, yRotation, 0f), transform);
                instance.name = prefab.name;
                DisableColliders(instance);
            }
        }

        /// <summary>Instantiate a tool prefab at a random position within bounds and initialize its ToolPickup.</summary>
        public void SpawnTool(GameObject toolPrefab, string itemId, IInventorySystem inventory, float scale = 1f, Quaternion rotation = default, float yOffset = 0f)
        {
            if (toolPrefab == null)
            {
                Debug.LogWarning($"[ToolSpawnZone] Tool prefab is null for item '{itemId}'. Skipping.");
                return;
            }

            if (Zone == null)
            {
                Debug.LogWarning($"[ToolSpawnZone] No BoxCollider found on '{name}'. Cannot spawn tool.");
                return;
            }

            Vector3 position = GetRandomPositionInBounds();
            position.y = yOffset;
            GameObject instance = Instantiate(toolPrefab, position, rotation, transform);
            instance.name = toolPrefab.name;
            instance.transform.localScale = Vector3.one * scale;

            var pickup = instance.GetComponent<ToolPickup>();
            if (pickup == null)
                pickup = instance.AddComponent<ToolPickup>();

            pickup.Initialize(itemId, inventory);
        }

        /// <summary>
        /// Picks a random clutter prefab with weighted distribution:
        /// bushes and rocks are more common, trees are rare (capped at MaxTreesPerZone).
        /// </summary>
        private GameObject PickClutterPrefab(ref int treeCount)
        {
            // Weight: trees 1, bushes 2, rocks 4, grass 4
            // If tree cap reached, redistribute weight to other categories
            int treeWeight = (treeCount < MaxTreesPerZone && treePrefabs != null && treePrefabs.Length > 0) ? 1 : 0;
            int bushWeight = (bushPrefabs != null && bushPrefabs.Length > 0) ? 2 : 0;
            int rockWeight = (rockPrefabs != null && rockPrefabs.Length > 0) ? 4 : 0;
            int grassWeight = (grassPrefabs != null && grassPrefabs.Length > 0) ? 4 : 0;

            int totalWeight = treeWeight + bushWeight + rockWeight + grassWeight;
            if (totalWeight == 0)
                return null;

            int roll = Random.Range(0, totalWeight);

            if (roll < treeWeight)
            {
                treeCount++;
                return treePrefabs[Random.Range(0, treePrefabs.Length)];
            }
            roll -= treeWeight;

            if (roll < bushWeight)
                return bushPrefabs[Random.Range(0, bushPrefabs.Length)];
            roll -= bushWeight;

            if (roll < rockWeight)
                return rockPrefabs[Random.Range(0, rockPrefabs.Length)];

            return grassPrefabs[Random.Range(0, grassPrefabs.Length)];
        }

        /// <summary>Returns a random XZ position within padded bounds, with Y set to 0.</summary>
        private Vector3 GetRandomPositionInBounds()
        {
            Bounds bounds = Zone.bounds;

            float x = Random.Range(bounds.min.x + clutterPadding, bounds.max.x - clutterPadding);
            float z = Random.Range(bounds.min.z + clutterPadding, bounds.max.z - clutterPadding);

            return new Vector3(x, 0f, z);
        }

        /// <summary>Removes all colliders from a clutter instance so the player can walk through it.</summary>
        private static void DisableColliders(GameObject instance)
        {
            var colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Object.Destroy(colliders[i]);
            }
        }
    }
}
