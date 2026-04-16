using FarmSimVR.MonoBehaviours.UI;
using UnityEditor;
using UnityEngine;

namespace FarmSimVR.Editor
{
    /// <summary>
    /// One-shot: builds <c>Assets/_Project/Prefabs/UI/NpcNameplate.prefab</c> from <see cref="NpcNameplateFactory"/>.
    /// </summary>
    public static class CreateNpcNameplatePrefab
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/NpcNameplate.prefab";

        [MenuItem("FarmSimVR/Town/Create Npc Nameplate Prefab")]
        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/UI"))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "UI");

            var holder = new GameObject("_NpcNameplateExport");
            GameObject plate = NpcNameplateFactory.CreateNameplate(holder.transform, Vector3.zero);
            plate.transform.SetParent(null, false);

            PrefabUtility.SaveAsPrefabAsset(plate, PrefabPath);
            Object.DestroyImmediate(plate);
            Object.DestroyImmediate(holder);

            AssetDatabase.Refresh();
        }
    }
}
