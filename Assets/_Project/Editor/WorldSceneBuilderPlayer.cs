using UnityEditor;
using UnityEngine;

namespace FarmSimVR.Editor
{
    public static partial class WorldSceneBuilder
    {
        private static void BuildExplorationPlayer()
        {
            // Player root
            var player = new GameObject("ExplorationPlayer");
            player.transform.position = new Vector3(50f, 1.5f, 45f); // spawn at farm entrance
            player.tag = "Player";

            // Character Controller for collision
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            // First-person camera as child
            var camGO = new GameObject("FirstPersonCamera");
            camGO.transform.SetParent(player.transform);
            camGO.transform.localPosition = new Vector3(0f, 1.6f, 0f); // eye height
            camGO.transform.localRotation = Quaternion.identity;
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            cam.clearFlags = CameraClearFlags.Skybox;
            camGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            camGO.AddComponent<AudioListener>();

            // Attach the FPS controller script
            player.AddComponent<FarmSimVR.MonoBehaviours.FirstPersonExplorer>();

            Debug.Log("[WorldSceneBuilder] First-person exploration player created.");
        }
    }
}
