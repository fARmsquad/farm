using System.Collections;
using UnityEngine;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.MonoBehaviours.Autoplay
{
    public class AutoplayNPC : AutoplayBase
    {
        private void Awake()
        {
            specId = "INT-006";
            specTitle = "NPC Controller";
            totalSteps = 4;
        }

        protected override IEnumerator RunDemo()
        {
            Step("Spawn Mayor NPC");
            var npc1 = SpawnNpc("Mayor", new Color(0.2f, 0.6f, 1f), new Vector3(3f, 0f, 5f),
                "Welcome to Willowbrook!", "The farm needs a good hand.", "I believe in you!");
            yield return Wait(2f);

            Step("Spawn Farmer NPC");
            var npc2 = SpawnNpc("Old Farmer", new Color(0.4f, 0.8f, 0.2f), new Vector3(-3f, 0f, 5f),
                "This land has good soil.", "Treat it well and it'll treat you well.");
            yield return Wait(2f);

            Step("Trigger Mayor dialogue");
            var dm = DialogueManager.Instance;
            if (dm != null)
            {
                var data = MakeDialogue("Mayor", new Color(0.2f, 0.6f, 1f),
                    "Hello there! I'm the Mayor of Willowbrook.",
                    "NPCs face the player, show interaction prompts, and trigger dialogue.",
                    "Each NPC can have unique dialogue, colors, and interaction ranges.");
                bool done = false;
                dm.OnDialogueComplete.AddListener(() => done = true);
                dm.StartDialogue(data);
                yield return new WaitUntil(() => done);
                dm.OnDialogueComplete.RemoveAllListeners();
            }
            yield return Wait(1f);

            Step("Remove all NPCs");
            if (npc1 != null) Destroy(npc1);
            if (npc2 != null) Destroy(npc2);
            yield return Wait(1f);
        }

        private GameObject SpawnNpc(string npcName, Color color, Vector3 pos, params string[] lines)
        {
            var root = new GameObject($"NPC_{npcName}");
            root.transform.position = pos;

            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(root.transform, false);
            capsule.GetComponent<Renderer>().material.color = color;

            var nameGo = new GameObject("NameTag");
            nameGo.transform.SetParent(root.transform, false);
            nameGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            var tm = nameGo.AddComponent<TextMesh>();
            tm.text = npcName;
            tm.fontSize = 32;
            tm.characterSize = 0.15f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = color;

            var npc = root.AddComponent<NPCController>();
            var t = npc.GetType();
            SetField(t, npc, "npcName", npcName);
            SetField(t, npc, "capsuleColor", color);
            SetField(t, npc, "interactionRange", 5f);
            SetField(t, npc, "dialogueData", MakeDialogue(npcName, color, lines));

            return root;
        }

        private static DialogueData MakeDialogue(string speaker, Color color, params string[] lines)
        {
            var data = ScriptableObject.CreateInstance<DialogueData>();
            var dl = new DialogueLine[lines.Length];
            for (int i = 0; i < lines.Length; i++)
                dl[i] = new DialogueLine { speakerName = speaker, text = lines[i], autoAdvance = true, duration = 2.5f, speakerColor = color };
            data.lines = dl;
            return data;
        }

        private static void SetField(System.Type type, object obj, string field, object value)
        {
            type.GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(obj, value);
        }
    }
}
