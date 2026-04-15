using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using FarmSimVR.Core.Mailbox;
using UnityEngine;
using UnityEngine.Networking;

namespace FarmSimVR.MonoBehaviours.Mailbox
{
    /// <summary>
    /// Calls the OpenAI chat completions API to generate a batch of mail messages.
    /// Returns structured JSON parsed into MailMessage objects.
    /// </summary>
    public class MailLLMClient : MonoBehaviour
    {
        private const string API_URL        = "https://api.openai.com/v1/chat/completions";
        private const float  REQUEST_TIMEOUT = 30f;

        [Header("OpenAI")]
        [SerializeField] private string apiKey;
        [SerializeField] private string model = "gpt-4o-mini";

        [NonSerialized] private bool   _gauntletApiKeyCached;
        [NonSerialized] private string _cachedGauntletApiKey;

        private const string SYSTEM_PROMPT =
            "You generate mail for a cozy farming village game. " +
            "All content must be child-safe, warm, friendly, and wholesome. " +
            "No conflict, no scary content, no adult themes. " +
            "Use a slightly old-fashioned letter style. " +
            "Gentle mystery is fine (a kind stranger, an unknown admirer of the farm) but never unsettling. " +
            "Junk mail should be cheerful, absurd in-world advertisements. " +
            "Real mail should feel personal and warm. " +
            "IMPORTANT: Use plain ASCII text only — no emoji, no special Unicode symbols, no bullet points. " +
            "Format the body as a proper letter: a greeting line, 1-3 short paragraphs, then a sign-off. " +
            "Separate each paragraph with a blank line (two newlines).\n\n" +
            "Respond ONLY with a JSON object:\n" +
            "{\"messages\":[{\"sender\":\"...\",\"subject\":\"...\",\"body\":\"...\",\"type\":\"real|junk\",\"attachmentItemId\":null,\"attachmentQuantity\":0}]}";

        public IEnumerator GenerateMail(
            int            dayNumber,
            List<string>   knownNpcNames,
            Action<List<MailMessage>> onComplete,
            Action<string> onError)
        {
            string key = OpenAIConfigurationResolver.ResolveApiKey(
                apiKey, ref _gauntletApiKeyCached, ref _cachedGauntletApiKey, null);

            if (string.IsNullOrWhiteSpace(key))
            {
                onError?.Invoke("No API key — set OPENAI_API_KEY or assign it on MailLLMClient.");
                yield break;
            }

            int junkCount = UnityEngine.Random.Range(1, 4);
            int realCount = UnityEngine.Random.Range(1, 3);
            string npcList = knownNpcNames.Count > 0 ? string.Join(", ", knownNpcNames) : "no one yet";

            string userPrompt =
                $"Day {dayNumber + 1} of the farm. " +
                $"The player has met: {npcList}. " +
                $"Generate {junkCount} junk mail item(s) and {realCount} real mail item(s). " +
                $"Uniqueness token: {Guid.NewGuid()}.";

            string body = BuildRequestJson(userPrompt);

            using var req = new UnityWebRequest(API_URL, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout         = (int)REQUEST_TIMEOUT;
            req.SetRequestHeader("Content-Type",  "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {key}");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Mail generation request failed: {req.error}");
                yield break;
            }

            var messages = ParseResponse(req.downloadHandler.text);
            if (messages == null)
                onError?.Invoke("Failed to parse mail response JSON.");
            else
                onComplete?.Invoke(messages);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string BuildRequestJson(string userPrompt)
        {
            string sys  = EscapeJson(SYSTEM_PROMPT);
            string user = EscapeJson(userPrompt);
            return
                $"{{\"model\":\"{model}\"," +
                $"\"response_format\":{{\"type\":\"json_object\"}}," +
                $"\"temperature\":1.1," +
                $"\"messages\":[" +
                $"{{\"role\":\"system\",\"content\":\"{sys}\"}}," +
                $"{{\"role\":\"user\",\"content\":\"{user}\"}}" +
                $"]}}";
        }

        private static string EscapeJson(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");

        // Strip emoji and non-ASCII symbols so TMP doesn't render them as boxes
        private static string StripEmoji(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder(s.Length);
            int i = 0;
            while (i < s.Length)
            {
                // Surrogate pairs = emoji above U+FFFF — skip both chars
                if (char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
                {
                    i += 2;
                    continue;
                }
                char c = s[i];
                // Skip common symbol/emoji ranges in BMP
                if ((c >= '\u2600' && c <= '\u27FF') ||   // Misc symbols, dingbats
                    (c >= '\uFE00' && c <= '\uFEFF') ||   // Variation selectors, BOM
                    c == '\u200D' || c == '\uFFFD')        // ZWJ, replacement char
                {
                    i++;
                    continue;
                }
                sb.Append(c);
                i++;
            }
            return sb.ToString().Trim();
        }

        private static List<MailMessage> ParseResponse(string rawJson)
        {
            try
            {
                var completion = JsonUtility.FromJson<CompletionResponse>(rawJson);
                if (completion?.choices == null || completion.choices.Length == 0) return null;

                string content = completion.choices[0].message.content;
                var batch = JsonUtility.FromJson<MailBatch>(content);
                if (batch?.messages == null) return null;

                var result = new List<MailMessage>(batch.messages.Length);
                foreach (var dto in batch.messages)
                {
                    MailAttachment attachment = null;
                    if (!string.IsNullOrEmpty(dto.attachmentItemId) && dto.attachmentQuantity > 0)
                        attachment = new MailAttachment(dto.attachmentItemId, dto.attachmentQuantity);

                    result.Add(new MailMessage(
                        StripEmoji(dto.sender), StripEmoji(dto.subject), StripEmoji(dto.body),
                        dto.type == "junk" ? MailType.Junk : MailType.Real,
                        attachment));
                }
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MailLLMClient] Parse error: {e.Message}");
                return null;
            }
        }

        // ── JSON DTOs ─────────────────────────────────────────────────────────

        [Serializable] private class CompletionResponse { public CompletionChoice[] choices; }
        [Serializable] private class CompletionChoice   { public CompletionMessage message; }
        [Serializable] private class CompletionMessage  { public string content; }

        [Serializable] private class MailBatch { public MailDto[] messages; }
        [Serializable] private class MailDto
        {
            public string sender;
            public string subject;
            public string body;
            public string type;
            public string attachmentItemId;
            public int    attachmentQuantity;
        }
    }
}
