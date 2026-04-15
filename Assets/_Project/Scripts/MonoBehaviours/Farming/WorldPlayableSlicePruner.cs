using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmSimVR.MonoBehaviours.Farming
{
    public static class WorldPlayableSlicePruner
    {
        private static readonly string[] AllowedRootNames =
        {
            "WorldSceneBootstrap",
            "Farm",
            "Terrain",
            "ExplorationPlayer",
            "Player",
            "Main Camera",
            "Directional Light",
            "SimpleAudioManager",
            "MissionManager",
            "DialogueCanvas",
            "ScreenEffectsCanvas",
            "GameManager",
        };

        private static readonly string[] AllowedFarmChildren =
        {
            "Buildings",
            "FarmPaths",
            "GroundCover",
            "Plots",
            "Pen",
            "Zones",
        };

        public static void Apply(GameObject host)
        {
            var scene = host.scene.IsValid() ? host.scene : SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            foreach (var root in roots)
            {
                if (root == null)
                    continue;

                root.SetActive(ShouldKeepRoot(root.name));
            }

            var farm = GameObject.Find("Farm");
            if (farm == null)
                return;

            foreach (Transform child in farm.transform)
                child.gameObject.SetActive(ShouldKeepFarmChild(child.name));

            DisableExtraDirectionalLights();
        }

        private static bool ShouldKeepRoot(string rootName)
        {
            for (var i = 0; i < AllowedRootNames.Length; i++)
            {
                if (AllowedRootNames[i] == rootName)
                    return true;
            }

            return false;
        }

        private static bool ShouldKeepFarmChild(string childName)
        {
            for (var i = 0; i < AllowedFarmChildren.Length; i++)
            {
                if (AllowedFarmChildren[i] == childName)
                    return true;
            }

            return false;
        }

        private static void DisableExtraDirectionalLights()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            var keptOne = false;

            foreach (var light in lights)
            {
                if (light.type != LightType.Directional)
                    continue;

                if (!keptOne)
                {
                    keptOne = true;
                    RenderSettings.sun = light;
                    light.gameObject.SetActive(true);
                    continue;
                }

                light.gameObject.SetActive(false);
            }
        }
    }
}
