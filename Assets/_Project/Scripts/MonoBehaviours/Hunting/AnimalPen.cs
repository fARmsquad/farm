using UnityEngine;
using System.Collections.Generic;
using FarmSimVR.Core.Hunting;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    [System.Serializable]
    public struct PenAnimalEntry
    {
        public AnimalType type;
        public GameObject prefab;
    }

    public class AnimalPen : MonoBehaviour
    {
        [SerializeField] private PenAnimalEntry[] penAnimalPrefabs;
        [SerializeField] private Vector3 penCenter = new Vector3(5, 0, 6.5f);
        [SerializeField] private float penRadius = 3f;
        [SerializeField] private bool buildFenceOnStart = true;
        [SerializeField] private int fencePostCount = 12;

        private readonly List<PenAnimal> _penAnimals = new();
        private BarnDropOff _barnDropOff;

        public IReadOnlyList<PenAnimal> PenAnimals => _penAnimals;
        public Vector3 PenCenter => penCenter;
        public float PenRadius => penRadius;

        public void Initialize(BarnDropOff barnDropOff)
        {
            if (_barnDropOff != null) return; // already initialized
            _barnDropOff = barnDropOff;
            _barnDropOff.OnAnimalsDeposited += HandleAnimalsDeposited;
            Debug.Log($"[AnimalPen] Initialized via explicit call. Prefabs: {penAnimalPrefabs?.Length ?? 0}");
        }

        private void Start()
        {
            // Self-wire if not already initialized by HuntingManager
            if (_barnDropOff == null)
            {
                var barn = FindAnyObjectByType<BarnDropOff>();
                if (barn != null)
                {
                    _barnDropOff = barn;
                    _barnDropOff.OnAnimalsDeposited += HandleAnimalsDeposited;
                    Debug.Log($"[AnimalPen] Self-wired to BarnDropOff. Prefabs: {penAnimalPrefabs?.Length ?? 0}");
                }
                else
                {
                    Debug.LogError("[AnimalPen] No BarnDropOff found in scene!");
                }
            }

            if (buildFenceOnStart)
                BuildFence();
        }

        private void OnDestroy()
        {
            if (_barnDropOff != null)
                _barnDropOff.OnAnimalsDeposited -= HandleAnimalsDeposited;
        }

        private void HandleAnimalsDeposited(IReadOnlyList<CaughtAnimalRecord> records)
        {
            Debug.Log($"[AnimalPen] HandleAnimalsDeposited called with {records.Count} records");
            foreach (var record in records)
                SpawnPenAnimal(record);
        }

        private void SpawnPenAnimal(CaughtAnimalRecord record)
        {
            GameObject prefab = FindPrefab(record.Type);
            if (prefab == null)
            {
                Debug.LogWarning($"[AnimalPen] No prefab mapped for {record.Type}");
                return;
            }

            // Random position inside pen
            Vector2 rnd = Random.insideUnitCircle * penRadius * 0.7f;
            Vector3 spawnPos = penCenter + new Vector3(rnd.x, 0, rnd.y);
            float randomYRot = Random.Range(0f, 360f);

            GameObject animal = Instantiate(prefab, spawnPos, Quaternion.Euler(0, randomYRot, 0), transform);
            animal.name = $"Pen_{record.Type}";

            // Strip wild-animal components if present
            var flee = animal.GetComponent<AnimalFleeBehavior>();
            if (flee != null) Destroy(flee);
            var catchZone = animal.GetComponent<CatchZone>();
            if (catchZone != null) Destroy(catchZone);

            // Set up wander constrained to pen
            var wander = animal.GetComponent<AnimalWander>();
            if (wander == null)
                wander = animal.AddComponent<AnimalWander>();
            wander.SetBounds(penCenter, penRadius);

            // Add identity component
            var penAnimal = animal.AddComponent<PenAnimal>();
            penAnimal.Initialize(record.Type, Time.time);
            _penAnimals.Add(penAnimal);

            Debug.Log($"[AnimalPen] {record.Type} is now vibing in the pen! ({_penAnimals.Count} total)");
        }

        private GameObject FindPrefab(AnimalType type)
        {
            if (penAnimalPrefabs == null) return null;
            foreach (var entry in penAnimalPrefabs)
                if (entry.type == type)
                    return entry.prefab;
            return null;
        }

        private void BuildFence()
        {
            var fenceParent = new GameObject("Fence");
            fenceParent.transform.SetParent(transform);
            fenceParent.transform.position = penCenter;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            Material postMat = null;
            Material railMat = null;
            if (shader != null)
            {
                postMat = new Material(shader) { color = new Color(0.4f, 0.25f, 0.1f) };
                railMat = new Material(shader) { color = new Color(0.5f, 0.35f, 0.15f) };
            }

            Vector3[] postPositions = new Vector3[fencePostCount];

            for (int i = 0; i < fencePostCount; i++)
            {
                float angle = (i / (float)fencePostCount) * Mathf.PI * 2;
                Vector3 pos = penCenter + new Vector3(
                    Mathf.Cos(angle) * penRadius,
                    0.4f,
                    Mathf.Sin(angle) * penRadius);
                postPositions[i] = pos;

                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = $"Post_{i}";
                post.transform.SetParent(fenceParent.transform);
                post.transform.position = pos;
                post.transform.localScale = new Vector3(0.12f, 0.8f, 0.12f);
                if (postMat != null) post.GetComponent<Renderer>().sharedMaterial = postMat;
                Destroy(post.GetComponent<Collider>());
            }

            // Rails connecting posts
            for (int i = 0; i < fencePostCount; i++)
            {
                int next = (i + 1) % fencePostCount;
                Vector3 midpoint = (postPositions[i] + postPositions[next]) / 2f;
                Vector3 dir = postPositions[next] - postPositions[i];
                float length = dir.magnitude;

                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = $"Rail_{i}";
                rail.transform.SetParent(fenceParent.transform);
                rail.transform.position = midpoint + Vector3.up * 0.1f;
                rail.transform.rotation = Quaternion.LookRotation(dir);
                rail.transform.localScale = new Vector3(0.06f, 0.06f, length);
                if (railMat != null) rail.GetComponent<Renderer>().sharedMaterial = railMat;
                Destroy(rail.GetComponent<Collider>());
            }
        }
    }
}
