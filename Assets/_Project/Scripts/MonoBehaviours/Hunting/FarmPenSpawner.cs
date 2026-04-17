using UnityEngine;
using FarmSimVR.MonoBehaviours.Economy;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    /// <summary>
    /// Reads LivestockRegistry from EconomyManager on scene load and spawns
    /// all owned animals into the farm pen. Attach to the same host as AnimalPen
    /// (or any GameObject in the FarmMain scene).
    /// </summary>
    public class FarmPenSpawner : MonoBehaviour
    {
        private void Start()
        {
            if (EconomyManager.Instance == null)
            {
                Debug.LogWarning("[FarmPenSpawner] No EconomyManager found — skipping livestock spawn.");
                return;
            }

            var pen = FindAnyObjectByType<AnimalPen>();
            if (pen == null)
            {
                Debug.LogWarning("[FarmPenSpawner] No AnimalPen found in scene — skipping livestock spawn.");
                return;
            }

            var animals = EconomyManager.Instance.Livestock.Animals;
            for (int i = 0; i < animals.Count; i++)
                pen.AddAnimal(animals[i]);

            if (animals.Count > 0)
                Debug.Log($"[FarmPenSpawner] Spawned {animals.Count} animal(s) from registry.");
        }
    }
}
