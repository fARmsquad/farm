using FarmSimVR.MonoBehaviours.Cinematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmSimVR.Editor
{
    /// <summary>
    /// Finds every NPCController in CoreScene.unity, adds an Animator if missing,
    /// assigns FarmGirlAnimator.controller so NPCs play their idle animation,
    /// and wires the Animator reference back into NPCController.
    ///
    /// Menu: fARm/Town/Wire NPC Idle Animators
    ///
    /// Safe to re-run — idempotent.
    /// </summary>
    public static class TownNpcAnimatorBuilder
    {
        private const string TownScenePath      = "Assets/_Project/Scenes/Town.unity";
        private const string AnimControllerPath = "Assets/_Project/Animations/FarmGirl/FarmGirlAnimator.controller";

        [MenuItem("fARm/Town/Wire NPC Idle Animators")]
        public static void Build()
        {
            var idleController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimControllerPath);
            if (idleController == null)
            {
                Debug.LogError($"[TownNpcAnimatorBuilder] Animator controller not found at {AnimControllerPath}");
                return;
            }

            // Open Town.unity additively if not already loaded — NPCs live there
            var townScene = SceneManager.GetSceneByPath(TownScenePath);
            if (!townScene.isLoaded)
            {
                townScene = EditorSceneManager.OpenScene(TownScenePath, OpenSceneMode.Additive);
                Debug.Log("[TownNpcAnimatorBuilder] Opened Town.unity additively.");
            }

            SceneManager.SetActiveScene(townScene);

            var npcs = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None);
            if (npcs.Length == 0)
            {
                Debug.LogWarning("[TownNpcAnimatorBuilder] No NPCControllers found in CoreScene.");
                return;
            }

            int wired = 0;
            foreach (var npc in npcs)
            {
                // Prefer an Animator already on the NPC or any child; add one to root if absent
                var anim = npc.GetComponentInChildren<Animator>();
                if (anim == null)
                    anim = npc.gameObject.AddComponent<Animator>();

                anim.runtimeAnimatorController = idleController;

                var so = new SerializedObject(npc);
                so.FindProperty("animator").objectReferenceValue = anim;
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(npc.gameObject);
                wired++;
            }

            EditorSceneManager.SaveScene(townScene);
            Debug.Log($"[TownNpcAnimatorBuilder] Wired idle animator on {wired} NPC(s) in Town.unity.");
        }
    }
}
