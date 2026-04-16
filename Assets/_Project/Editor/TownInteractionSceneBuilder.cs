using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Autoplay;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.Editor
{
    /// <summary>
    /// Builds the GeneratedTownInteraction scene: a small market square with stalls,
    /// townsfolk, and the farmer-girl player who auto-walks up for a chat.
    /// Menu: FarmSim > Town > Build Town Interaction Scene
    /// </summary>
    public static class TownInteractionSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/GeneratedTownInteraction.unity";
        private const string DialogueDataPath = "Assets/_Project/Data/TownChat_DialogueData.asset";

        // ── NPC spawn positions around a central market square ──────
        // Index 0 is Old Garrett (background NPC).
        // Index 1 is Mira the Baker — the demo auto-walks here so the player
        // meets the baker immediately and can sell eggs.
        private static readonly (Vector3 pos, float yRot, string name)[] NpcSlots =
        {
            (new Vector3( 6f, 0f,  4f),  200f, "Old Garrett"),
            (new Vector3(-5.5f, 0f, 4f), 150f, "Mira the Baker"),   // in front of Bread Stall
            (new Vector3( 0f, 0f,  8f),  180f, "Young Pip"),
        };

        [MenuItem("FarmSim/Town/Build Town Interaction Scene")]
        public static void Build()
        {
            // ── Open / create the scene ──────────────────────────────
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Input system event routing (required for mouse clicks on UGUI) ──
            EnsureEventSystem();

            // ── Environment ──────────────────────────────────────────
            BuildGround();
            BuildSky();
            BuildSunLight();
            BuildStalls();
            BuildDecoration();

            // ── Characters ──────────────────────────────────────────
            var dialogueData = EnsureDialogueData();
            var npcs = BuildNPCs(dialogueData);

            // ── Player (farm girl) ───────────────────────────────────
            var player = BuildPlayer();

            // ── Camera ───────────────────────────────────────────────
            BuildFollowCamera(player.transform);

            // ── Dialogue UI ──────────────────────────────────────────
            BuildDialogueUI();

            // ── Autoplay controller ──────────────────────────────────
            // Walk to Mira the Baker (index 1) so the player lands at the baker's stall.
            var targetNpc = npcs.Length > 1 ? npcs[1] : (npcs.Length > 0 ? npcs[0] : null);
            BuildAutoplay(player, targetNpc);

            // ── Save ─────────────────────────────────────────────────
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[TownInteractionSceneBuilder] Scene built and saved to " + ScenePath);
        }

        // ── Ground ────────────────────────────────────────────────────

        private static void BuildGround()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "Ground";
            go.transform.localScale = new Vector3(6f, 1f, 6f);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.55f, 0.45f, 0.28f);
            go.GetComponent<Renderer>().material = mat;
        }

        // ── Sky / lighting ────────────────────────────────────────────

        private static void BuildSky()
        {
            RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = new Color(0.6f, 0.75f, 0.95f);
            RenderSettings.ambientEquatorColor  = new Color(0.55f, 0.55f, 0.45f);
            RenderSettings.ambientGroundColor   = new Color(0.2f, 0.15f, 0.1f);
        }

        private static void BuildSunLight()
        {
            var go = new GameObject("Sun");
            go.transform.rotation = Quaternion.Euler(52f, -30f, 0f);
            var light = go.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.color     = new Color(1f, 0.95f, 0.82f);
            light.intensity = 1.1f;
            light.shadows   = LightShadows.Soft;
        }

        // ── Stalls ────────────────────────────────────────────────────

        private static void BuildStalls()
        {
            // Three market stalls arranged in a loose arc
            var stallDefs = new (Vector3 pos, float yRot, string label)[]
            {
                (new Vector3( 7f, 0f,  6f), 210f, "Produce Stand"),
                (new Vector3(-6f, 0f,  5f), 160f, "Bread Stall"),
                (new Vector3( 1f, 0f, 10f), 180f, "Goods Stall"),
            };

            foreach (var (pos, yRot, label) in stallDefs)
            {
                // Try Synty produce stand first, fall back to primitive
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Synty/PolygonFarm/Prefabs/Buildings/SM_Bld_ProduceStand_01.prefab");

                if (prefab != null)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    inst.name = label;
                    inst.transform.position = pos;
                    inst.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
                }
                else
                {
                    // Fallback: simple awning shape from primitives
                    BuildPrimitiveStall(label, pos, yRot);
                }

                // Add crate props alongside each stall
                PlaceProp("Assets/Synty/PolygonFarm/Prefabs/Props/SM_Prop_Crate_01.prefab",
                    pos + new Vector3(1.2f, 0f, 0.5f), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                PlaceProp("Assets/Synty/PolygonFarm/Prefabs/Props/SM_Prop_Barrel_02.prefab",
                    pos + new Vector3(-1.0f, 0f, 0.3f), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            }

            // A couple of hay bales for atmosphere
            PlaceProp("Assets/Synty/PolygonFarm/Prefabs/Props/SM_Prop_Hay_Bale_Round_01.prefab",
                new Vector3(-8f, 0f, -2f), Quaternion.Euler(0f, 30f, 0f));
            PlaceProp("Assets/Synty/PolygonFarm/Prefabs/Props/SM_Prop_Hay_Bale_Square_01.prefab",
                new Vector3(-7.5f, 0f, 0f), Quaternion.Euler(0f, 15f, 0f));

            // Sign post at the entrance
            PlaceProp("Assets/Synty/PolygonFarm/Prefabs/Props/SM_Prop_SignPost_02.prefab",
                new Vector3(0f, 0f, -8f), Quaternion.Euler(0f, 0f, 0f));

            // Well in the center of the square
            PlaceProp("Assets/Synty/PolygonFarm/Prefabs/Props/SM_Prop_Well_01.prefab",
                new Vector3(0f, 0f, 2f), Quaternion.identity);
        }

        private static void BuildPrimitiveStall(string name, Vector3 pos, float yRot)
        {
            var root = new GameObject(name);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            // Counter
            var counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "Counter";
            counter.transform.SetParent(root.transform, false);
            counter.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            counter.transform.localScale    = new Vector3(3f, 1f, 1f);

            // Roof awning
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(root.transform, false);
            roof.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            roof.transform.localScale    = new Vector3(3.4f, 0.15f, 1.6f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.7f, 0.25f, 0.15f);
            roof.GetComponent<Renderer>().material = mat;
        }

        // ── Decoration ────────────────────────────────────────────────

        private static void BuildDecoration()
        {
            // Trees around the perimeter
            var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Tree_Large_01.prefab");

            var treePositions = new Vector3[]
            {
                new(-14f, 0f, -12f),
                new( 14f, 0f, -12f),
                new(-14f, 0f,  14f),
                new( 14f, 0f,  14f),
                new(  0f, 0f,  16f),
            };

            foreach (var tp in treePositions)
            {
                if (treePrefab != null)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab);
                    inst.transform.position = tp;
                    inst.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                }
            }

            // Fence along the back edge
            var fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Synty/PolygonFarm/Prefabs/Props/SM_Prop_Fence_Wood_01.prefab");
            if (fencePrefab != null)
            {
                for (int i = -3; i <= 3; i++)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(fencePrefab);
                    inst.transform.position = new Vector3(i * 4f, 0f, 14f);
                }
            }
        }

        // ── NPCs ──────────────────────────────────────────────────────

        private static NPCController[] BuildNPCs(DialogueData dialogueData)
        {
            var result = new NPCController[NpcSlots.Length];

            for (int i = 0; i < NpcSlots.Length; i++)
            {
                var (pos, yRot, npcName) = NpcSlots[i];

                // Pick a character prefab — alternate between available ones
                string[] charPrefabPaths =
                {
                    "Assets/Synty/PolygonFarm/Prefabs/Characters/SM_Chr_Farmer_Male_01.prefab",
                    "Assets/Synty/PolygonFarm/Prefabs/Characters/SM_Chr_Farmer_Female_01.prefab",
                    "Assets/Synty/PolygonFarm/Prefabs/Characters/SM_Chr_Farmer_Male_Old_01.prefab",
                };
                string path = charPrefabPaths[i % charPrefabPaths.Length];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                GameObject npcGo;
                if (prefab != null)
                {
                    npcGo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    npcGo.name = npcName;
                    npcGo.transform.position = pos;
                    npcGo.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
                }
                else
                {
                    npcGo = BuildCapsuleNpc(npcName, pos, yRot);
                }

                var npc = npcGo.GetComponent<NPCController>() ?? npcGo.AddComponent<NPCController>();

                var so = new SerializedObject(npc);
                so.FindProperty("npcName").stringValue         = npcName;
                so.FindProperty("interactionRange").floatValue = 3.5f;
                // Only assign scripted dialogue to Old Garrett (index 0) as a fallback.
                if (i == 0 && dialogueData != null)
                    so.FindProperty("dialogueData").objectReferenceValue = dialogueData;
                so.ApplyModifiedPropertiesWithoutUndo();

                // Floating world-space name label — NPCController picks this up automatically.
                AddNameLabel(npcGo, npcName);

                result[i] = npc;
            }

            return result;
        }

        private static GameObject BuildCapsuleNpc(string npcName, Vector3 pos, float yRot)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = npcName;
            go.transform.position = pos + new Vector3(0f, 1f, 0f);
            go.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.4f, 0.6f, 0.85f);
            go.GetComponent<Renderer>().material = mat;

            return go;
        }

        // ── Player (farm girl) ────────────────────────────────────────

        private static GameObject BuildPlayer()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Synty/PolygonFarm/Prefabs/Characters/SM_Chr_FarmGirl_01.prefab");

            GameObject player;
            if (prefab != null)
            {
                player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                player.name = "Player_FarmGirl";
            }
            else
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player_FarmGirl";
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.85f, 0.55f, 0.3f);
                player.GetComponent<Renderer>().material = mat;
            }

            // Position at the entrance of the market square
            player.transform.position = new Vector3(0f, 0f, -6f);
            player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            player.tag = "Player";

            // CharacterController for NavMesh-free movement
            if (player.GetComponent<CharacterController>() == null)
            {
                var cc = player.AddComponent<CharacterController>();
                cc.height = 1.8f;
                cc.radius = 0.3f;
                cc.center = new Vector3(0f, 0.9f, 0f);
            }

            // Third-person camera anchor
            var camAnchor = new GameObject("CameraAnchor");
            camAnchor.transform.SetParent(player.transform);
            camAnchor.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            return player;
        }

        // ── Follow Camera ─────────────────────────────────────────────

        private static void BuildFollowCamera(Transform playerTransform)
        {
            // Remove any leftover audio listeners
            foreach (var al in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                Object.DestroyImmediate(al);

            var camGo = new GameObject("FollowCamera");
            camGo.tag = "MainCamera";

            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView   = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane  = 300f;
            cam.clearFlags    = CameraClearFlags.Skybox;
            camGo.AddComponent<UniversalAdditionalCameraData>();
            camGo.AddComponent<AudioListener>();

            // Initial position: behind and above the player
            camGo.transform.position = playerTransform.position + new Vector3(0f, 3.5f, -6f);
            camGo.transform.LookAt(playerTransform.position + Vector3.up);

            var follow = camGo.AddComponent<TownCameraFollow>();
            var followSo = new SerializedObject(follow);
            followSo.FindProperty("target").objectReferenceValue      = playerTransform;
            followSo.FindProperty("offset").vector3Value              = new Vector3(0f, 3.5f, -6f);
            followSo.FindProperty("smoothSpeed").floatValue           = 4f;
            followSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Dialogue UI ───────────────────────────────────────────────

        private static (DialogueManager mgr, Canvas canvas) BuildDialogueUI()
        {
            var root = new GameObject("DialogueCanvas");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight   = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            // Panel
            var panel = new GameObject("DialoguePanel");
            panel.transform.SetParent(root.transform, false);
            var pR = panel.AddComponent<RectTransform>();
            pR.anchorMin        = new Vector2(0.1f, 0f);
            pR.anchorMax        = new Vector2(0.9f, 0f);
            pR.pivot            = new Vector2(0.5f, 0f);
            pR.sizeDelta        = new Vector2(0f, 180f);
            pR.anchoredPosition = new Vector2(0f, 30f);
            var pBg = panel.AddComponent<Image>();
            pBg.color = new Color(0.04f, 0.04f, 0.09f, 0.88f);

            // Speaker name
            var spkGo = new GameObject("SpeakerName");
            spkGo.transform.SetParent(panel.transform, false);
            var spkR = spkGo.AddComponent<RectTransform>();
            spkR.anchorMin = new Vector2(0f, 0.78f); spkR.anchorMax = new Vector2(1f, 1f);
            spkR.sizeDelta = Vector2.zero; spkR.offsetMin = new Vector2(24f, 0f); spkR.offsetMax = new Vector2(-24f, -8f);
            var spkTmp = spkGo.AddComponent<TextMeshProUGUI>();
            spkTmp.fontSize = 26; spkTmp.fontStyle = FontStyles.Bold;
            spkTmp.color = Color.yellow; spkTmp.alignment = TextAlignmentOptions.BottomLeft;

            // Dialogue text
            var dlgGo = new GameObject("DialogueText");
            dlgGo.transform.SetParent(panel.transform, false);
            var dlgR = dlgGo.AddComponent<RectTransform>();
            dlgR.anchorMin = Vector2.zero; dlgR.anchorMax = new Vector2(1f, 0.78f);
            dlgR.sizeDelta = Vector2.zero; dlgR.offsetMin = new Vector2(24f, 12f); dlgR.offsetMax = new Vector2(-24f, -4f);
            var dlgTmp = dlgGo.AddComponent<TextMeshProUGUI>();
            dlgTmp.fontSize = 22; dlgTmp.color = Color.white;
            dlgTmp.alignment = TextAlignmentOptions.TopLeft; dlgTmp.enableWordWrapping = true;

            // Hint label
            var hintGo = new GameObject("HintLabel");
            hintGo.transform.SetParent(panel.transform, false);
            var hintR = hintGo.AddComponent<RectTransform>();
            hintR.anchorMin = new Vector2(1f, 0f); hintR.anchorMax = new Vector2(1f, 0f);
            hintR.pivot = Vector2.one; hintR.sizeDelta = new Vector2(220f, 28f);
            hintR.anchoredPosition = new Vector2(-12f, 10f);
            var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
            hintTmp.fontSize = 14; hintTmp.color = new Color(1f, 1f, 1f, 0.45f);
            hintTmp.alignment = TextAlignmentOptions.Right;
            hintTmp.text = "[E] next  [Space] skip";

            // Wire up DialogueManager
            var mgr = root.AddComponent<DialogueManager>();
            var so = new SerializedObject(mgr);
            so.FindProperty("dialogueCanvas").objectReferenceValue  = canvas;
            so.FindProperty("speakerNameText").objectReferenceValue = spkTmp;
            so.FindProperty("dialogueText").objectReferenceValue    = dlgTmp;
            so.FindProperty("panelBackground").objectReferenceValue = pBg;
            so.ApplyModifiedPropertiesWithoutUndo();

            return (mgr, canvas);
        }

        // ── Autoplay controller ───────────────────────────────────────

        private static void BuildAutoplay(
            GameObject player,
            NPCController targetNpc)
        {
            var go       = new GameObject("TownInteractionAutoplay");
            var autoplay = go.AddComponent<TownInteractionAutoplay>();
            var skip     = go.AddComponent<SkipPrompt>();

            // Quick skip: visible immediately, single short hold.
            var skipSo = new SerializedObject(skip);
            skipSo.FindProperty("showDelay").floatValue    = 0f;    // visible from first frame
            skipSo.FindProperty("holdDuration").floatValue = 0.3f;  // tap-and-hold to skip
            skipSo.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(autoplay);
            so.FindProperty("playerTransform").objectReferenceValue = player.transform;
            so.FindProperty("targetNpc").objectReferenceValue       = targetNpc;
            so.FindProperty("skipPrompt").objectReferenceValue      = skip;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddNameLabel(GameObject npcGo, string npcName)
        {
            var labelGO = new GameObject("NameTag");
            labelGO.transform.SetParent(npcGo.transform, false);
            labelGO.transform.localPosition = new Vector3(0f, 2.6f, 0f);

            var mesh = labelGO.AddComponent<TextMesh>();
            mesh.text          = npcName;
            mesh.characterSize = 0.09f;
            mesh.fontSize      = 32;
            mesh.anchor        = TextAnchor.MiddleCenter;
            mesh.alignment     = TextAlignment.Center;
            mesh.color         = new Color(1f, 0.92f, 0.55f);
        }

        // ── Event system ──────────────────────────────────────────────

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        // ── DialogueData asset ────────────────────────────────────────

        private static DialogueData EnsureDialogueData()
        {
            // Reuse existing asset if already created
            var existing = AssetDatabase.LoadAssetAtPath<DialogueData>(DialogueDataPath);
            if (existing != null) return existing;

            var data = ScriptableObject.CreateInstance<DialogueData>();
            data.lines = new DialogueLine[]
            {
                new DialogueLine
                {
                    speakerName  = "Old Garrett",
                    text         = "Well I never! If it isn't the farmer girl from up the road. \nHow's the harvest treating you this season?",
                    duration     = 4f,
                    autoAdvance  = true,
                    speakerColor = new Color(1f, 0.85f, 0.4f),
                },
                new DialogueLine
                {
                    speakerName  = "Farm Girl",
                    text         = "Not bad, Garrett! The soil's been kind. \nI was hoping to trade some extra turnips for a bit of flour.",
                    duration     = 4f,
                    autoAdvance  = true,
                    speakerColor = new Color(0.55f, 0.9f, 0.55f),
                },
                new DialogueLine
                {
                    speakerName  = "Old Garrett",
                    text         = "Ha! Turnips for flour — now that's a deal I haven't heard since old Bram's day. \nMira's your girl for flour. She's just over there by the bread stall.",
                    duration     = 5f,
                    autoAdvance  = true,
                    speakerColor = new Color(1f, 0.85f, 0.4f),
                },
                new DialogueLine
                {
                    speakerName  = "Farm Girl",
                    text         = "Perfect. Oh, and have you seen any good rain coming? \nMy onions could use a proper soak.",
                    duration     = 3.5f,
                    autoAdvance  = true,
                    speakerColor = new Color(0.55f, 0.9f, 0.55f),
                },
                new DialogueLine
                {
                    speakerName  = "Old Garrett",
                    text         = "Weather-caster in the square says two days, maybe three. \nBest plant what you can before then. Safe travels, girl!",
                    duration     = 4.5f,
                    autoAdvance  = true,
                    speakerColor = new Color(1f, 0.85f, 0.4f),
                },
            };

            // Ensure the directory exists
            System.IO.Directory.CreateDirectory("Assets/_Project/Data");
            AssetDatabase.CreateAsset(data, DialogueDataPath);
            AssetDatabase.SaveAssets();

            return data;
        }

        // ── Prop helper ───────────────────────────────────────────────

        private static void PlaceProp(string prefabPath, Vector3 pos, Quaternion rot)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.position = pos;
            inst.transform.rotation = rot;
        }
    }
}
