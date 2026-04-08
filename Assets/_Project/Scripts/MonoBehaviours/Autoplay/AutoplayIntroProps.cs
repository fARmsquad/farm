using System.Collections;
using UnityEngine;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.MonoBehaviours.Autoplay
{
    public class AutoplayIntroProps : AutoplayBase
    {
        private void Awake()
        {
            specId = "INT-013";
            specTitle = "Intro Props & Ambient NPCs";
            totalSteps = 5;
        }

        protected override IEnumerator RunDemo()
        {
            Step("Attaching lantern");
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var lanternGo = new GameObject("Lantern_Autoplay");
                lanternGo.transform.SetParent(player.transform, false);
                lanternGo.AddComponent<LanternHolder>();
                Debug.Log("[AutoplayIntroProps] Lantern attached to player.");
            }
            else
            {
                Debug.LogWarning("[AutoplayIntroProps] No Player-tagged object found, skipping lantern.");
            }
            yield return Wait(3f);

            Step("Spawning baby chicks");
            Vector3 chickBase = new Vector3(5f, 0f, 5f);
            for (int i = 0; i < 4; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
                Vector3 pos = chickBase + offset;

                if (Terrain.activeTerrain != null)
                    pos.y = Terrain.activeTerrain.SampleHeight(pos);

                var chickGo = new GameObject($"BabyChick_Autoplay_{i}");
                chickGo.transform.position = pos;
                chickGo.AddComponent<BabyChick>();
            }
            Debug.Log("[AutoplayIntroProps] Spawned 4 baby chicks near (5,0,5).");
            yield return Wait(4f);

            Step("Launching boot");
            var bootGo = new GameObject("BootProjectile_Autoplay");
            var boot = bootGo.AddComponent<BootProjectile>();
            boot.Launch(new Vector3(0f, 6f, 8f), new Vector3(5f, 0f, 5f));
            Debug.Log("[AutoplayIntroProps] Boot launched from (0,6,8) toward (5,0,5).");
            yield return Wait(2f);

            Step("Running cat across rooftop");
            var runnerGo = new GameObject("RooftopCat_Autoplay");
            var runner = runnerGo.AddComponent<RooftopRunner>();
            runner.Run(new Vector3(-8f, 5f, 0f), new Vector3(8f, 5f, 0f));
            Debug.Log("[AutoplayIntroProps] Cat running from (-8,5,0) to (8,5,0).");
            yield return Wait(3f);

            Step("All props active");
            Debug.Log("[AutoplayIntroProps] All intro props are active. Observe the scene.");
            yield return Wait(3f);
        }
    }
}
