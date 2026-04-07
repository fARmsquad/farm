using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace FarmSimVR.Editor
{
    public static class HuntingSceneSetup
    {
        [MenuItem("FarmSimVR/Setup Hunting Scene (Animal Pen)")]
        public static void SetupAnimalPen()
        {
            // Find or create AnimalPen GO
            var penObj = GameObject.Find("AnimalPen");
            if (penObj == null)
            {
                penObj = new GameObject("AnimalPen");
                penObj.transform.position = new Vector3(5, 0, 6.5f);
                Debug.Log("[Setup] Created AnimalPen GameObject");
            }

            // Add AnimalPen component
            Component penComp = null;
            foreach (var c in penObj.GetComponents<Component>())
                if (c.GetType().Name == "AnimalPen") penComp = c;

            if (penComp == null)
            {
                var type = System.Type.GetType("FarmSimVR.MonoBehaviours.Hunting.AnimalPen, FarmSimVR.MonoBehaviours");
                if (type != null)
                    penComp = penObj.AddComponent(type);
                else
                {
                    Debug.LogError("[Setup] Could not find AnimalPen type. Compilation error?");
                    return;
                }
            }

            // Configure pen
            var penSO = new SerializedObject(penComp);
            penSO.FindProperty("penCenter").vector3Value = new Vector3(5, 0, 6.5f);
            penSO.FindProperty("penRadius").floatValue = 3f;
            penSO.FindProperty("buildFenceOnStart").boolValue = true;

            // Map animal prefabs — must match AnimalType enum: Chicken=0, Cow=1, Horse=2, Pig=3, Sheep=4
            string[] names = { "Chicken", "Cow", "Horse", "Pig", "Sheep" };
            int[] types = { 0, 1, 2, 3, 4 };

            var prefabArray = penSO.FindProperty("penAnimalPrefabs");
            prefabArray.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                var element = prefabArray.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("type").enumValueIndex = types[i];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/_Project/Prefabs/Animals/{names[i]}.prefab");
                element.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            }
            penSO.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Setup] Configured AnimalPen with prefab mappings");

            // Wire into HuntingManager
            var mgrObj = GameObject.Find("HuntingManager");
            if (mgrObj != null)
            {
                foreach (var c in mgrObj.GetComponents<Component>())
                {
                    if (c.GetType().Name == "HuntingManager")
                    {
                        var mgrSO = new SerializedObject(c);
                        mgrSO.FindProperty("animalPen").objectReferenceValue = penComp;
                        mgrSO.ApplyModifiedPropertiesWithoutUndo();
                        Debug.Log("[Setup] Wired AnimalPen into HuntingManager");
                        break;
                    }
                }
            }

            // Update camera offset for better third-person feel
            var camObj = GameObject.Find("Main Camera");
            if (camObj != null)
            {
                foreach (var c in camObj.GetComponents<Component>())
                {
                    if (c.GetType().Name == "ThirdPersonCamera")
                    {
                        var camSO = new SerializedObject(c);
                        camSO.FindProperty("offset").vector3Value = new Vector3(0, 5, -7);
                        camSO.FindProperty("positionSmoothTime").floatValue = 0.15f;
                        camSO.FindProperty("rotationSmoothSpeed").floatValue = 10f;
                        camSO.ApplyModifiedPropertiesWithoutUndo();
                        Debug.Log("[Setup] Updated ThirdPersonCamera settings");
                        break;
                    }
                }
            }

            // Save
            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Setup] Scene saved! Press Play to test.");
        }
    }
}
