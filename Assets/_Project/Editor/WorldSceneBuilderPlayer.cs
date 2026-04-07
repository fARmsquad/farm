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
            player.transform.position = new Vector3(30f, 1.5f, 27f); // spawn at farm entrance
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
            cam.farClipPlane = 300f;
            cam.clearFlags = CameraClearFlags.Skybox;
            camGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            // Remove any existing AudioListeners to avoid duplicates
            foreach (var existing in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                Object.DestroyImmediate(existing);
            camGO.AddComponent<AudioListener>();

            // Attach the FPS controller script
            player.AddComponent<FarmSimVR.MonoBehaviours.FirstPersonExplorer>();

            Debug.Log("[WorldSceneBuilder] First-person exploration player created.");
        }

        // ── Zone Signs ───────────────────────────────────────────

        private static void BuildZoneSigns()
        {
            var signsRoot = CreateEmpty("ZoneSigns", Vector3.zero);
            string signPrefab = "Assets/Synty/PolygonFarm/Prefabs/Props/SM_Prop_SignPost_01.prefab";

            (string name, Vector3 pos, float yRot)[] signs = {
                ("McTavish Farm", new Vector3(30f, 0.5f, 29f), 180f),
                ("Willowbrook", new Vector3(-27f, 0f, 18f), 90f),
                ("North Field", new Vector3(-72f, 0f, 63f), 0f),
                ("Sandy Shores", new Vector3(27f, 0f, 75f), -90f),
                ("Meadow", new Vector3(-39f, 0f, -51f), 0f),
                ("River", new Vector3(-33f, 0f, -60f), 45f),
                ("County Fair", new Vector3(75f, 0f, -51f), -90f),
                ("Wildflower Hills", new Vector3(0f, 0f, -97f), 0f),
                ("Trail", new Vector3(-21f, 0f, 21f), 45f),
            };

            foreach (var (name, pos, yRot) in signs)
            {
                var sign = InstantiatePrefab(signPrefab, pos,
                    Quaternion.Euler(0f, yRot, 0f), signsRoot.transform);
                var textGO = new GameObject($"Label_{name}");
                textGO.transform.SetParent(sign.transform);
                textGO.transform.localPosition = new Vector3(0f, 3f, 0f);
                textGO.transform.localRotation = Quaternion.identity;
                var tm = textGO.AddComponent<TextMesh>();
                tm.text = name;
                tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                tm.GetComponent<MeshRenderer>().material = tm.font.material;
                tm.fontSize = 32;
                tm.characterSize = 0.5f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = new Color(0.15f, 0.1f, 0.05f);
            }
            Debug.Log("[WorldSceneBuilder] Zone signs placed.");
        }
    }
}
