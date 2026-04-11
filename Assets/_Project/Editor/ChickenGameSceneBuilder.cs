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
        private const string FencePath        = "Assets/Synty/PolygonFarm/Prefabs/Props/SM_Prop_Fence_Wood_01.prefab";
        private const string FencePolePath   = "Assets/Synty/PolygonFarm/Prefabs/Props/SM_Prop_Fence_Wood_Pole_01.prefab";
        private const string FenceGatePath   = "Assets/Synty/PolygonFarm/Prefabs/Props/SM_Prop_Fence_Wood_Gate_01.prefab";
        private const string GroundPath     = "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Ground_Flat_01.prefab";
        private const string DirtPath       = "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Ground_Dirt_01.prefab";
        private const string GrassPatch1    = "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Grass_Patch_01.prefab";
        private const string GrassPatch2    = "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Grass_Patch_02.prefab";
        private const string GrassPatch3    = "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Grass_Patch_03.prefab";
        private const string CoopPath       = "Assets/_Project/Prefabs/Models/coop.prefab";
        private const string Tree1Path      = "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Tree_01.prefab";
        private const string Tree2Path      = "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Tree_02.prefab";
        private const string SkyboxPath     = "Assets/_Project/Materials/SkyboxProcedural.mat";

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
            PlaceFence(env.transform);
            PlaceWallColliders(env.transform);
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

        static void PlaceFence(Transform parent)
        {
            var fenceModel  = AssetDatabase.LoadAssetAtPath<GameObject>(FencePath);
            var polePrefab  = AssetDatabase.LoadAssetAtPath<GameObject>(FencePolePath);
            var gatePrefab  = AssetDatabase.LoadAssetAtPath<GameObject>(FenceGatePath);

            var fenceParent = new GameObject("Fence");
            fenceParent.transform.SetParent(parent);

            // Square pen — four straight walls, corners at ±PenHalf
            // faceAngle: the Y-rotation that makes the panel face INWARD
            //   South wall (z=-PenHalf): faces +Z → 0°
            //   East  wall (x=+PenHalf): faces -X → 270°
            //   North wall (z=+PenHalf): faces -Z → 180°
            //   West  wall (x=-PenHalf): faces +X → 90°
            PlaceWall(fenceParent.transform, fenceModel, polePrefab, gatePrefab,
                new Vector3(-PenHalf, 0, -PenHalf), new Vector3(PenHalf, 0, -PenHalf),   0f, addGate: true);
            PlaceWall(fenceParent.transform, fenceModel, polePrefab, null,
                new Vector3( PenHalf, 0, -PenHalf), new Vector3(PenHalf, 0,  PenHalf), 270f, addGate: false);
            PlaceWall(fenceParent.transform, fenceModel, polePrefab, null,
                new Vector3( PenHalf, 0,  PenHalf), new Vector3(-PenHalf, 0, PenHalf), 180f, addGate: false);
            PlaceWall(fenceParent.transform, fenceModel, polePrefab, null,
                new Vector3(-PenHalf, 0,  PenHalf), new Vector3(-PenHalf, 0, -PenHalf),  90f, addGate: false);
        }

        static void PlaceWall(Transform parent, GameObject fencePrefab, GameObject polePrefab,
            GameObject gatePrefab, Vector3 start, Vector3 end, float faceAngle, bool addGate)
        {
            // Corner post at the start of this wall
            if (polePrefab != null)
            {
                var corner = (GameObject)PrefabUtility.InstantiatePrefab(polePrefab, parent);
                corner.transform.SetPositionAndRotation(start, Quaternion.Euler(0f, faceAngle, 0f));
            }

            if (fencePrefab == null) return;

            float wallLength = Vector3.Distance(start, end);
            Vector3 dir      = (end - start).normalized;
            var rot          = Quaternion.Euler(0f, faceAngle, 0f);

            // SM_Prop_Fence_Wood_01: pivot at right edge, extends 2.5 units left in local -X.
            // Panels must be placed going END→START so each panel fills from its pivot
            // backward, tiling correctly along the wall.
            const float PanelWidth = 2.5f;
            int   count            = Mathf.CeilToInt(wallLength / PanelWidth); // 5 for 12-unit wall
            float step             = wallLength / count;                        // 2.4

            // Gate centred at wall midpoint
            var wallMid = (start + end) * 0.5f;
            if (addGate && gatePrefab != null)
            {
                var gate = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab, parent);
                gate.transform.SetPositionAndRotation(wallMid, rot);
            }
            const float GateHalfW = 1.5f; // 3-unit clear zone for the gate

            for (int i = 0; i < count; i++)
            {
                // Pivot placed from end back toward start
                Vector3 pos = start + dir * (wallLength - step * i);
                pos.y = 0f;

                // Skip panels whose pivot falls inside the gate exclusion zone
                if (addGate)
                {
                    float t = Vector3.Dot(pos - wallMid, dir);
                    if (Mathf.Abs(t) < GateHalfW + PanelWidth * 0.5f) continue;
                }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(fencePrefab, parent);
                go.name = "FencePanel";
                go.transform.SetPositionAndRotation(pos, rot);

                // Mid-pole every 2 panels
                if (polePrefab != null && i > 0 && i % 2 == 0)
                {
                    var mp  = (GameObject)PrefabUtility.InstantiatePrefab(polePrefab, parent);
                    mp.transform.SetPositionAndRotation(pos, rot);
                }
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

        static void PlaceProps(Transform parent)
        {
            var props = new GameObject("Props");
            props.transform.SetParent(parent);

            // Chicken coop — just outside the fence
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
