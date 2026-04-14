using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Uploads short WAV recordings to the OpenAI Transcriptions API.
    /// </summary>
    public sealed class OpenAITranscriptionClient : MonoBehaviour
    {
        private const string ApiUrl = "https://api.openai.com/v1/audio/transcriptions";
        private const int RequestTimeoutSeconds = 60;

        [Header("OpenAI Transcription Configuration")]
        [SerializeField] private string apiKey;
        [SerializeField] private string model = "gpt-4o-mini-transcribe";

        [NonSerialized] private string _gauntletEnvSearchRootOverride;
        [NonSerialized] private bool _gauntletApiKeyCached;
        [NonSerialized] private string _cachedGauntletApiKey;

        public IEnumerator TranscribeWav(
            byte[] wavBytes,
            string prompt,
            Action<string> onComplete,
            Action<string> onError)
        {
            if (wavBytes == null || wavBytes.Length == 0)
            {
                onError?.Invoke("No recorded audio was available for transcription.");
                yield break;
            }

            string resolvedApiKey = ResolveApiKey();
            if (string.IsNullOrWhiteSpace(resolvedApiKey))
            {
                onError?.Invoke("OpenAI API key is not set for Town voice input.");
                yield break;
            }

            using UnityWebRequest request = BuildRequest(wavBytes, prompt);
            request.timeout = RequestTimeoutSeconds;
            request.SetRequestHeader("Authorization", $"Bearer {resolvedApiKey}");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string detail = request.downloadHandler?.text ?? request.error;
                onError?.Invoke($"OpenAI transcription failed ({request.responseCode}): {detail}");
                yield break;
            }

            onComplete?.Invoke(request.downloadHandler?.text?.Trim());
        }

        private UnityWebRequest BuildRequest(byte[] wavBytes, string prompt)
        {
            var formSections = new List<IMultipartFormSection>
            {
                new MultipartFormDataSection("model", model),
                new MultipartFormDataSection("response_format", "text"),
                new MultipartFormFileSection("file", wavBytes, "town-voice-input.wav", "audio/wav")
            };

            if (!string.IsNullOrWhiteSpace(prompt))
                formSections.Add(new MultipartFormDataSection("prompt", prompt));

            return UnityWebRequest.Post(ApiUrl, formSections);
        }

        private string ResolveApiKey()
        {
            return OpenAIConfigurationResolver.ResolveApiKey(
                apiKey,
                ref _gauntletApiKeyCached,
                ref _cachedGauntletApiKey,
                _gauntletEnvSearchRootOverride);
        }
    }
}
