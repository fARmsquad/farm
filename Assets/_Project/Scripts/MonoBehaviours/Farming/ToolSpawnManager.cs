using System;
using UnityEngine;
using FarmSimVR.Core.Inventory;
using FarmSimVR.MonoBehaviours.UI;

namespace FarmSimVR.MonoBehaviours.Farming
{
    /// <summary>
    /// Scene-level manager that orchestrates tool distribution across zones.
    /// Shuffles 12 zones, assigns tools to random zones, and tracks collection progress.
    /// Bootstraps its own player rig and inventory when FarmSimDriver is absent.
    /// </summary>
    public sealed class ToolSpawnManager : MonoBehaviour
    {
        [Header("Zones")]
        [SerializeField] private ToolSpawnZone[] zones;

        [Header("Tools")]
        [SerializeField] private ToolEntry[] tools;

        [Header("Player")]
        [SerializeField] private GameObject playerPrefab;

        private IInventorySystem _inventory;
        private IItemDatabase _database;
        private ToolEquipState _toolEquip;
        private int _toolsRemaining;

        private const float DefaultToolScale = 0.33f;

        /// <summary>True when all tools have been picked up.</summary>
        public bool AllToolsCollected => _toolsRemaining <= 0 && _toolsRemaining != -1;

        /// <summary>Count of uncollected tools.</summary>
        public int ToolsRemaining => _toolsRemaining;

        /// <summary>Fired when the last tool is collected.</summary>
        public event Action OnAllToolsCollected;

        private void Start()
        {
            EnsurePlayerRig();
            ResolveInventory();

            if (_inventory == null)
            {
                Debug.LogWarning("[ToolSpawnManager] Could not resolve IInventorySystem. Tools will not spawn.");
                return;
            }

            if (zones == null || zones.Length == 0)
            {
                Debug.LogWarning("[ToolSpawnManager] No zones assigned.");
                return;
            }

            if (tools == null || tools.Length == 0)
            {
                Debug.LogWarning("[ToolSpawnManager] No tools configured.");
                return;
            }

            DistributeTools();
        }

