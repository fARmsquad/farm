using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace FarmSimVR.Editor
{
    /// <summary>
    /// One-click setup for the HuntingTest scene.
    /// Idempotent — safe to run multiple times. Fixes broken references.
    /// </summary>
    public static class HuntingSceneSetup
    {
        // AnimalType enum: Chicken=0, Cow=1, Horse=2, Pig=3, Sheep=4
        static readonly string[] AnimalNames = { "Chicken", "Cow", "Horse", "Pig", "Sheep" };

        [MenuItem("FarmSimVR/Setup Hunting Scene (Full)")]
        public static void FullSetup()
        {
            int fixes = 0;

            // --- Load all animal prefabs ---
            GameObject[] prefabs = new GameObject[AnimalNames.Length];
            for (int i = 0; i < AnimalNames.Length; i++)
            {
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/_Project/Prefabs/Animals/{AnimalNames[i]}.prefab");
                if (prefabs[i] == null)
                    Debug.LogError($"[Setup] Missing prefab: {AnimalNames[i]}");
            }

            // --- Fix WildAnimalSpawner prefab array ---
            var spawnerObj = GameObject.Find("WildAnimalSpawner");
            if (spawnerObj != null)
            {
                foreach (var c in spawnerObj.GetComponents<Component>())
                {
                    if (c.GetType().Name == "WildAnimalSpawner")
                    {
                        var so = new SerializedObject(c);
                        var arr = so.FindProperty("animalPrefabs");
                        arr.arraySize = prefabs.Length;
                        for (int i = 0; i < prefabs.Length; i++)
                            arr.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];

                        // Also wire player transform
                        var player = GameObject.Find("Player");
                        if (player != null)
                            so.FindProperty("playerTransform").objectReferenceValue = player.transform;

                        // Wire config
                        var config = AssetDatabase.LoadAssetAtPath<Object>("Assets/_Project/Data/HuntingConfig.asset");
                        if (config != null)
                            so.FindProperty("config").objectReferenceValue = config;

                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(c);
                        fixes++;
                        Debug.Log($"[Setup] WildAnimalSpawner: wired {prefabs.Length} prefabs");
                        break;
                    }
                }
            }

            // --- Setup AnimalPen ---
            var penObj = GameObject.Find("AnimalPen");
            if (penObj == null)
            {
                penObj = new GameObject("AnimalPen");
                penObj.transform.position = new Vector3(5, 0, 6.5f);
                Debug.Log("[Setup] Created AnimalPen");
            }

            Component penComp = FindComponent(penObj, "AnimalPen");
            if (penComp == null)
            {
                var type = System.Type.GetType("FarmSimVR.MonoBehaviours.Hunting.AnimalPen, FarmSimVR.MonoBehaviours");
                if (type != null) penComp = penObj.AddComponent(type);
            }

            if (penComp != null)
            {
                var so = new SerializedObject(penComp);
                so.FindProperty("penCenter").vector3Value = new Vector3(5, 0, 6.5f);
                so.FindProperty("penRadius").floatValue = 3f;
                so.FindProperty("buildFenceOnStart").boolValue = true;

                var arr = so.FindProperty("penAnimalPrefabs");
                arr.arraySize = AnimalNames.Length;
                for (int i = 0; i < AnimalNames.Length; i++)
                {
                    var el = arr.GetArrayElementAtIndex(i);
                    el.FindPropertyRelative("type").enumValueIndex = i;
                    el.FindPropertyRelative("prefab").objectReferenceValue = prefabs[i];
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(penComp);
                fixes++;
                Debug.Log("[Setup] AnimalPen: configured with prefab mappings");
            }

            // --- Wire HuntingManager ---
            var mgrObj = GameObject.Find("HuntingManager");
            if (mgrObj != null)
            {
                var mgrComp = FindComponent(mgrObj, "HuntingManager");
                if (mgrComp != null)
                {
                    var so = new SerializedObject(mgrComp);
                    WireIfFound(so, "spawner", spawnerObj, "WildAnimalSpawner");
                    WireIfFound(so, "barnDropOff", GameObject.Find("BarnDropOff"), "BarnDropOff");
                    WireIfFound(so, "hud", GameObject.Find("HuntingHUD"), "HuntingHUD");
                    WireIfFound(so, "playerInput", GameObject.Find("Player"), "KeyboardPlayerInput");
                    if (penComp != null)
                        so.FindProperty("animalPen").objectReferenceValue = penComp;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(mgrComp);
                    fixes++;
                    Debug.Log("[Setup] HuntingManager: all references wired");
                }
            }

            // --- Camera ---
            var camObj = GameObject.Find("Main Camera");
            if (camObj != null)
            {
                var camComp = FindComponent(camObj, "ThirdPersonCamera");
                if (camComp != null)
                {
                    var so = new SerializedObject(camComp);
                    var player = GameObject.Find("Player");
                    if (player != null)
                        so.FindProperty("target").objectReferenceValue = player.transform;
                    so.FindProperty("offset").vector3Value = new Vector3(0, 10, -7);
                    so.FindProperty("smoothTime").floatValue = 0.2f;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(camComp);
                    fixes++;
                }
            }

            // --- Debug Tools ---
            AddDebugToolsInternal();
            fixes++;

            // --- Save ---
            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[Setup] Done! Applied {fixes} fixes. Scene saved.");
        }

        [MenuItem("FarmSimVR/Add Debug Tools to Scene")]
        public static void AddDebugTools()
        {
            AddDebugToolsInternal();
            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Setup] Debug tools added and scene saved.");
        }

        static void AddDebugToolsInternal()
        {
            // GameStateLogger
            if (GameObject.Find("GameStateLogger") == null)
            {
                var obj = new GameObject("GameStateLogger");
                var type = System.Type.GetType(
                    "FarmSimVR.MonoBehaviours.Diagnostics.GameStateLogger, FarmSimVR.MonoBehaviours");
                if (type != null)
                {
                    obj.AddComponent(type);
                    Debug.Log("[Setup] Added GameStateLogger");
                }
                else
                    Debug.LogError("[Setup] GameStateLogger type not found");
            }

            // Note: yasirkula IngameDebugConsole removed from auto-setup.
            // If needed, drag the prefab from Packages manually into the scene.
            // It requires a Canvas + EventSystem to work properly.
        }

        static Component FindComponent(GameObject obj, string typeName)
        {
            if (obj == null) return null;
            foreach (var c in obj.GetComponents<Component>())
                if (c != null && c.GetType().Name == typeName)
                    return c;
            return null;
        }

        static void WireIfFound(SerializedObject so, string propName, GameObject targetObj, string compName)
        {
            if (targetObj == null) return;
            var comp = FindComponent(targetObj, compName);
            if (comp != null)
                so.FindProperty(propName).objectReferenceValue = comp;
        }
    }
}
