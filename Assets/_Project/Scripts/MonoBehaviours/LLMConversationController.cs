using System;
using System.Collections;
using System.Collections.Generic;
using FarmSimVR.Core;
using FarmSimVR.MonoBehaviours.Cinematics;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Manages LLM-powered NPC conversations.
    /// Subscribes to every NPCController.OnInteracted in the scene,
    /// maintains per-NPC conversation history, and fires events for the UI.
    /// All conversation turns are logged to the Unity console.
    /// </summary>
    public class LLMConversationController : MonoBehaviour
    {
        private const string LOG_PREFIX = "[Chat]";

        [SerializeField] private OpenAIClient openAIClient;

        public event Action<string, string> OnNPCResponse;    // (npcName, responseText)
        public event Action<string[]>       OnOptionsReady;
        public event Action                 OnWaiting;
        public event Action                 OnConversationEnded;
        public event Action<string>         OnError;

        public bool IsInConversation { get; private set; }

        private readonly Dictionary<string, List<ChatMessage>> _histories = new();
        private string _activeNpc;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            foreach (var npc in FindObjectsByType<NPCController>(FindObjectsSortMode.None))
                npc.OnInteracted += HandleNPCInteracted;
        }

        private void OnDestroy()
        {
            foreach (var npc in FindObjectsByType<NPCController>(FindObjectsSortMode.None))
                npc.OnInteracted -= HandleNPCInteracted;
        }

        // ── Interaction entry point ───────────────────────────────────────────

        private void HandleNPCInteracted(NPCController npc)
        {
            if (IsInConversation) return;
            BeginConversation(npc.NpcName);
        }

        private void BeginConversation(string npcName)
        {
            IsInConversation = true;
            _activeNpc       = npcName;

            Debug.Log($"{LOG_PREFIX} === Conversation started with {npcName} ===");

            if (!_histories.ContainsKey(npcName))
            {
                _histories[npcName] = new List<ChatMessage>
                {
                    new("system", NPCPersonaCatalog.GetSystemPrompt(npcName))
                };
            }

            _histories[npcName].Add(new ChatMessage("user", "[START_CONVERSATION]"));
            StartCoroutine(SendAndUpdate());
        }

        // ── Option selection ──────────────────────────────────────────────────

        /// <summary>Called by DialogueChoiceUI when the player picks a reply bubble.</summary>
        public void SelectOption(string option)
        {
            if (!IsInConversation) return;

            Debug.Log($"{LOG_PREFIX} [Player] {option}");

            if (IsGoodbye(option))
            {
                EndConversation();
                return;
            }

            _histories[_activeNpc].Add(new ChatMessage("user", option));
            StartCoroutine(SendAndUpdate());
        }

        /// <summary>
        /// Ends the current conversation and notifies listeners.
        /// </summary>
        public void EndConversation()
        {
            Debug.Log($"{LOG_PREFIX} === Conversation ended with {_activeNpc} ===");
            IsInConversation = false;
            _activeNpc       = null;
            OnConversationEnded?.Invoke();
        }

        // ── LLM call ──────────────────────────────────────────────────────────

        private IEnumerator SendAndUpdate()
        {
            OnWaiting?.Invoke();
            Debug.Log($"{LOG_PREFIX} Waiting for {_activeNpc}'s response...");

            string result = null;
            string error  = null;

            yield return openAIClient.Chat(
                _histories[_activeNpc],
                r => result = r,
                e => error  = e);

            if (error != null)
            {
                Debug.LogError($"{LOG_PREFIX} Error: {error}");
                OnError?.Invoke(error);
                EndConversation();
                yield break;
            }

            var parsed = JsonUtility.FromJson<LLMResponse>(result);
            if (parsed == null || string.IsNullOrEmpty(parsed.response))
            {
                Debug.LogError($"{LOG_PREFIX} Unexpected response format: {result}");
                OnError?.Invoke("Unexpected response format.");
                EndConversation();
                yield break;
            }

            _histories[_activeNpc].Add(new ChatMessage("assistant", result));

            // Log the NPC's response and options to the console
            Debug.Log($"{LOG_PREFIX} [{_activeNpc}] {parsed.response}");
            if (parsed.options != null && parsed.options.Length > 0)
            {
                for (int i = 0; i < parsed.options.Length; i++)
                    Debug.Log($"{LOG_PREFIX}   [{i + 1}] {parsed.options[i]}");
            }

            OnNPCResponse?.Invoke(_activeNpc, parsed.response);
            OnOptionsReady?.Invoke(parsed.options ?? new[] { "Goodbye." });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool IsGoodbye(string option)
        {
            if (string.IsNullOrEmpty(option)) return false;
            string lower = option.ToLowerInvariant();
            return lower.Contains("goodbye") || lower.Contains("farewell")
                || lower.Contains("see you") || lower.Contains("take care");
        }

        [Serializable]
        private class LLMResponse
        {
            public string   response;
            public string[] options;
        }
    }
}