        /// <summary>Shuffles zones, spawns clutter on all, assigns tools to random zones.</summary>
        public void DistributeTools()
        {
            // Spawn clutter on all zones
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] != null)
                    zones[i].SpawnClutter();
            }

            // Fisher-Yates shuffle
            var shuffled = new ToolSpawnZone[zones.Length];
            Array.Copy(zones, shuffled, zones.Length);

            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            // Distribute tools to the first N shuffled zones
            _toolsRemaining = Mathf.Min(tools.Length, shuffled.Length);
            int toolCount = _toolsRemaining;

            for (int i = 0; i < toolCount; i++)
            {
                var entry = tools[i];
                if (entry.prefab == null)
                {
                    Debug.LogWarning($"[ToolSpawnManager] Tool entry at index {i} has no prefab assigned.");
                    _toolsRemaining--;
                    continue;
                }

                float scale = entry.scale > 0f ? entry.scale : DefaultToolScale;
                Quaternion rotation = Quaternion.Euler(entry.spawnRotation);
                shuffled[i].SpawnTool(entry.prefab, entry.itemId, _inventory, scale, rotation, entry.yOffset);

                // Find the ToolPickup we just spawned and subscribe to its event
                var pickup = shuffled[i].GetComponentInChildren<ToolPickup>();
                if (pickup != null)
                {
                    pickup.OnCollected += HandleToolCollected;
                }
            }

            Debug.Log($"[ToolSpawnManager] Distributed {toolCount} tools across {zones.Length} zones. " +
                      $"{_toolsRemaining} tools to collect.");
        }

        private void HandleToolCollected()
        {
            _toolsRemaining--;
            Debug.Log($"[ToolSpawnManager] Tool collected. {_toolsRemaining} remaining.");

            if (_toolsRemaining <= 0)
            {
                Debug.Log("[ToolSpawnManager] All tools collected!");
                OnAllToolsCollected?.Invoke();
            }
        }

        /// <summary>
        /// Ensures the player rig exists. Instantiates the assigned player prefab
        /// and outfits it with CharacterController + ThirdPersonFarmExplorer.
        /// </summary>
        private void EnsurePlayerRig()
        {
            // Already have a moving player
            var existingExplorer = FindAnyObjectByType<ThirdPersonFarmExplorer>();
            if (existingExplorer != null)
                return;

            // Remove any existing placeholder player objects
            var existingPlayer = GameObject.FindWithTag("Player");
            if (existingPlayer != null)
                Destroy(existingPlayer);

            // Also remove the GLB farmgirl if present
            var glbFarmgirl = GameObject.Find("farmgirl");
            if (glbFarmgirl != null)
                Destroy(glbFarmgirl);

            // Spawn the player prefab
            Vector3 spawnPos = ResolveSpawnPosition();
            GameObject player;

            if (playerPrefab != null)
            {
                player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                player.name = playerPrefab.name;
            }
            else
            {
                player = new GameObject("Player");
                player.transform.position = spawnPos;
                Debug.LogWarning("[ToolSpawnManager] No player prefab assigned. Created empty Player object.");
            }

            player.tag = "Player";

            if (player.GetComponent<CharacterController>() == null)
            {
                var cc = player.AddComponent<CharacterController>();
                cc.height = 1.8f;
                cc.radius = 0.35f;
                cc.center = new Vector3(0f, 0.9f, 0f);
                cc.stepOffset = 0.35f;
            }

            if (player.GetComponent<ThirdPersonFarmExplorer>() == null)
                player.AddComponent<ThirdPersonFarmExplorer>();

            Debug.Log($"[ToolSpawnManager] Spawned player '{player.name}' at {spawnPos}.");
        }

        /// <summary>Finds a spawn point in the scene or returns a default position.</summary>
        private static Vector3 ResolveSpawnPosition()
        {
            var spawn = GameObject.Find("PlayerSpawn") ?? GameObject.Find("SpawnPoint");
            return spawn != null ? spawn.transform.position : new Vector3(0f, 0.1f, -8f);
        }

        /// <summary>
        /// Resolves the inventory from FarmSimDriver if present,
        /// otherwise creates a standalone inventory with the starter database.
        /// Also initializes UI controllers.
        /// </summary>
        private void ResolveInventory()
        {
            var driver = FindAnyObjectByType<FarmSimDriver>();
            if (driver != null)
            {
                _inventory = driver.Inventory;
                return;
            }

            // Standalone mode: create our own inventory and equip state
            _database = ItemDatabase.CreateStarterDatabase();
            _inventory = new InventorySystem(_database, 24);
            _toolEquip = new ToolEquipState();

            // Initialize inventory UI
            var inventoryUI = FindAnyObjectByType<InventoryUIController>();
            if (inventoryUI != null)
                inventoryUI.Initialize(_inventory, _database, _toolEquip);

            // Initialize hotbar UI
            var hotbarUI = FindAnyObjectByType<HotbarUIController>();
            if (hotbarUI != null)
                hotbarUI.Initialize(_inventory, _database, _toolEquip);

            Debug.Log("[ToolSpawnManager] Created standalone inventory and initialized UI.");
        }

        /// <summary>Serializable entry for a tool to distribute.</summary>
        [Serializable]
        public struct ToolEntry
        {
            [Tooltip("Inventory item ID (e.g. 'tool_hoe')")]
            public string itemId;

            [Tooltip("Tool prefab to instantiate")]
            public GameObject prefab;

            [Tooltip("Scale factor for this tool (0 = use default 0.33)")]
            public float scale;

            [Tooltip("Spawn rotation in euler angles (e.g. 90,0,0 to lay flat)")]
            public Vector3 spawnRotation;

            [Tooltip("Y offset from ground level")]
            public float yOffset;
        }
    }
}
