using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Farming
{
    public static class FarmFirstPersonRigUtility
    {
        private const float CameraHeight = 1.6f;

        public static FirstPersonExplorer EnsureRig()
        {
            var existing = Object.FindAnyObjectByType<FirstPersonExplorer>();
            if (existing != null)
            {
                EnsureCamera(existing.transform);
                EnsureCharacterController(existing.gameObject);
                return existing;
            }

            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = ResolveSpawnPosition();
            EnsureCharacterController(player);
            EnsureCamera(player.transform);
            var explorer = player.AddComponent<FirstPersonExplorer>();
            return explorer;
        }

        private static void EnsureCharacterController(GameObject player)
        {
            var controller = player.GetComponent<CharacterController>();
            if (controller != null)
                return;

            controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.35f;
        }

        private static void EnsureCamera(Transform player)
        {
            var childCamera = player.GetComponentInChildren<Camera>();
            if (childCamera != null)
            {
                EnsureMainCameraTag(childCamera.gameObject);
                PositionCamera(childCamera.transform);
                return;
            }

            var main = Camera.main;
            if (main != null)
            {
                main.transform.SetParent(player, false);
                EnsureMainCameraTag(main.gameObject);
                PositionCamera(main.transform);
                return;
            }

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(player, false);
            cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
            PositionCamera(cameraGo.transform);
        }

        private static void EnsureMainCameraTag(GameObject cameraObject)
        {
            if (!cameraObject.CompareTag("MainCamera"))
                cameraObject.tag = "MainCamera";
        }

        private static void PositionCamera(Transform cameraTransform)
        {
            cameraTransform.localPosition = new Vector3(0f, CameraHeight, 0f);
            cameraTransform.localRotation = Quaternion.identity;
        }

        private static Vector3 ResolveSpawnPosition()
        {
            var spawn = GameObject.Find("SpawnPoint");
            return spawn != null ? spawn.transform.position : new Vector3(0f, 0.1f, -8f);
        }
    }
}
