using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using FarmSimVR.MonoBehaviours.ChickenGame;

namespace FarmSimVR.Editor
{
    public static class ChickenGameSceneBuilder
    {
        // ── Arena ─────────────────────────────────────────────────────────────
        // Square pen: wall centres sit at ±PenHalf on X and Z
        private const float PenHalf          = 10f;
        // Circular arena for AI (must stay inside the square walls)
        private const float ArenaRadius      = 9.5f;

        // ── Lighting (matches WorldMain exactly) ──────────────────────────────
        private static readonly Color SunColor      = new(1f,    0.96f, 0.88f);
        private static readonly Color FogColor      = new(1f,    0.83f, 0.62f);
        private static readonly Color AmbientSky    = new(0.31f, 0.28f, 0.38f);
        private static readonly Color AmbientEquator= new(0.55f, 0.50f, 0.42f);
        private static readonly Color AmbientGround = new(0.23f, 0.20f, 0.16f);

        // ── Asset paths ───────────────────────────────────────────────────────
        private const string FarmgirlPath   = "Assets/_Project/Prefabs/Models/farmgirl.prefab";
        private const string ChickenPath    = "Assets/_Project/Prefabs/Models/chicken.prefab";
        private const string GroundPath     = "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Ground_Flat_01.prefab";
        private const string DirtPath       = "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Ground_Dirt_01.prefab";
        private const string GrassPatch1    = "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Grass_Patch_01.prefab";
        private const string GrassPatch2    = "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Grass_Patch_02.prefab";
        private const string GrassPatch3    = "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Grass_Patch_03.prefab";
        private const string CoopPath       = "Assets/_Project/Prefabs/Models/coop.prefab";
        private const string Tree1Path      = "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Tree_01.prefab";
        private const string Tree2Path      = "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Tree_02.prefab";
        private const string FenceRoundPath = "Assets/Synty/PolygonFarm/Prefabs/Props/SM_Prop_Fence_Wood_Round_01.prefab";
        private const string SkyboxPath     = "Assets/_Project/Materials/SkyboxProcedural.mat";

        private const string SfxBawkPath    = "Assets/_Project/Sounds/SFX/bawk-bawk.mp3";
        private const string SfxChickletsPath = "Assets/_Project/Sounds/SFX/chicklets.mp3";
        private const string SfxWingFlapPath = "Assets/_Project/Sounds/SFX/wing-flap.mp3";
        private const string SpeechIntroPath = "Assets/_Project/Sounds/Speech/ya-darn-chicken.mp3";
        private const string MusicBanjoPath = "Assets/_Project/Sounds/Music/banjo-fast.mp3";
        private const string SpeechGrabPath = "Assets/_Project/Sounds/Speech/you-aint-gettin-away.mp3";
        private const string SpeechDropPath = "Assets/_Project/Sounds/Speech/i-aint-cluckin-around.mp3";
        private const string SfxVictoryPath = "Assets/_Project/Sounds/SFX/victory.mp3";

        [MenuItem("FarmSim/Build Chicken Game Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.OpenScene(
                "Assets/_Project/Scenes/ChickenGame.unity",
                OpenSceneMode.Single);

            foreach (var root in scene.GetRootGameObjects())
                Object.DestroyImmediate(root);

            EnsureTag("Player");

            ApplyLighting();
            CreateEnvironment();
            var player  = CreatePlayer();
            var chicken = CreateChicken();
            CreateGameManager(chicken, player.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ChickenGameSceneBuilder] ChickenGame scene built and saved.");
        }

        /// <summary>
        /// Adds or replaces <c>Environment/FenceRing</c> in the <b>active</b> scene only — does not delete
        /// other objects. Use this when you already have a Chicken Game scene open and do not want
        /// <see cref="BuildScene"/> (which wipes the scene and rebuilds everything from scratch).
        /// </summary>
        [MenuItem("FarmSim/Add Chicken Game Fence Ring (current scene)")]
        public static void AddFenceRingToCurrentScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[ChickenGameSceneBuilder] No valid active scene.");
                return;
            }

