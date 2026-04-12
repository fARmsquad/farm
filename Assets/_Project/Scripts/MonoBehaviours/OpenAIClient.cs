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
    /// Sends chat messages to the OpenAI Chat Completions API and returns the
    /// assistant's reply. Set the API key and model in the Inspector.
    /// </summary>
    public class OpenAIClient : MonoBehaviour
    {
        private const string API_URL = "https://api.openai.com/v1/chat/completions";
        private const float REQUEST_TIMEOUT = 30f;

        [Header("OpenAI Configuration")]
        [SerializeField] private string apiKey;
        [SerializeField] private string model = "gpt-4o-mini";
        [SerializeField] [Range(0f, 2f)] private float temperature = 0.8f;
        [SerializeField] private int maxTokens = 300;

        /// <summary>
        /// Sends the conversation history to the OpenAI API.
        /// Invokes onSuccess with the raw content string, or onError on failure.
        /// </summary>
        public IEnumerator Chat(
            List<ChatMessage> messages,
            Action<string> onSuccess,
            Action<string> onError)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                onError?.Invoke("OpenAI API key is not set. Assign it on the OpenAIClient component.");
                yield break;
            }

            string requestBody = BuildRequestJson(messages);

            using var request = new UnityWebRequest(API_URL, UnityWebRequest.kHttpVerbPOST);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(requestBody);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = (int)REQUEST_TIMEOUT;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorDetail = request.downloadHandler?.text ?? request.error;
                onError?.Invoke($"OpenAI request failed ({request.responseCode}): {errorDetail}");
                yield break;
            }

            string responseBody = request.downloadHandler.text;
            string content = ExtractContent(responseBody);

            if (content == null)
            {
                onError?.Invoke($"Failed to parse OpenAI response: {responseBody}");
                yield break;
            }

            onSuccess?.Invoke(content);
        }

        /// <summary>
        /// Builds the JSON payload for the Chat Completions endpoint.
        /// </summary>
        private string BuildRequestJson(List<ChatMessage> messages)
        {
            var sb = new StringBuilder(512);
            sb.Append("{\"model\":\"").Append(EscapeJson(model)).Append("\",");
            sb.Append("\"temperature\":").Append(temperature.ToString("F2")).Append(",");
            sb.Append("\"max_tokens\":").Append(maxTokens).Append(",");
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

        /// <summary>
        /// Extracts the assistant message content from the API response JSON.
        /// </summary>
        private static string ExtractContent(string json)
        {
            var response = JsonUtility.FromJson<OpenAIResponse>(json);
            if (response?.choices == null || response.choices.Length == 0) return null;
            return response.choices[0]?.message?.content;
        }

        [Serializable]
        private class OpenAIResponse
        {
            public Choice[] choices;
        }

        [Serializable]
        private class Choice
        {
            public Message message;
        }

        [Serializable]
        private class Message
        {
            public string content;
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
