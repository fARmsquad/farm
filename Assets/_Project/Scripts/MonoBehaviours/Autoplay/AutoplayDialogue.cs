using System.Collections;
using UnityEngine;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.MonoBehaviours.Autoplay
{
    public class AutoplayDialogue : AutoplayBase
    {
        private void Awake()
        {
            specId = "INT-003";
            specTitle = "Dialogue System";
            totalSteps = 3;
        }

        protected override IEnumerator RunDemo()
        {
            var dm = DialogueManager.Instance;
            if (dm == null) { currentLabel = "DialogueManager not found!"; yield break; }

            // Manual dialogue
            Step("Manual dialogue (3 lines, auto-advancing)");
            var data1 = ScriptableObject.CreateInstance<DialogueData>();
            data1.lines = new DialogueLine[]
            {
                new() { speakerName = "Mayor", text = "Welcome to Willowbrook! Our town has a long history of farming.", autoAdvance = true, duration = 3f, speakerColor = new Color(0.2f, 0.6f, 1f) },
                new() { speakerName = "Farmer", text = "The soil here is the best in the valley. You'll love it.", autoAdvance = true, duration = 3f, speakerColor = new Color(0.4f, 0.8f, 0.2f) },
                new() { speakerName = "Mayor", text = "Take your time settling in. We're glad to have you!", autoAdvance = true, duration = 3f, speakerColor = new Color(0.2f, 0.6f, 1f) },
            };
            bool done = false;
            dm.OnDialogueComplete.AddListener(() => done = true);
            dm.StartDialogue(data1);
            yield return new WaitUntil(() => done);
            dm.OnDialogueComplete.RemoveAllListeners();
            yield return Wait(1f);

            // Different speakers
            Step("Multi-speaker exchange");
            var data2 = ScriptableObject.CreateInstance<DialogueData>();
            data2.lines = new DialogueLine[]
            {
                new() { speakerName = "Merchant", text = "I've got seeds, tools, and supplies for your farm.", autoAdvance = true, duration = 2.5f, speakerColor = new Color(1f, 0.6f, 0.1f) },
                new() { speakerName = "Mayor", text = "The merchant's prices are fair. Trust me on that.", autoAdvance = true, duration = 2.5f, speakerColor = new Color(0.2f, 0.6f, 1f) },
                new() { speakerName = "Merchant", text = "Come back anytime you need something!", autoAdvance = true, duration = 2.5f, speakerColor = new Color(1f, 0.6f, 0.1f) },
            };
            done = false;
            dm.OnDialogueComplete.AddListener(() => done = true);
            dm.StartDialogue(data2);
            yield return new WaitUntil(() => done);
            dm.OnDialogueComplete.RemoveAllListeners();
            yield return Wait(1f);

            // Narrator style
            Step("Narrator (single speaker, longer text)");
            var data3 = ScriptableObject.CreateInstance<DialogueData>();
            data3.lines = new DialogueLine[]
            {
                new() { speakerName = "Narrator", text = "The dialogue system supports typewriter text, per-line speaker colors, auto-advance timing, and manual advance via Space or E.", autoAdvance = true, duration = 4f, speakerColor = Color.white },
                new() { speakerName = "Narrator", text = "It's built as a singleton with a Canvas overlay, making it easy to trigger from any script or NPC interaction.", autoAdvance = true, duration = 4f, speakerColor = Color.white },
            };
            done = false;
            dm.OnDialogueComplete.AddListener(() => done = true);
            dm.StartDialogue(data3);
            yield return new WaitUntil(() => done);
            dm.OnDialogueComplete.RemoveAllListeners();
        }
    }
}
