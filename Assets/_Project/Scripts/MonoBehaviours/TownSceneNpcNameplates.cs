using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.UI;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Spawns overhead name cards for every <see cref="NPCController"/> in the loaded scene (Town).
    /// </summary>
    public sealed class TownSceneNpcNameplates : MonoBehaviour
    {
        [SerializeField] private float heightOffset = 2.2f;

        private void Awake()
        {
            foreach (var npc in FindObjectsByType<NPCController>(FindObjectsSortMode.None))
                NpcNameplateFactory.CreateNameplate(npc.transform, new Vector3(0f, heightOffset, 0f));
        }
    }
}
