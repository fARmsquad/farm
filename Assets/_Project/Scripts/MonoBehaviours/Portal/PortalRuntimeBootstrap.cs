using FarmSimVR.MonoBehaviours.Farming;
using UnityEngine;
using UnityEngine.SceneManagement;
using FarmSimVR.MonoBehaviours;

namespace FarmSimVR.MonoBehaviours.Portal
{
    public static class PortalRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneBootstrap()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        public static bool TryBootstrapForScene(Scene scene)
        {
            if (PortalManager.Instance != null || !scene.IsValid() || !scene.isLoaded)
                return false;

            if (!SceneContainsPortalTrigger(scene))
                return false;

            if (!TryResolvePlayer(out var playerTransform, out var characterController))
            {
                Debug.LogWarning("[PortalRuntimeBootstrap] Portal scene loaded without a resolvable player rig. Portal runtime bootstrap skipped.");
                return false;
            }

            var runtime = new GameObject("PortalRuntime");
            if (Application.isPlaying)
                Object.DontDestroyOnLoad(runtime);

            var manager = runtime.AddComponent<PortalManager>();
            manager.BootstrapFromActiveScene(playerTransform, characterController, scene.path);
            return true;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryBootstrapForScene(scene);
        }

        private static bool SceneContainsPortalTrigger(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<PortalTrigger>(true) != null)
                    return true;
            }

            return false;
        }

        private static bool TryResolvePlayer(out Transform playerTransform, out CharacterController characterController)
        {
            var townPlayer = Object.FindAnyObjectByType<TownPlayerController>(FindObjectsInactive.Include);
            if (TryGetPlayerReferences(townPlayer != null ? townPlayer.transform : null, out playerTransform, out characterController))
                return true;

            var farmExplorer = Object.FindAnyObjectByType<ThirdPersonFarmExplorer>(FindObjectsInactive.Include);
            if (TryGetPlayerReferences(farmExplorer != null ? farmExplorer.transform : null, out playerTransform, out characterController))
                return true;

            var firstPersonExplorer = Object.FindAnyObjectByType<FirstPersonExplorer>(FindObjectsInactive.Include);
            if (TryGetPlayerReferences(firstPersonExplorer != null ? firstPersonExplorer.transform : null, out playerTransform, out characterController))
                return true;

            var taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (TryGetPlayerReferences(taggedPlayer != null ? taggedPlayer.transform : null, out playerTransform, out characterController))
                return true;

            var genericCharacterController = Object.FindAnyObjectByType<CharacterController>(FindObjectsInactive.Include);
            if (genericCharacterController != null)
            {
                playerTransform = genericCharacterController.transform;
                characterController = genericCharacterController;
                return true;
            }

            playerTransform = null;
            characterController = null;
            return false;
        }

        private static bool TryGetPlayerReferences(
            Transform candidate,
            out Transform playerTransform,
            out CharacterController characterController)
        {
            playerTransform = candidate;
            characterController = candidate != null ? candidate.GetComponent<CharacterController>() : null;
            return playerTransform != null;
        }
    }
}
