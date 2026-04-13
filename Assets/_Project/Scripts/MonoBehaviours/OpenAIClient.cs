using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using FarmSimVR.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Sends chat messages to the OpenAI Chat Completions API with streaming support.
    /// Streams token-by-token via SSE and fires onChunk for each delta,
    /// then onComplete with the full accumulated text when the stream ends.
    /// </summary>
    public class OpenAIClient : MonoBehaviour
    {
        private const string API_URL = "https://api.openai.com/v1/chat/completions";
        private const float REQUEST_TIMEOUT = 60f;
        private const string SSE_DATA_PREFIX = "data: ";
        private const string SSE_DONE_MARKER = "[DONE]";

        [Header("OpenAI Configuration")]
        [SerializeField] private string apiKey;
        [SerializeField] private string model = "gpt-4o-mini";
        [SerializeField] [Range(0f, 2f)] private float temperature = 0.8f;
        [SerializeField] private int maxTokens = 300;

        /// <summary>
        /// Streams a chat completion from the OpenAI API.
        /// onChunk is called with each new token as it arrives.
        /// onComplete is called with the full accumulated response text when done.
        /// onError is called if the request fails.
        /// </summary>
        public IEnumerator ChatStream(
            List<ChatMessage> messages,
            Action<string> onChunk,
            Action<string> onComplete,
            Action<string> onError)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                onError?.Invoke("OpenAI API key is not set. Assign it on the OpenAIClient component.");
                yield break;
            }

            string requestBody = BuildRequestJson(messages, stream: true);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(requestBody);

            using var request = new UnityWebRequest(API_URL, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = (int)REQUEST_TIMEOUT;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            var op = request.SendWebRequest();

            var accumulated = new StringBuilder(512);
            int bytesRead = 0;

            // Poll the download buffer each frame to extract SSE chunks
            while (!op.isDone)
            {
                string raw = request.downloadHandler?.text;
                if (raw != null && raw.Length > bytesRead)
                {
                    string newData = raw.Substring(bytesRead);
                    bytesRead = raw.Length;
                    ProcessSSEChunk(newData, accumulated, onChunk);
                }
                yield return null;
            }

            // Process any remaining data after request completes
            string finalRaw = request.downloadHandler?.text;
            if (finalRaw != null && finalRaw.Length > bytesRead)
            {
                string remaining = finalRaw.Substring(bytesRead);
                ProcessSSEChunk(remaining, accumulated, onChunk);
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorDetail = request.downloadHandler?.text ?? request.error;
                onError?.Invoke($"OpenAI stream failed ({request.responseCode}): {errorDetail}");
                yield break;
            }

            onComplete?.Invoke(accumulated.ToString());
        }

        /// <summary>
        /// Parses SSE lines from a raw chunk and extracts delta content tokens.
        /// </summary>
        private static void ProcessSSEChunk(string chunk, StringBuilder accumulated, Action<string> onChunk)
        {
            string[] lines = chunk.Split('\n');
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (!trimmed.StartsWith(SSE_DATA_PREFIX)) continue;

                string payload = trimmed.Substring(SSE_DATA_PREFIX.Length).Trim();
                if (payload == SSE_DONE_MARKER) continue;

                string token = ExtractDeltaContent(payload);
                if (string.IsNullOrEmpty(token)) continue;

                accumulated.Append(token);
                onChunk?.Invoke(token);
            }
        }

        /// <summary>
        /// Extracts the content field from a streaming delta JSON chunk.
        /// Format: {"choices":[{"delta":{"content":"token"}}]}
        /// </summary>
        private static string ExtractDeltaContent(string json)
        {
            const string key = "\"content\":\"";
            int idx = json.IndexOf(key, StringComparison.Ordinal);
            if (idx < 0) return null;

            int valueStart = idx + key.Length;
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
        /// Builds the JSON payload for the Chat Completions endpoint.
        /// </summary>
        private string BuildRequestJson(List<ChatMessage> messages, bool stream = false)
        {
            var sb = new StringBuilder(512);
            sb.Append("{\"model\":\"").Append(EscapeJson(model)).Append("\",");
            sb.Append("\"temperature\":").Append(temperature.ToString("F2")).Append(",");
            sb.Append("\"max_tokens\":").Append(maxTokens).Append(",");
            if (stream) sb.Append("\"stream\":true,");
            sb.Append("\"messages\":[");

            for (int i = 0; i < messages.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append("{\"role\":\"").Append(EscapeJson(messages[i].Role)).Append("\",");
                sb.Append("\"content\":\"").Append(EscapeJson(messages[i].Content)).Append("\"}");
            }

            sb.Append("]}");
            return sb.ToString();
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
