using System;
using System.Collections;
using System.Text;
using FarmSimVR.Core;
using FarmSimVR.Core.Story;
using UnityEngine;
using UnityEngine.Networking;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    public sealed class StorySequenceAdvancePayload
    {
        public StorySequenceAdvancePayload(
            string baseUrl,
            string sessionId,
            string entrySceneName,
            StoryPackageSnapshot runtimePackage,
            string errorMessage = "")
        {
            BaseUrl = baseUrl ?? string.Empty;
            SessionId = sessionId ?? string.Empty;
            EntrySceneName = entrySceneName ?? string.Empty;
            RuntimePackage = runtimePackage;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public string BaseUrl { get; }
        public string SessionId { get; }
        public string EntrySceneName { get; }
        public StoryPackageSnapshot RuntimePackage { get; }
        public string ErrorMessage { get; }

        public bool Success =>
            !string.IsNullOrWhiteSpace(BaseUrl) &&
            !string.IsNullOrWhiteSpace(SessionId) &&
            !string.IsNullOrWhiteSpace(EntrySceneName) &&
            RuntimePackage != null &&
            string.IsNullOrWhiteSpace(ErrorMessage);
    }

    internal static class StorySequenceServiceClient
    {
        private const int RequestTimeoutSeconds = 240;
        private const string SessionRoute = "/api/v1/story-sequence-sessions";

        public static IEnumerator CreateSessionAndAdvance(
            string configuredBaseUrl,
            Action<StorySequenceAdvancePayload> onComplete)
        {
            string environmentOverride = Environment.GetEnvironmentVariable(
                TownVoiceTokenServiceEndpointResolver.EnvironmentVariableName);
            string lastError = null;

            foreach (string baseUrl in TownVoiceTokenServiceEndpointResolver.BuildCandidateBaseUrls(
                         configuredBaseUrl,
                         environmentOverride))
            {
                using var createRequest = BuildJsonPostRequest(baseUrl + SessionRoute, "{}");
                yield return createRequest.SendWebRequest();

                if (createRequest.result != UnityWebRequest.Result.Success)
                {
                    lastError = ReadErrorMessage(createRequest);
                    continue;
                }

                if (!TryParseCreatedSession(createRequest.downloadHandler?.text, out var sessionId))
                {
                    lastError = "Story sequence session create response was invalid.";
                    continue;
                }

                StorySequenceAdvancePayload payload = null;
                yield return AdvanceSessionAtBaseUrl(
                    baseUrl,
                    sessionId,
                    result => payload = result);

                if (payload != null && payload.Success)
                {
                    onComplete?.Invoke(payload);
                    yield break;
                }

                lastError = payload?.ErrorMessage ?? "Story sequence advance response was empty.";
            }

            onComplete?.Invoke(
                new StorySequenceAdvancePayload(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    null,
                    string.IsNullOrWhiteSpace(lastError)
                        ? "Generated story sequence is unavailable right now."
                        : lastError));
        }

        public static IEnumerator AdvanceSession(
            string configuredBaseUrl,
            string sessionId,
            Action<StorySequenceAdvancePayload> onComplete)
        {
            string environmentOverride = Environment.GetEnvironmentVariable(
                TownVoiceTokenServiceEndpointResolver.EnvironmentVariableName);
            string lastError = null;

            foreach (string baseUrl in TownVoiceTokenServiceEndpointResolver.BuildCandidateBaseUrls(
                         configuredBaseUrl,
                         environmentOverride))
            {
                StorySequenceAdvancePayload payload = null;
                yield return AdvanceSessionAtBaseUrl(baseUrl, sessionId, result => payload = result);

                if (payload != null && payload.Success)
                {
                    onComplete?.Invoke(payload);
                    yield break;
                }

                lastError = payload?.ErrorMessage ?? "Story sequence advance response was empty.";
            }

            onComplete?.Invoke(
                new StorySequenceAdvancePayload(
                    string.Empty,
                    sessionId,
                    string.Empty,
                    null,
                    string.IsNullOrWhiteSpace(lastError)
                        ? "Generated story sequence continuation is unavailable right now."
                        : lastError));
        }

        private static IEnumerator AdvanceSessionAtBaseUrl(
            string baseUrl,
            string sessionId,
            Action<StorySequenceAdvancePayload> onComplete)
        {
            using var request = BuildJsonPostRequest(
                $"{baseUrl}{SessionRoute}/{sessionId}/next-turn",
                "{}");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(
                    new StorySequenceAdvancePayload(
                        string.Empty,
                        sessionId,
                        string.Empty,
                        null,
                        ReadErrorMessage(request)));
                yield break;
            }

            if (!TryParseAdvanceResponse(baseUrl, sessionId, request.downloadHandler?.text, out var payload))
            {
                payload = new StorySequenceAdvancePayload(
                    string.Empty,
                    sessionId,
                    string.Empty,
                    null,
                    "Story sequence advance response was invalid.");
            }

            onComplete?.Invoke(payload);
        }

        private static UnityWebRequest BuildJsonPostRequest(string url, string jsonBody)
        {
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody ?? "{}"));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = RequestTimeoutSeconds;
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }

        private static bool TryParseCreatedSession(string responseText, out string sessionId)
        {
            sessionId = string.Empty;
            if (string.IsNullOrWhiteSpace(responseText))
                return false;

            var payload = JsonUtility.FromJson<CreateSessionResponse>(responseText);
            if (payload == null || string.IsNullOrWhiteSpace(payload.session_id))
                return false;

            sessionId = payload.session_id.Trim();
            return true;
        }

        private static bool TryParseAdvanceResponse(
            string baseUrl,
            string fallbackSessionId,
            string responseText,
            out StorySequenceAdvancePayload payload)
        {
            payload = null;
            if (string.IsNullOrWhiteSpace(responseText))
                return false;

            var response = JsonUtility.FromJson<AdvanceSessionResponse>(responseText);
            if (response?.turn?.request?.cutscene == null || response.turn.result == null)
                return false;

            string errorMessage = string.Empty;
            if (!response.turn.result.is_valid)
                errorMessage = BuildErrorMessage(response.turn.result.errors);

            payload = new StorySequenceAdvancePayload(
                baseUrl,
                string.IsNullOrWhiteSpace(response.session?.session_id)
                    ? fallbackSessionId
                    : response.session.session_id,
                response.turn.request.cutscene.scene_name,
                response.turn.result.unity_package,
                errorMessage);
            return true;
        }

        private static string BuildErrorMessage(string[] errors)
        {
            if (errors == null || errors.Length == 0)
                return "Generated story sequence request failed.";

            return string.Join(" ", errors);
        }

        private static string ReadErrorMessage(UnityWebRequest request)
        {
            string body = request.downloadHandler?.text;
            if (!string.IsNullOrWhiteSpace(body))
                return body;

            return string.IsNullOrWhiteSpace(request.error)
                ? "Generated story sequence request failed."
                : request.error;
        }

        [Serializable]
        private sealed class CreateSessionResponse
        {
            public string session_id;
        }

        [Serializable]
        private sealed class AdvanceSessionResponse
        {
            public SessionPayload session;
            public TurnPayload turn;
        }

        [Serializable]
        private sealed class SessionPayload
        {
            public string session_id;
        }

        [Serializable]
        private sealed class TurnPayload
        {
            public RequestPayload request;
            public ResultPayload result;
        }

        [Serializable]
        private sealed class RequestPayload
        {
            public CutscenePayload cutscene;
        }

        [Serializable]
        private sealed class CutscenePayload
        {
            public string scene_name;
        }

        [Serializable]
        private sealed class ResultPayload
        {
            public bool is_valid;
            public StoryPackageSnapshot unity_package;
            public string[] errors;
        }
    }
}
