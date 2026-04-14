using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
        [SerializeField] private bool enableVoiceInput = true;

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
        public event Action<string>         OnPlayerPromptSubmitted;
        public event Action<string>         OnExitBlocked;

        public bool IsInConversation { get; private set; }

        private List<ChatMessage> _history = new();
        private string _activeNpc;
        private readonly TownConversationMemoryStore _conversationMemory = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (!enableVoiceInput || GetComponent<TownVoiceInputController>() != null)
                return;

            gameObject.AddComponent<TownVoiceInputController>();
        }

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

            _history.Add(new ChatMessage("user", TownKnowledgeGraph.BuildOpeningPrompt(npcName)));
            StartCoroutine(StreamAndUpdate());
        }

        // ── Option selection ──────────────────────────────────────────────────

        /// <summary>Called by DialogueChoiceUI when the player picks a reply.</summary>
        public void SelectOption(string option)
        {
            SubmitPlayerPrompt(option);
        }

        public bool SubmitPlayerPrompt(string prompt)
        {
            if (!IsInConversation)
                return false;

            string trimmedPrompt = prompt?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedPrompt))
                return false;

            Debug.Log($"{LOG_PREFIX} [Player] {trimmedPrompt}");

            var exitDecision = TownConversationExitGate.Evaluate(trimmedPrompt, CountAssistantMessages(_history));
            if (!string.IsNullOrWhiteSpace(exitDecision.BlockedMessage))
            {
                OnExitBlocked?.Invoke(exitDecision.BlockedMessage);
                return false;
            }

            if (exitDecision.ShouldEndConversation)
            {
                EndConversation();
                return true;
            }

            _conversationMemory.RecordPlayerPrompt(_activeNpc, trimmedPrompt);
            _history.Add(new ChatMessage("user", trimmedPrompt));
            OnPlayerPromptSubmitted?.Invoke(trimmedPrompt);
            StartCoroutine(StreamAndUpdate());
            return true;
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
            TownConversationContextWindow requestContext = _conversationMemory.BuildContextWindow(_activeNpc);

            yield return openAIClient.ChatStream(
                BuildRequestMessages(requestContext),
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

            // Parse structured output when present; otherwise fall back to the raw streamed text.
            var parsed = TryParseResponse(fullText);
            if (parsed == null)
            {
                Debug.LogError($"{LOG_PREFIX} Unexpected response format: {fullText}");
                OnError?.Invoke("Unexpected response format.");
                EndConversation();
                yield break;
            }

            int turnIndex = CountAssistantMessages(_history);
            _conversationMemory.RecordNpcResponse(_activeNpc, parsed.response);
            _history.Add(new ChatMessage("assistant", parsed.response));
            TownConversationContextWindow responseContext = _conversationMemory.BuildContextWindow(_activeNpc);
            string[] displayOptions = TownDialogueOptionComposer.BuildOptions(
                _activeNpc,
                turnIndex,
                parsed.response,
                parsed.options,
                _history,
                responseContext);

            Debug.Log($"{LOG_PREFIX} [{_activeNpc}] {parsed.response}");
            if (displayOptions != null)
            {
                for (int i = 0; i < displayOptions.Length; i++)
                    Debug.Log($"{LOG_PREFIX}   [{i + 1}] {displayOptions[i]}");
            }

            OnNPCResponse?.Invoke(_activeNpc, parsed.response);
            OnOptionsReady?.Invoke(displayOptions);
        }

        private List<ChatMessage> BuildRequestMessages(TownConversationContextWindow contextWindow)
        {
            var requestMessages = new List<ChatMessage>(_history.Count + 1);
            for (int i = 0; i < _history.Count; i++)
            {
                requestMessages.Add(_history[i]);
                if (i != 0 || !string.Equals(_history[i].Role, "system", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (contextWindow == null || string.IsNullOrWhiteSpace(contextWindow.AdditionalInstructions))
                    continue;

                requestMessages.Add(new ChatMessage("system", contextWindow.AdditionalInstructions));
            }

            return requestMessages;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static LLMResponse TryParseResponse(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            // Preserve compatibility with any older structured-output prompts.
            string cleaned = text.Trim();
            if (cleaned.StartsWith("```"))
            {
                int firstNewline = cleaned.IndexOf('\n');
                if (firstNewline >= 0) cleaned = cleaned.Substring(firstNewline + 1);
                if (cleaned.EndsWith("```"))
                    cleaned = cleaned.Substring(0, cleaned.Length - 3).Trim();
            }

            if (!cleaned.StartsWith("{", StringComparison.Ordinal))
                return BuildFallbackResponse(text);

            if (TryParseLegacyJsonResponse(cleaned, out var legacyParsed))
                return legacyParsed;

            try
            {
                var parsed = JsonUtility.FromJson<LLMResponse>(cleaned);
                if (parsed != null && !string.IsNullOrEmpty(parsed.response))
                    return new LLMResponse
                    {
                        response = parsed.response,
                        options = NormalizeOptions(parsed.options)
                    };
            }
            catch (Exception)
            {
                // Fall through
            }

            return BuildFallbackResponse(text);
        }

        private static bool TryParseLegacyJsonResponse(string json, out LLMResponse parsed)
        {
            parsed = null;
            if (!TryExtractJsonString(json, "response", out string responseText) || string.IsNullOrWhiteSpace(responseText))
                return false;

            string[] options = TryExtractJsonStringArray(json, "options", out string[] parsedOptions)
                ? NormalizeOptions(parsedOptions)
                : CreateFallbackOptions();

            parsed = new LLMResponse
            {
                response = responseText,
                options = options
            };
            return true;
        }

        private static bool TryExtractJsonString(string json, string key, out string value)
        {
            value = null;
            if (!TryFindJsonValueIndex(json, key, out int valueIndex) || valueIndex >= json.Length || json[valueIndex] != '"')
                return false;

            return TryReadJsonString(json, valueIndex, out value, out _);
        }

        private static bool TryExtractJsonStringArray(string json, string key, out string[] values)
        {
            values = null;
            if (!TryFindJsonValueIndex(json, key, out int valueIndex) || valueIndex >= json.Length || json[valueIndex] != '[')
                return false;

            var results = new List<string>();
            int cursor = valueIndex + 1;
            while (cursor < json.Length)
            {
                cursor = SkipWhitespace(json, cursor);
                if (cursor >= json.Length) return false;
                if (json[cursor] == ']')
                {
                    values = results.ToArray();
                    return true;
                }
                if (json[cursor] == ',')
                {
                    cursor++;
                    continue;
                }
                if (!TryReadJsonString(json, cursor, out string item, out cursor)) return false;
                results.Add(item);
            }

            return false;
        }

        private static bool TryFindJsonValueIndex(string json, string key, out int valueIndex)
        {
            valueIndex = -1;
            int keyIndex = json.IndexOf($"\"{key}\"", StringComparison.Ordinal);
            if (keyIndex < 0) return false;

            int colonIndex = json.IndexOf(':', keyIndex);
            if (colonIndex < 0) return false;

            valueIndex = SkipWhitespace(json, colonIndex + 1);
            return valueIndex < json.Length;
        }

        private static bool TryReadJsonString(string json, int openingQuoteIndex, out string value, out int nextIndex)
        {
            value = null;
            nextIndex = openingQuoteIndex;
            if (openingQuoteIndex >= json.Length || json[openingQuoteIndex] != '"') return false;

            var builder = new StringBuilder();
            for (int i = openingQuoteIndex + 1; i < json.Length; i++)
            {
                char character = json[i];
                if (character == '"')
                {
                    value = builder.ToString();
                    nextIndex = i + 1;
                    return true;
                }
                if (character != '\\')
                {
                    builder.Append(character);
                    continue;
                }
                if (!TryAppendEscapedCharacter(json, ref i, builder)) return false;
            }

            return false;
        }

        private static bool TryAppendEscapedCharacter(string json, ref int index, StringBuilder builder)
        {
            int escapedIndex = index + 1;
            if (escapedIndex >= json.Length) return false;

            char escaped = json[escapedIndex];
            switch (escaped)
            {
                case '"': builder.Append('"'); index = escapedIndex; return true;
                case '\\': builder.Append('\\'); index = escapedIndex; return true;
                case '/': builder.Append('/'); index = escapedIndex; return true;
                case 'b': builder.Append('\b'); index = escapedIndex; return true;
                case 'f': builder.Append('\f'); index = escapedIndex; return true;
                case 'n': builder.Append('\n'); index = escapedIndex; return true;
                case 'r': builder.Append('\r'); index = escapedIndex; return true;
                case 't': builder.Append('\t'); index = escapedIndex; return true;
                case 'u': return TryAppendUnicodeEscape(json, ref index, builder);
                default: return false;
            }
        }

        private static bool TryAppendUnicodeEscape(string json, ref int index, StringBuilder builder)
        {
            int hexStart = index + 2;
            if (hexStart + 4 > json.Length) return false;

            string hex = json.Substring(hexStart, 4);
            if (!ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort codePoint))
                return false;

            builder.Append((char)codePoint);
            index = hexStart + 3;
            return true;
        }

        private static int SkipWhitespace(string text, int startIndex)
        {
            while (startIndex < text.Length && char.IsWhiteSpace(text[startIndex]))
                startIndex++;

            return startIndex;
        }

        private static LLMResponse BuildFallbackResponse(string text)
        {
            return new LLMResponse
            {
                response = text,
                options = CreateFallbackOptions()
            };
        }

        private static string[] NormalizeOptions(string[] options)
        {
            return options != null && options.Length > 0
                ? options
                : CreateFallbackOptions();
        }

        private static int CountAssistantMessages(List<ChatMessage> history)
        {
            int assistantMessages = 0;
            for (int i = 0; i < history.Count; i++)
            {
                if (string.Equals(history[i].Role, "assistant", StringComparison.OrdinalIgnoreCase))
                    assistantMessages++;
            }
            return assistantMessages;
        }

        private static string[] CreateFallbackOptions() => new[] { "Continue...", "Goodbye." };

        [Serializable]
        private class LLMResponse
        {
            public string   response;
            public string[] options;
        }
    }
}
