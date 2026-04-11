using FarmSimVR.Core.Hunting;
using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    public sealed class WorldPenGameController : MonoBehaviour
    {
        [SerializeField] private string startZoneName = "Chicken Coop";
        [SerializeField] private Transform penRoot;
        [SerializeField] private WildAnimalSpawner spawner;
        [SerializeField] private BarnDropOff dropOff;
        [SerializeField] private AnimalPen animalPen;
        [SerializeField] private KeyboardPlayerInput playerInput;
        [SerializeField] private WorldPenProgressionController progression;
        [SerializeField] private WorldPenGameCatalog catalog;

        private ZoneTracker _zoneTracker;
        private CaughtAnimalTracker _tracker;
        private string _statusMessage = string.Empty;
        private float _statusUntil;
        private bool _isGameActive;

        public bool IsGameActive => _isGameActive;
        public bool IsPromptVisible => !_isGameActive && IsInStartZone();
        public int CarriedCount => _tracker?.CarriedCount ?? 0;
        public int DepositedCount => _tracker?.DepositedCount ?? 0;
        public int WildCount => spawner != null ? spawner.ActiveCount : 0;
        public string StatusMessage => Time.unscaledTime <= _statusUntil ? _statusMessage : string.Empty;

        public void Configure(
            Transform root,
            WildAnimalSpawner wildSpawner,
            BarnDropOff barnDropOff,
            AnimalPen runtimeAnimalPen,
            KeyboardPlayerInput input,
            WorldPenProgressionController progressionController)
        {
            penRoot = root;
            spawner = wildSpawner;
            dropOff = barnDropOff;
            animalPen = runtimeAnimalPen;
            playerInput = input;
            progression = progressionController;
        }

        private void Update()
        {
            ResolveDependencies();
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (!keyboard.gKey.wasPressedThisFrame)
                return;

            if (_isGameActive)
            {
                StopGame();
                return;
            }

            if (IsInStartZone())
                StartGame();
        }

        public bool TryStartGame(out string message)
        {
            if (_isGameActive)
            {
                message = "Pen game is already active.";
                return false;
            }

            if (!IsInStartZone())
            {
                message = $"Enter the {startZoneName} zone to start.";
                return false;
            }

            StartGame();
            message = StatusMessage;
            return true;
        }

        public bool TryStopGame(out string message)
        {
            if (!_isGameActive)
            {
                message = "Pen game is not active.";
                return false;
            }

            StopGame();
            message = StatusMessage;
            return true;
        }

        private void StartGame()
        {
            ResolveDependencies();
            if (!TryResolveCatalog())
            {
                SetStatus("Pen catalog missing.");
                return;
            }

            _tracker = new CaughtAnimalTracker();
            dropOff.Initialize(_tracker);
            dropOff.OnDeposit -= HandleDeposit;
            dropOff.OnDeposit += HandleDeposit;

            ConfigureAnimalPen();
            animalPen.Initialize(dropOff);

            var catchMultiplier = progression?.Service != null ? progression.Service.GetCatchRadiusMultiplier() : 1f;
            var fleeMultiplier = progression?.Service != null ? progression.Service.GetFleeSpeedMultiplier() : 1f;
            spawner.Configure(catalog.HuntingConfig, catalog.WildAnimalPrefabs, playerInput.transform, penRoot, catchMultiplier, fleeMultiplier);
            spawner.Initialize(playerInput, _tracker);
            spawner.enabled = true;
            spawner.ClearActiveAnimals();
            spawner.TrySpawnNow();
            _isGameActive = true;
            SetStatus("Pen game started. Catch with E and deposit at the gate.");
            GameStateLogger.Instance?.LogEvent("World pen game started");
        }

        private void StopGame()
        {
            if (dropOff != null)
                dropOff.OnDeposit -= HandleDeposit;

            if (spawner != null)
            {
                spawner.enabled = false;
                spawner.ClearActiveAnimals();
            }

            _tracker = null;
            _isGameActive = false;
            SetStatus("Pen game ended.");
            GameStateLogger.Instance?.LogEvent("World pen game ended");
        }

        private void HandleDeposit(int animalCount)
        {
            if (progression == null || animalCount <= 0)
            {
                SetStatus($"Deposited {animalCount} animals.");
                return;
            }

            var reward = progression.ApplyDeposits(animalCount);
            SetStatus($"Deposited {animalCount} animals. +{reward.ExperienceEarned} pen XP.");
        }

        private void ConfigureAnimalPen()
        {
            var entries = catalog != null ? catalog.PenAnimalPrefabs : System.Array.Empty<PenAnimalEntry>();
            animalPen.ConfigureRuntime(entries, penRoot.position, ResolvePenRadius(), false);
        }

        private float ResolvePenRadius()
        {
            if (penRoot == null)
                return 4f;

            var renderers = penRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return 4f;

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.75f;
        }

        private bool TryResolveCatalog()
        {
            if (catalog == null)
                catalog = Resources.Load<WorldPenGameCatalog>("WorldPenGameCatalog");

            if (catalog != null)
                return true;

            catalog = WorldPenRuntimeFallbackFactory.Create();
            return catalog != null;
        }

        private bool IsInStartZone()
        {
            return string.Equals(_zoneTracker?.CurrentZone, startZoneName, System.StringComparison.Ordinal);
        }

        private void ResolveDependencies()
        {
            if (_zoneTracker == null)
                _zoneTracker = FindAnyObjectByType<ZoneTracker>();

            if (progression == null)
                progression = GetComponent<WorldPenProgressionController>() ?? FindAnyObjectByType<WorldPenProgressionController>();

            if (playerInput == null)
                playerInput = FindAnyObjectByType<KeyboardPlayerInput>();
        }

        private void SetStatus(string message)
        {
            _statusMessage = message ?? string.Empty;
            _statusUntil = Time.unscaledTime + 3f;
        }

        private static class WorldPenRuntimeFallbackFactory
        {
            public static WorldPenGameCatalog Create()
            {
                var fallback = ScriptableObject.CreateInstance<WorldPenGameCatalog>();
                var config = ScriptableObject.CreateInstance<HuntingConfig>();
                var prefabs = CreateFallbackPrefabs();
                SetField(fallback, "huntingConfig", config);
                SetField(fallback, "wildAnimalPrefabs", prefabs);
                SetField(fallback, "penAnimalPrefabs", CreateEntries(prefabs));
                return fallback;
            }

            private static GameObject[] CreateFallbackPrefabs()
            {
                return new[]
                {
                    CreateHiddenPrefab("Chicken", PrimitiveType.Capsule, new Color(0.95f, 0.9f, 0.6f)),
                    CreateHiddenPrefab("Cow", PrimitiveType.Cube, new Color(0.85f, 0.85f, 0.85f)),
                    CreateHiddenPrefab("Horse", PrimitiveType.Capsule, new Color(0.58f, 0.4f, 0.22f)),
                    CreateHiddenPrefab("Pig", PrimitiveType.Sphere, new Color(0.95f, 0.68f, 0.76f)),
                    CreateHiddenPrefab("Sheep", PrimitiveType.Capsule, new Color(0.96f, 0.96f, 0.96f)),
                };
            }

            private static PenAnimalEntry[] CreateEntries(GameObject[] prefabs)
            {
                return new[]
                {
                    new PenAnimalEntry { type = AnimalType.Chicken, prefab = prefabs[0] },
                    new PenAnimalEntry { type = AnimalType.Cow, prefab = prefabs[1] },
                    new PenAnimalEntry { type = AnimalType.Horse, prefab = prefabs[2] },
                    new PenAnimalEntry { type = AnimalType.Pig, prefab = prefabs[3] },
                    new PenAnimalEntry { type = AnimalType.Sheep, prefab = prefabs[4] },
                };
            }

            private static GameObject CreateHiddenPrefab(string name, PrimitiveType primitiveType, Color color)
            {
                var go = GameObject.CreatePrimitive(primitiveType);
                go.name = name;
                go.hideFlags = HideFlags.HideAndDontSave;
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    if (shader != null)
                    {
                        var material = new Material(shader);
                        material.color = color;
                        renderer.sharedMaterial = material;
                    }
                }

                go.SetActive(false);
                return go;
            }

            private static void SetField<T>(WorldPenGameCatalog target, string fieldName, T value)
            {
                var field = typeof(WorldPenGameCatalog).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(target, value);
            }
        }
    }
}
