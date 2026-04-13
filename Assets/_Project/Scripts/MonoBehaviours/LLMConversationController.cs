using System;
using System.Collections;
using System.Collections.Generic;
using FarmSimVR.Core;
using FarmSimVR.MonoBehaviours.Cinematics;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Manages LLM-powered NPC conversations with streaming support.
    /// Each conversation starts with a fresh history (system prompt only).
    /// Subscribes to every NPCController.OnInteracted in the scene.
    /// </summary>
    public class LLMConversationController : MonoBehaviour
    {
        private const string LOG_PREFIX = "[Chat]";

        [SerializeField] private OpenAIClient openAIClient;

        /// <summary>Fired once when the NPC name is known and streaming begins.</summary>
        public event Action<string> OnStreamStarted;       // (npcName)

        /// <summary>Fired for each token chunk as it streams in.</summary>
        public event Action<string> OnStreamChunk;          // (tokenChunk)

        /// <summary>Fired when streaming is complete with the final parsed response and options.</summary>
        public event Action<string, string> OnNPCResponse;  // (npcName, responseText)
        public event Action<string[]>       OnOptionsReady;
        public event Action                 OnWaiting;
        public event Action                 OnConversationEnded;
        public event Action<string>         OnError;

        public bool IsInConversation { get; private set; }

        private List<ChatMessage> _history = new();
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

            // Fresh history every time — no cross-NPC bleed
            _history = new List<ChatMessage>
            {
                new("system", NPCPersonaCatalog.GetSystemPrompt(npcName))
            };

            _history.Add(new ChatMessage("user", "[START_CONVERSATION]"));
            StartCoroutine(StreamAndUpdate());
        }

        // ── Option selection ──────────────────────────────────────────────────

        /// <summary>Called by DialogueChoiceUI when the player picks a reply.</summary>
        public void SelectOption(string option)
        {
            if (!IsInConversation) return;

            Debug.Log($"{LOG_PREFIX} [Player] {option}");

            if (IsGoodbye(option))
            {
                EndConversation();
                return;
            }

            _history.Add(new ChatMessage("user", option));
            StartCoroutine(StreamAndUpdate());
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

        // ── Streaming LLM call ────────────────────────────────────────────────

        private IEnumerator StreamAndUpdate()
        {
            OnWaiting?.Invoke();
            Debug.Log($"{LOG_PREFIX} Waiting for {_activeNpc}'s response...");

            bool streamStartedFired = false;
            string fullText = null;
            string error    = null;

            yield return openAIClient.ChatStream(
                _history,
                onChunk: token =>
                {
                    if (!streamStartedFired)
                    {
                        streamStartedFired = true;
                        OnStreamStarted?.Invoke(_activeNpc);
                    }
                    OnStreamChunk?.Invoke(token);
                },
                onComplete: text => fullText = text,
                onError:    err  => error    = err
            );

            if (error != null)
            {
                Debug.LogError($"{LOG_PREFIX} Error: {error}");
                OnError?.Invoke(error);
                EndConversation();
                yield break;
            }

            // Parse the completed JSON to extract response text and options
            var parsed = TryParseResponse(fullText);
            if (parsed == null)
            {
                Debug.LogError($"{LOG_PREFIX} Unexpected response format: {fullText}");
                OnError?.Invoke("Unexpected response format.");
                EndConversation();
                yield break;
            }

            _history.Add(new ChatMessage("assistant", fullText));

            Debug.Log($"{LOG_PREFIX} [{_activeNpc}] {parsed.response}");
            if (parsed.options != null)
            {
                for (int i = 0; i < parsed.options.Length; i++)
                    Debug.Log($"{LOG_PREFIX}   [{i + 1}] {parsed.options[i]}");
            }

            OnNPCResponse?.Invoke(_activeNpc, parsed.response);
            OnOptionsReady?.Invoke(parsed.options ?? new[] { "Goodbye." });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static LLMResponse TryParseResponse(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            // The model may wrap the JSON in markdown code fences — strip them
            string cleaned = text.Trim();
            if (cleaned.StartsWith("```"))
            {
                int firstNewline = cleaned.IndexOf('\n');
                if (firstNewline >= 0) cleaned = cleaned.Substring(firstNewline + 1);
                if (cleaned.EndsWith("```"))
                    cleaned = cleaned.Substring(0, cleaned.Length - 3).Trim();
            }

            try
            {
                var parsed = JsonUtility.FromJson<LLMResponse>(cleaned);
                if (parsed != null && !string.IsNullOrEmpty(parsed.response))
                    return parsed;
            }
            catch (Exception)
            {
                // Fall through
            }

            // Fallback: treat the entire text as the response with no options
            return new LLMResponse { response = text, options = new[] { "Continue...", "Goodbye." } };
        }

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
