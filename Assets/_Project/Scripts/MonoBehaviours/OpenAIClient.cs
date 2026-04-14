using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using FarmSimVR.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Sends chat messages to the OpenAI Responses API with streaming support.
    /// Streams text deltas via SSE and fires onChunk for each direct text delta,
    /// then onComplete with the full accumulated response text when the stream ends.
    /// </summary>
    public class OpenAIClient : MonoBehaviour
    {
        private const string API_URL = "https://api.openai.com/v1/responses";
        private const float REQUEST_TIMEOUT = 60f;
        private const string OpenAiApiKeyEnvironmentVariable = "OPENAI_API_KEY";
        private const string GauntletDirectoryPath = "Development/gauntlet";
        private static readonly string[] PreferredGauntletEnvRelativePaths =
        {
            "collab-board/.env.vercel.production",
            "collab-board/.env",
            "chatbox-fork-pr-target-guard/.env.local",
            "chatbox-fork-pr-target-guard/.env",
            "chatbox/.env.local",
            "chatbox/.env"
        };

        [Header("OpenAI Configuration")]
        [SerializeField] private string apiKey;
        [SerializeField] private string model = "gpt-4o-mini";
        [SerializeField] [Range(0f, 2f)] private float temperature = 0.8f;
        [SerializeField] private int maxTokens = 140;

        [NonSerialized] private string _gauntletEnvSearchRootOverride;
        [NonSerialized] private bool _gauntletApiKeyCached;
        [NonSerialized] private string _cachedGauntletApiKey;

        /// <summary>
        /// Streams a text response from the OpenAI API.
        /// onChunk is called with each new text delta as it arrives.
        /// onComplete is called with the full accumulated response text when done.
        /// onError is called if the request fails.
        /// </summary>
        public IEnumerator ChatStream(
            List<ChatMessage> messages,
            Action<string> onChunk,
            Action<string> onComplete,
            Action<string> onError)
        {
            string resolvedApiKey = ResolveApiKey();
            if (string.IsNullOrWhiteSpace(resolvedApiKey))
            {
                onError?.Invoke(
                    "OpenAI API key is not set. Provide OPENAI_API_KEY in the environment or assign a local override on OpenAIClient.");
                yield break;
            }

            string requestBody = BuildRequestJson(messages, stream: true);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(requestBody);

            using var request = new UnityWebRequest(API_URL, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = (int)REQUEST_TIMEOUT;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "text/event-stream");
            request.SetRequestHeader("Authorization", $"Bearer {resolvedApiKey}");

            var op = request.SendWebRequest();

            var accumulated = new StringBuilder(512);
            var pendingEventBuffer = new StringBuilder(512);
            int bytesRead = 0;
            string streamError = null;
            // Poll the download buffer each frame to extract SSE events incrementally.
            while (!op.isDone)
            {
                string raw = request.downloadHandler?.text;
                if (raw != null && raw.Length > bytesRead)
                {
                    string newData = raw.Substring(bytesRead);
                    bytesRead = raw.Length;
                    ProcessStreamingText(newData, pendingEventBuffer, accumulated, onChunk, ref streamError);
                }

                if (streamError != null)
                    break;

                yield return null;
            }

            // Process any remaining data after the request completes.
            string finalRaw = request.downloadHandler?.text;
            if (finalRaw != null && finalRaw.Length > bytesRead)
            {
                string remaining = finalRaw.Substring(bytesRead);
                ProcessStreamingText(remaining, pendingEventBuffer, accumulated, onChunk, ref streamError);
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorDetail = request.downloadHandler?.text ?? request.error;
                onError?.Invoke($"OpenAI stream failed ({request.responseCode}): {errorDetail}");
                yield break;
            }

            if (streamError != null)
            {
                onError?.Invoke(streamError);
                yield break;
            }

            onComplete?.Invoke(accumulated.ToString());
        }

        /// <summary>
        /// Parses streamed SSE events and appends direct text deltas as they arrive.
        /// </summary>
        private static void ProcessStreamingText(
            string chunk,
            StringBuilder pendingEventBuffer,
            StringBuilder accumulated,
            Action<string> onChunk,
            ref string streamError)
        {
            if (string.IsNullOrEmpty(chunk))
                return;

            pendingEventBuffer.Append(chunk.Replace("\r", string.Empty));

            while (TryPopNextEvent(pendingEventBuffer, out string rawEvent))
            {
                string eventName = null;
                var dataBuilder = new StringBuilder();
                string[] lines = rawEvent.Split('\n');
                foreach (string line in lines)
                {
                    if (line.StartsWith("event:", StringComparison.Ordinal))
                    {
                        eventName = line.Substring("event:".Length).Trim();
                        continue;
                    }

                    if (!line.StartsWith("data:", StringComparison.Ordinal))
                        continue;

                    if (dataBuilder.Length > 0)
                        dataBuilder.Append('\n');

                    dataBuilder.Append(line.Substring("data:".Length).TrimStart());
                }

                if (dataBuilder.Length == 0)
                    continue;

                string payload = dataBuilder.ToString();
                if (payload == "[DONE]")
                    continue;

                string eventType = string.IsNullOrEmpty(eventName)
                    ? ExtractJsonStringValue(payload, "type")
                    : eventName;

                if (eventType == "response.output_text.delta")
                {
                    string delta = ExtractJsonStringValue(payload, "delta");
                    if (string.IsNullOrEmpty(delta))
                        continue;

                    accumulated.Append(delta);
                    onChunk?.Invoke(delta);
                    continue;
                }

                if (eventType == "response.completed")
                    continue;

                if (eventType == "error")
                {
                    streamError = ExtractJsonStringValue(payload, "message")
                        ?? "OpenAI streaming request failed.";
                }
            }
        }

        /// <summary>
        /// Extracts a string property from a JSON object without needing a full JSON dependency.
        /// </summary>
        private static string ExtractJsonStringValue(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
                return null;

            string quotedKey = $"\"{key}\"";
            int keyIndex = json.IndexOf(quotedKey, StringComparison.Ordinal);
            if (keyIndex < 0)
                return null;

            int colonIndex = json.IndexOf(':', keyIndex + quotedKey.Length);
            if (colonIndex < 0)
                return null;

            int valueStart = colonIndex + 1;
            while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
                valueStart++;

            if (valueStart >= json.Length || json[valueStart] != '"')
                return null;

            valueStart++;
            var result = new StringBuilder();

            for (int i = valueStart; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '\\' && i + 1 < json.Length)
                {
                    char next = json[i + 1];
                    switch (next)
                    {
                        case '"':  result.Append('"');  i++; break;
                        case '\\': result.Append('\\'); i++; break;
                        case 'n':  result.Append('\n'); i++; break;
                        case 'r':  result.Append('\r'); i++; break;
                        case 't':  result.Append('\t'); i++; break;
                        default:   result.Append('\\'); result.Append(next); i++; break;
                    }
                }
                else if (c == '"')
                {
                    break;
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.Length > 0 ? result.ToString() : null;
        }

        /// <summary>
        /// Builds the JSON payload for the Responses endpoint.
        /// </summary>
        private string BuildRequestJson(List<ChatMessage> messages, bool stream = false)
        {
            var sb = new StringBuilder(512);
            string instructions = null;
            var inputMessages = new List<ChatMessage>(messages.Count);
            foreach (var message in messages)
            {
                if (string.Equals(message.Role, "system", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(instructions))
                        instructions = message.Content;
                    else
                        instructions = $"{instructions}\n\n{message.Content}";

                    continue;
                }

                inputMessages.Add(message);
            }

            sb.Append("{\"model\":\"").Append(EscapeJson(model)).Append("\",");
            sb.Append("\"text\":{\"format\":{\"type\":\"text\"}},");
            sb.Append("\"temperature\":").Append(temperature.ToString("0.00", CultureInfo.InvariantCulture)).Append(",");
            sb.Append("\"max_output_tokens\":").Append(maxTokens).Append(",");
            sb.Append("\"store\":false,");
            if (stream)
                sb.Append("\"stream\":true,");

            if (!string.IsNullOrWhiteSpace(instructions))
                sb.Append("\"instructions\":\"").Append(EscapeJson(instructions)).Append("\",");

            sb.Append("\"input\":[");
            for (int i = 0; i < inputMessages.Count; i++)
            {
                if (i > 0)
                    sb.Append(",");

                sb.Append("{\"role\":\"").Append(EscapeJson(inputMessages[i].Role)).Append("\",");
                sb.Append("\"content\":\"")
                    .Append(EscapeJson(inputMessages[i].Content))
                    .Append("\"}");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private static bool TryPopNextEvent(StringBuilder pendingEventBuffer, out string rawEvent)
        {
            int boundaryIndex = IndexOfEventBoundary(pendingEventBuffer);
            if (boundaryIndex < 0)
            {
                rawEvent = null;
                return false;
            }

            rawEvent = pendingEventBuffer.ToString(0, boundaryIndex);
            pendingEventBuffer.Remove(0, boundaryIndex + 2);
            return true;
        }

        private static int IndexOfEventBoundary(StringBuilder pendingEventBuffer)
        {
            for (int i = 0; i < pendingEventBuffer.Length - 1; i++)
            {
                if (pendingEventBuffer[i] == '\n' && pendingEventBuffer[i + 1] == '\n')
                    return i;
            }

            return -1;
        }

        private string ResolveApiKey()
        {
            string envValue = Environment.GetEnvironmentVariable(OpenAiApiKeyEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(envValue))
                return envValue.Trim();

            string gauntletValue = ResolveGauntletApiKey();
            if (!string.IsNullOrWhiteSpace(gauntletValue))
                return gauntletValue;

            if (!string.IsNullOrWhiteSpace(apiKey))
                return apiKey.Trim();

            return null;
        }

        private string ResolveGauntletApiKey()
        {
            if (_gauntletApiKeyCached)
                return _cachedGauntletApiKey;

            _gauntletApiKeyCached = true;
            string searchRoot = ResolveGauntletSearchRoot();
            if (string.IsNullOrWhiteSpace(searchRoot) || !Directory.Exists(searchRoot))
                return null;

            try
            {
                for (int i = 0; i < PreferredGauntletEnvRelativePaths.Length; i++)
                {
                    string preferredPath = Path.Combine(searchRoot, PreferredGauntletEnvRelativePaths[i]);
                    if (!File.Exists(preferredPath))
                        continue;

                    if (!TryReadApiKeyFromEnvFile(preferredPath, out string preferredKey))
                        continue;

                    _cachedGauntletApiKey = preferredKey;
                    return preferredKey;
                }

                foreach (string envFile in Directory.EnumerateFiles(searchRoot, ".env*", SearchOption.AllDirectories))
                {
                    if (ShouldSkipEnvFile(envFile))
                        continue;

                    if (!TryReadApiKeyFromEnvFile(envFile, out string key))
                        continue;

                    _cachedGauntletApiKey = key;
                    return key;
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        private string ResolveGauntletSearchRoot()
        {
            if (!string.IsNullOrWhiteSpace(_gauntletEnvSearchRootOverride))
                return _gauntletEnvSearchRootOverride;

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(home))
                return null;

            return Path.Combine(home, GauntletDirectoryPath);
        }

        private static bool TryReadApiKeyFromEnvFile(string path, out string apiKeyValue)
        {
            apiKeyValue = null;
            foreach (string line in File.ReadLines(path))
            {
                if (!line.StartsWith("OPENAI_API_KEY=", StringComparison.Ordinal))
                    continue;

                string rawValue = line.Substring("OPENAI_API_KEY=".Length).Trim();
                apiKeyValue = TrimOptionalQuotes(rawValue);
                return !string.IsNullOrWhiteSpace(apiKeyValue);
            }

            return false;
        }

        private static bool ShouldSkipEnvFile(string path)
        {
            string fileName = Path.GetFileName(path);
            if (fileName.EndsWith(".example", StringComparison.OrdinalIgnoreCase))
                return true;

            if (fileName.EndsWith(".sample", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static string TrimOptionalQuotes(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (value.Length >= 2)
            {
                bool quotedWithDouble = value[0] == '"' && value[^1] == '"';
                bool quotedWithSingle = value[0] == '\'' && value[^1] == '\'';
                if (quotedWithDouble || quotedWithSingle)
                    value = value.Substring(1, value.Length - 2);
            }

            return value.Trim();
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }
    }
}
