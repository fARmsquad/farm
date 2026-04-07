using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    [CreateAssetMenu(fileName = "HuntingConfig", menuName = "FarmSimVR/Hunting Config")]
    public class HuntingConfig : ScriptableObject
    {
        [Header("Spawning")]
        [Tooltip("Seconds between wild animal spawns")]
        public float spawnInterval = 8f;

        [Tooltip("Maximum wild animals alive at once")]
        public int maxWildAnimals = 5;

        [Tooltip("Distance from center where animals spawn (farm perimeter)")]
        public float spawnRadius = 9f;

        [Header("Detection & Catch")]
        [Tooltip("Distance at which animal detects player and starts fleeing")]
        public float detectionRadius = 5f;

        [Tooltip("Distance at which player can catch the animal")]
        public float catchRadius = 2f;

        [Header("Flee Behavior")]
        [Tooltip("Speed multiplier when fleeing (applied on top of wander speed)")]
        public float fleeSpeedMultiplier = 2.5f;

        [Tooltip("How long animal stays in flee mode after losing sight of player")]
        public float fleeCooldown = 3f;
    }
}