            Transform parent;
            var env = GameObject.Find("Environment");
            if (env != null)
                parent = env.transform;
            else
            {
                var go = new GameObject("Environment");
                parent = go.transform;
            }

            var existing = parent.Find("FenceRing");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            PlaceCircularFence(parent);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[ChickenGameSceneBuilder] Fence ring added under Environment.");
        }

        // ── Lighting ─────────────────────────────────────────────────────────

        static void ApplyLighting()
        {
            var go    = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type            = LightType.Directional;
            light.color           = SunColor;
            light.intensity       = 1.2f;
            light.shadows         = LightShadows.Soft;
            light.shadowStrength  = 0.7f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();

            RenderSettings.fog              = true;
            RenderSettings.fogMode          = FogMode.Linear;
            RenderSettings.fogColor         = FogColor;
            RenderSettings.fogStartDistance = 50f;
            RenderSettings.fogEndDistance   = 150f;

            RenderSettings.ambientMode        = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor    = AmbientSky;
            RenderSettings.ambientEquatorColor= AmbientEquator;
            RenderSettings.ambientGroundColor = AmbientGround;

            var skyMat = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            if (skyMat != null) RenderSettings.skybox = skyMat;
        }

        // ── Environment ──────────────────────────────────────────────────────

        static void CreateEnvironment()
        {
            var env = new GameObject("Environment");

            PlaceGround(env.transform);
            AddGroundCollider(env.transform);
            PlaceWallColliders(env.transform);
            PlaceCircularFence(env.transform);
            PlaceProps(env.transform);
        }

        static void AddGroundCollider(Transform parent)
        {
            var go  = new GameObject("GroundCollider");
            go.transform.SetParent(parent);
            go.transform.position = Vector3.zero;
            var col  = go.AddComponent<BoxCollider>();
            col.size   = new Vector3(100f, 0.2f, 100f);
            col.center = new Vector3(0f, -0.1f, 0f);
        }

        static void PlaceGround(Transform parent)
        {
            var ground = new GameObject("Ground");
            ground.transform.SetParent(parent);

            var groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GroundPath);
            var dirtPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>(DirtPath);
            var patch1       = AssetDatabase.LoadAssetAtPath<GameObject>(GrassPatch1);
            var patch2       = AssetDatabase.LoadAssetAtPath<GameObject>(GrassPatch2);
            var patch3       = AssetDatabase.LoadAssetAtPath<GameObject>(GrassPatch3);

            // Grass tile grid (5×5, 5-unit spacing → covers 25×25 area)
            if (groundPrefab != null)
            {
                var grassParent = new GameObject("GrassTiles");
                grassParent.transform.SetParent(ground.transform);
                for (int x = -2; x <= 2; x++)
                for (int z = -2; z <= 2; z++)
                {
                    var t = (GameObject)PrefabUtility.InstantiatePrefab(groundPrefab, grassParent.transform);
                    t.transform.position = new Vector3(x * 5f, 0f, z * 5f);
                }
            }

            // Dirt tiles inside the pen (3×3, 2.5-unit spacing)
            if (dirtPrefab != null)
            {
                var dirtParent = new GameObject("DirtTiles");
                dirtParent.transform.SetParent(ground.transform);
                for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                {
                    var t = (GameObject)PrefabUtility.InstantiatePrefab(dirtPrefab, dirtParent.transform);
                    t.transform.position = new Vector3(x * 2.5f, 0f, z * 2.5f);
                }
            }

            // Scattered grass patches around the outside
            var patches = new[] { patch1, patch2, patch3 };
            var rng = new System.Random(42);
            var patchParent = new GameObject("GrassPatches");
            patchParent.transform.SetParent(ground.transform);
            for (int i = 0; i < 24; i++)
            {
                var prefab = patches[i % 3];
                if (prefab == null) continue;
                float angle = i * (360f / 24f);
                float rad   = (float)(rng.NextDouble() * 5f + PenHalf + 1.5f);
                float x     = Mathf.Sin(angle * Mathf.Deg2Rad) * rad;
                float z     = Mathf.Cos(angle * Mathf.Deg2Rad) * rad;
                var t       = (GameObject)PrefabUtility.InstantiatePrefab(prefab, patchParent.transform);
                t.transform.position = new Vector3(x, 0f, z);
                t.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            }
        }

        static void PlaceWallColliders(Transform parent)
        {
            var walls = new GameObject("InvisibleWalls");
            walls.transform.SetParent(parent);

            // Four flat box colliders matching the square pen walls
            var wallDefs = new (Vector3 pos, float ry)[]
            {
                (new Vector3(       0f, 1f, -PenHalf), 0f),   // South
                (new Vector3( PenHalf, 1f,        0f), 90f),  // East
                (new Vector3(       0f, 1f,  PenHalf), 0f),   // North
                (new Vector3(-PenHalf, 1f,        0f), 90f),  // West
            };
            string[] names = { "Wall_South", "Wall_East", "Wall_North", "Wall_West" };
            for (int i = 0; i < wallDefs.Length; i++)
            {
                var go  = new GameObject(names[i]);
                go.transform.SetParent(walls.transform);
                go.transform.SetPositionAndRotation(wallDefs[i].pos,
                    Quaternion.Euler(0f, wallDefs[i].ry, 0f));
                var col  = go.AddComponent<BoxCollider>();
                col.size = new Vector3(PenHalf * 2f, 2f, 1f);
            }
        }

        /// <summary>
        /// Curved Synty fence segments in a ring at the pen edge (visual only — colliders stripped so
        /// <see cref="PlaceWallColliders"/> remains the gameplay boundary).
        /// </summary>
        static void PlaceCircularFence(Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FenceRoundPath);
            if (prefab == null)
                return;

            var root = new GameObject("FenceRing");
            root.transform.SetParent(parent);
            root.transform.localPosition = Vector3.zero;

            const int segments = 12;
            var center = Vector3.zero;
            for (int i = 0; i < segments; i++)
            {
                float angleDeg = i * (360f / segments);
                float rad      = angleDeg * Mathf.Deg2Rad;
                var pos = center + new Vector3(
                    Mathf.Sin(rad) * PenHalf,
                    0f,
                    Mathf.Cos(rad) * PenHalf);
                var rot = Quaternion.Euler(0f, angleDeg, 0f);

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                inst.name = $"Fence_{i:00}";
                inst.transform.SetPositionAndRotation(pos, rot);

                foreach (var col in inst.GetComponentsInChildren<Collider>())
                    Object.DestroyImmediate(col);
            }
        }

        static void PlaceProps(Transform parent)
        {
            var props = new GameObject("Props");
            props.transform.SetParent(parent);

            // Chicken coop — arena edge
            var coopPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoopPath);
            if (coopPrefab != null)
            {
                var coop = (GameObject)PrefabUtility.InstantiatePrefab(coopPrefab, props.transform);
                coop.transform.position = new Vector3(8f, 0f, -1.5f);
                coop.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                coop.transform.localScale = Vector3.one * 3f;
            }

            // A few trees for atmosphere
            var tree1 = AssetDatabase.LoadAssetAtPath<GameObject>(Tree1Path);
            var tree2 = AssetDatabase.LoadAssetAtPath<GameObject>(Tree2Path);
            var treePrefabs = new[] { tree1, tree2 };
            var treePositions = new Vector3[]
            {
                new(-PenHalf - 3f, 0f, -PenHalf - 2f),
                new( PenHalf + 3f, 0f, -PenHalf - 2f),
                new( PenHalf + 3f, 0f,  PenHalf + 2f),
                new(-PenHalf - 3f, 0f,  PenHalf + 2f),
                // Note: tree positions derive from PenHalf, so they always sit outside the fence
            };
            for (int i = 0; i < treePositions.Length; i++)
            {
                var prefab = treePrefabs[i % 2];
                if (prefab == null) continue;
                var t = (GameObject)PrefabUtility.InstantiatePrefab(prefab, props.transform);
                t.transform.position = treePositions[i];
                t.transform.rotation = Quaternion.Euler(0f, i * 73f, 0f);
            }
        }

        // ── Player ───────────────────────────────────────────────────────────

        static GameObject CreatePlayer()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(FarmgirlPath);
            var go    = model != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(model)
                : GameObject.CreatePrimitive(PrimitiveType.Capsule);

            go.name = "Player";
            go.tag  = "Player";
            go.transform.SetPositionAndRotation(new Vector3(0f, 0f, -4f), Quaternion.identity);

            var cc    = go.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.height = 1.8f;
            cc.radius = 0.35f;

            var controller = go.AddComponent<ChickenPlayerController>();

            // FPS camera — child of player at eye height
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            camGO.transform.SetParent(go.transform);
            camGO.transform.localPosition = new Vector3(0f, 1.8f, 0.1f);
            camGO.transform.localRotation = Quaternion.identity;

            var cam = camGO.AddComponent<Camera>();
            cam.fieldOfView   = 75f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane  = 150f;
            camGO.AddComponent<AudioListener>();

            controller.fpCamera = cam;

            return go;
        }

        // ── Chicken ──────────────────────────────────────────────────────────

        static GameObject CreateChicken()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ChickenPath);
            var go    = model != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(model)
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);

            go.name = "Chicken";
            go.transform.position = new Vector3(1f, 0.3f, 2f);

            var ai        = go.AddComponent<ChickenAI>();
            ai.arenaRadius = ArenaRadius;

            var cluck = go.AddComponent<ChickenCluckAudio>();
            var cluckSo = new SerializedObject(cluck);
            cluckSo.FindProperty("_chicken").objectReferenceValue = ai;
            var clipsProp = cluckSo.FindProperty("_cluckClips");
            clipsProp.arraySize = 3;
            clipsProp.GetArrayElementAtIndex(0).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(SfxBawkPath);
            clipsProp.GetArrayElementAtIndex(1).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(SfxChickletsPath);
            clipsProp.GetArrayElementAtIndex(2).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(SfxWingFlapPath);
            cluckSo.ApplyModifiedProperties();

            return go;
        }

        // ── Game Manager ─────────────────────────────────────────────────────

        static void CreateGameManager(GameObject chickenGO, Transform player)
        {
            var go  = new GameObject("ChickenGameManager");
            var mgr = go.AddComponent<ChickenGameManager>();
            mgr.chicken   = chickenGO.GetComponent<ChickenAI>();
            mgr.player    = player;
            mgr.timeLimit = 45f;

            var sceneAudio = go.AddComponent<ChickenGameSceneAudio>();
            var audioSo    = new SerializedObject(sceneAudio);
            audioSo.FindProperty("_introClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(SpeechIntroPath);
            audioSo.FindProperty("_musicClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(MusicBanjoPath);
            audioSo.FindProperty("_grabClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(SpeechGrabPath);
            audioSo.FindProperty("_dropClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(SpeechDropPath);
            audioSo.FindProperty("_victoryClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(SfxVictoryPath);
            audioSo.ApplyModifiedProperties();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        static void EnsureTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || IsBuiltInTag(tag))
                return;

            var tm   = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tags = tm.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            tm.ApplyModifiedProperties();
        }

        static bool IsBuiltInTag(string tag)
        {
            return tag is "Untagged"
                or "Respawn"
                or "Finish"
                or "EditorOnly"
                or "MainCamera"
                or "Player"
                or "GameController";
        }
    }
}
