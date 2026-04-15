using UnityEngine;
using UnityEngine.InputSystem;
using FarmSimVR.MonoBehaviours.Debugging;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    public class DialogueDemo : MonoBehaviour
    {
        private DialogueManager dialogueManager;
        private static readonly Key Panel = DebugPanelShortcuts.Dialogue;

        private void Start()
        {
            TryFind();
            Debug.Log($"[DialogueDemo] Start — dialogueManager={(dialogueManager != null ? "found" : "NULL")}");
        }

        private void TryFind()
        {
            if (dialogueManager == null)
            {
                var found = FindObjectsByType<DialogueManager>(FindObjectsSortMode.None);
                if (found.Length > 0) dialogueManager = found[0];
            }
        }

        private void Update()
        {
            if (!DebugPanelShortcuts.UpdateToggle(Panel)) return;
            TryFind();

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit1)) { Debug.Log("[Dialogue] Manual"); OnStartManualDialogue(); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit2)) { Debug.Log("[Dialogue] Auto"); OnStartAutoDialogue(); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit3)) { Debug.Log("[Dialogue] Show"); dialogueManager?.Show(); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit4)) { Debug.Log("[Dialogue] Hide"); dialogueManager?.Hide(); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit5)) { Debug.Log("[Dialogue] Skip"); dialogueManager?.AdvanceToNextLine(); }
        }

        private void OnGUI()
        {
            if (!DebugPanelShortcuts.IsPanelActive(Panel)) return;

            float w = 310f; float h = 230f;
            float x = 10f; float y = Screen.height - h - 10f;
            float btnH = 28f; float pad = 3f;

            GUI.Box(new Rect(x, y, w, h), "Dialogue System (Shift+3)");
            float cy = y + 22f;

            string status = dialogueManager != null ? (dialogueManager.IsPlaying ? "Playing" : "Idle") : "NULL";
            GUI.Label(new Rect(x+4, cy, w-8, 20f), $"Status: {status}");
            cy += 24f;

            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[1] Manual Dialogue")) OnStartManualDialogue();   cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[2] Auto-Advance Dialogue")) OnStartAutoDialogue(); cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[3] Show Dialogue Box")) dialogueManager?.Show();  cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[4] Hide Dialogue Box")) dialogueManager?.Hide();  cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[5] Skip Line")) dialogueManager?.AdvanceToNextLine();
        }

        private void OnStartManualDialogue()
        {
            var data = ScriptableObject.CreateInstance<DialogueData>();
            data.lines = new DialogueLine[]
            {
                new DialogueLine { speakerName = "Mayor", text = "Welcome to Willowbrook, friend!", duration = 3f, autoAdvance = false, speakerColor = new Color(0.2f, 0.6f, 1f) },
                new DialogueLine { speakerName = "Farmer", text = "This old farm needs a lot of work...", duration = 3f, autoAdvance = false, speakerColor = new Color(0.4f, 0.8f, 0.2f) },
                new DialogueLine { speakerName = "Mayor", text = "I'm sure you'll have it running in no time!", duration = 3f, autoAdvance = false, speakerColor = new Color(0.2f, 0.6f, 1f) },
            };
            dialogueManager?.StartDialogue(data);
        }

        private void OnStartAutoDialogue()
        {
            var data = ScriptableObject.CreateInstance<DialogueData>();
            data.lines = new DialogueLine[]
            {
                new DialogueLine { speakerName = "Narrator", text = "The sun rises over Willowbrook valley...", duration = 2f, autoAdvance = true, speakerColor = new Color(1f, 0.85f, 0.4f) },
                new DialogueLine { speakerName = "Narrator", text = "A new farmer arrives at the old McTavish homestead.", duration = 2f, autoAdvance = true, speakerColor = new Color(1f, 0.85f, 0.4f) },
                new DialogueLine { speakerName = "Narrator", text = "And so the adventure begins.", duration = 2f, autoAdvance = true, speakerColor = new Color(1f, 0.85f, 0.4f) },
            };
            dialogueManager?.StartDialogue(data);
        }
    }
}
