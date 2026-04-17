using UnityEngine;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Attach to a trigger volume at a barn door.
    /// isEntrance = true  → in FarmMain, loads Barn scene and teleports player in.
    /// isEntrance = false → in Barn, teleports player out and unloads Barn scene.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BarnDoorTrigger : MonoBehaviour
    {
        [SerializeField] private string barnScenePath = "Assets/_Project/Scenes/Barn.unity";
        [SerializeField] private string spawnPointName;
        [SerializeField] private bool isEntrance;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            var controller = BarnTransitionController.GetOrCreate();
            if (controller.IsTransitioning)
                return;

            if (isEntrance)
                controller.EnterBarn(barnScenePath, spawnPointName);
            else
                controller.ExitBarn(barnScenePath, spawnPointName);
        }
    }
}
