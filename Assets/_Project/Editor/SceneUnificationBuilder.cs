using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Hunting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmSimVR.Editor
{
    /// <summary>
    /// Wires unified gameplay systems into a greybox scene so that all
    /// managers, player, camera and crop runtime are present and connected.
    /// Called from menu items or tests.
    /// </summary>
    public static class SceneUnificationBuilder
    {
        public static void ConfigureScene(Scene scene)
        {
            // ── Core managers ─────────────────────────────────────
            var managersGo = new GameObject("GameplaySystems");
            managersGo.AddComponent<GameManager>();
            managersGo.AddComponent<SimulationManager>();

            // ── Hunting subsystem ─────────────────────────────────
            var huntingGo = new GameObject("HuntingSystem");
            huntingGo.AddComponent<HuntingManager>();
            huntingGo.AddComponent<WildAnimalSpawner>();

            // ── Barn / pen ────────────────────────────────────────
            var barnAnchor = GameObject.Find("BarnPosition");
            var barnGo = barnAnchor != null
                ? barnAnchor
                : new GameObject("BarnDropOff");
            barnGo.AddComponent<BarnDropOff>();
            barnGo.AddComponent<AnimalPen>();

            // ── HUD ───────────────────────────────────────────────
            var hudGo = new GameObject("HuntingHUD");
            hudGo.AddComponent<HuntingHUD>();

            // ── Player ────────────────────────────────────────────
            var spawnPoint = GameObject.Find("SpawnPoint");
            var playerGo = new GameObject("Player");
            if (spawnPoint != null)
                playerGo.transform.position = spawnPoint.transform.position;

            playerGo.AddComponent<PlayerMovement>();
            playerGo.AddComponent<KeyboardPlayerInput>();

            // ── Camera ────────────────────────────────────────────
            var camGo = new GameObject("ThirdPersonCamera");
            camGo.AddComponent<Camera>();
            camGo.AddComponent<ThirdPersonCamera>();

            // ── Crop runtime wiring ───────────────────────────────
            WireCropPlots();
        }

        private static void WireCropPlots()
        {
            var plots = Object.FindObjectsByType<Transform>();
            foreach (var t in plots)
            {
                if (!t.name.StartsWith("CropPlot_")) continue;

                if (t.GetComponent<CropPlotController>() == null)
                    t.gameObject.AddComponent<CropPlotController>();

                var visual = t.Find("CropVisual");
                if (visual == null)
                {
                    var visualGo = new GameObject("CropVisual");
                    visualGo.transform.SetParent(t);
                    visualGo.transform.localPosition = Vector3.zero;
                    visual = visualGo.transform;
                }

                if (visual.GetComponent<CropVisualUpdater>() == null)
                    visual.gameObject.AddComponent<CropVisualUpdater>();
            }
        }
    }
}
