using System.Collections;
using UnityEngine;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.MonoBehaviours.Autoplay
{
    public class AutoplayMission : AutoplayBase
    {
        private void Awake()
        {
            specId = "INT-007";
            specTitle = "Mission Manager";
            totalSteps = 5;
        }

        protected override IEnumerator RunDemo()
        {
            var mm = MissionManager.Instance;
            if (mm == null) { currentLabel = "MissionManager not found!"; yield break; }

            Step("Start mission: Farm Tour");
            mm.StartMission("Farm Tour", "Follow the path to the farmhouse");
            yield return Wait(3f);

            Step("Update objective");
            mm.UpdateObjective("Now visit the barn");
            yield return Wait(3f);

            Step("Complete mission");
            mm.CompleteMission();
            yield return Wait(4.5f);

            Step("Start new mission: Meet the Mayor");
            mm.StartMission("Meet the Mayor", "Find the Mayor in town square");
            yield return Wait(3f);

            Step("Complete second mission");
            mm.CompleteMission();
            yield return Wait(4.5f);
        }
    }
}
