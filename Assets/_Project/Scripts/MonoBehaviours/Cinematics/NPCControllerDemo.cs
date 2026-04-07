using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using FarmSimVR.MonoBehaviours.Debugging;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    public class NPCControllerDemo : MonoBehaviour
    {
        private readonly List<NPCController> spawnedNPCs = new List<NPCController>();
        private static readonly Key Panel = DebugPanelShortcuts.NPCController;

        private void Update()
        {
            DebugPanelShortcuts.UpdateInput();
            if (!DebugPanelShortcuts.IsPanelActive(Panel)) return;

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit1)) OnSpawnMayor();
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit2)) OnSpawnFarmer();
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit3)) OnSpawnMerchant();
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit4)) OnRemoveAllNPCs();
        }

        private void OnGUI()
        {
            DebugPanelShortcuts.DrawMasterMenu();
            if (!DebugPanelShortcuts.IsPanelActive(Panel)) return;

            float w = 280f; float h = 200f;
            float x = 10f; float y = (Screen.height - h) / 2f;
            float btnH = 28f; float pad = 3f;

            GUI.Box(new Rect(x, y, w, h), "NPC Controller (Tab to close)");
            float cy = y + 22f;

            GUI.Label(new Rect(x+4, cy, w-8, 20f), $"Spawned NPCs: {spawnedNPCs.Count}");
            cy += 24f;

            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[1] Spawn Mayor NPC")) OnSpawnMayor();       cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[2] Spawn Farmer NPC")) OnSpawnFarmer();      cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[3] Spawn Merchant NPC")) OnSpawnMerchant();  cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[4] Remove All NPCs")) OnRemoveAllNPCs();
        }

        private void OnSpawnMayor() => SpawnNPC("Mayor", new Color(0.2f, 0.6f, 1f), new Vector3(60f, 0f, 30f), MakeSampleDialogue("Mayor", "Welcome to Willowbrook!", "The farm needs a good hand.", "I believe in you, friend."));
        private void OnSpawnFarmer() => SpawnNPC("Old Farmer", new Color(0.4f, 0.8f, 0.2f), new Vector3(65f, 0f, 30f), MakeSampleDialogue("Old Farmer", "This land has good soil.", "Treat it well and it'll treat you well."));
        private void OnSpawnMerchant() => SpawnNPC("Merchant", new Color(1f, 0.6f, 0.1f), new Vector3(55f, 0f, 30f), MakeSampleDialogue("Merchant", "I've got seeds, tools, and supplies.", "Come back when you need something."));

        private void OnRemoveAllNPCs()
        {
            foreach (var npc in spawnedNPCs)
                if (npc != null) Destroy(npc.gameObject);
            spawnedNPCs.Clear();
        }

        private void SpawnNPC(string npcName, Color color, Vector3 pos, DialogueData dialogue)
        {
            float terrainY = Terrain.activeTerrain != null ? Terrain.activeTerrain.SampleHeight(pos) : 0f;
            pos.y = terrainY + 1f;

            var root = new GameObject($"NPC_{npcName}");
            root.transform.position = pos;

            // Capsule
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Capsule";
            capsule.transform.SetParent(root.transform, false);
            capsule.GetComponent<Renderer>().material.color = color;

            // Name tag
            var nameTag = new GameObject("NameTag");
            nameTag.transform.SetParent(root.transform, false);
            nameTag.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            var tm = nameTag.AddComponent<TextMesh>();
            tm.text = npcName;
            tm.fontSize = 32;
            tm.characterSize = 0.15f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;

            // Prompt canvas
            var promptGo = new GameObject("PromptCanvas");
            promptGo.transform.SetParent(root.transform, false);
            promptGo.transform.localPosition = new Vector3(0f, 2.8f, 0f);
            var promptCanvas = promptGo.AddComponent<Canvas>();
            promptCanvas.renderMode = RenderMode.WorldSpace;
            promptGo.AddComponent<CanvasScaler>();
            var promptRect = promptGo.GetComponent<RectTransform>();
            promptRect.sizeDelta = new Vector2(200, 50);
            promptRect.localScale = Vector3.one * 0.01f;
            var textGo = new GameObject("PromptText");
            textGo.transform.SetParent(promptGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.text = "Press E";
            text.fontSize = 28;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptGo.SetActive(false);

            // Add NPCController and wire fields via reflection
            var npc = root.AddComponent<NPCController>();
            var npcType = npc.GetType();
            SetField(npcType, npc, "npcName", npcName);
            SetField(npcType, npc, "capsuleColor", color);
            SetField(npcType, npc, "dialogueData", dialogue);
            SetField(npcType, npc, "interactionRange", 5f);

            spawnedNPCs.Add(npc);
        }

        private static void SetField(System.Type type, object obj, string fieldName, object value)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        private static DialogueData MakeSampleDialogue(string speaker, params string[] lines)
        {
            var data = ScriptableObject.CreateInstance<DialogueData>();
            var dialogueLines = new DialogueLine[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                dialogueLines[i] = new DialogueLine
                {
                    speakerName = speaker,
                    text = lines[i],
                    duration = 2f,
                    autoAdvance = false,
                    speakerColor = Color.cyan
                };
            }
            data.lines = dialogueLines;
            return data;
        }
    }
}
