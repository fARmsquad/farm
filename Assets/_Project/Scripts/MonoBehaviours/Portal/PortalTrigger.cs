using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Portal
{
    /// <summary>
    /// Attached to a portal signpost GameObject. Detects when the player enters
    /// its trigger collider and tells PortalManager to start a scene transition.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PortalTrigger : MonoBehaviour
    {
        [Header("Destination")]
        [Tooltip("Scene path to load additively (e.g. Assets/_Project/Scenes/FarmMain.unity)")]
        [SerializeField] private string destinationScenePath;

        [Tooltip("Name of the spawn point GameObject in the destination scene")]
        [SerializeField] private string spawnPointName;

        /// <summary>Scene path that this portal leads to.</summary>
        public string DestinationScenePath => destinationScenePath;

        /// <summary>Name of the spawn point in the destination scene.</summary>
        public string SpawnPointName => spawnPointName;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            if (PortalManager.Instance == null)
            {
                Debug.LogError("[PortalTrigger] PortalManager.Instance is null. Is PortalManager in the Core scene?");
                return;
            }

            if (PortalManager.Instance.IsTransitioning)
                return;

            PortalManager.Instance.Transition(destinationScenePath, spawnPointName);
        }
    }
}
