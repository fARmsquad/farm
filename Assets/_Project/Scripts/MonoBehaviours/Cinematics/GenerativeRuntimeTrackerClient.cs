using System;
using System.Collections;
using FarmSimVR.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    internal sealed class GenerativeRuntimeTrackerSessionsPayload
    {
        public GenerativeRuntimeTrackerSessionsPayload(
            string baseUrl,
            GenerativeRuntimeTrackerSessionSummary[] sessions,
            string errorMessage = "")
        {
            BaseUrl = baseUrl ?? string.Empty;
            Sessions = sessions ?? Array.Empty<GenerativeRuntimeTrackerSessionSummary>();
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public string BaseUrl { get; }
        public GenerativeRuntimeTrackerSessionSummary[] Sessions { get; }
        public string ErrorMessage { get; }
        public bool Success => !string.IsNullOrWhiteSpace(BaseUrl) && string.IsNullOrWhiteSpace(ErrorMessage);
    }

    internal sealed class GenerativeRuntimeTrackerSessionDetailPayload
    {
        public GenerativeRuntimeTrackerSessionDetailPayload(
            string baseUrl,
            GenerativeRuntimeTrackerSessionDetail detail,
            string errorMessage = "")
        {
            BaseUrl = baseUrl ?? string.Empty;
            Detail = detail;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public string BaseUrl { get; }
        public GenerativeRuntimeTrackerSessionDetail Detail { get; }
        public string ErrorMessage { get; }
        public bool Success => !string.IsNullOrWhiteSpace(BaseUrl) && Detail != null && string.IsNullOrWhiteSpace(ErrorMessage);
    }

    internal static class GenerativeRuntimeTrackerClient
    {
        private const int RequestTimeoutSeconds = 60;
        private const string TrackerRoute = "/api/runtime/v1/tracker";

        public static IEnumerator ListSessions(
            string configuredBaseUrl,
            int limit,
            Action<GenerativeRuntimeTrackerSessionsPayload> onComplete)
        {
            string environmentOverride = Environment.GetEnvironmentVariable(TownVoiceTokenServiceEndpointResolver.EnvironmentVariableName);
            string lastError = null;

            foreach (string baseUrl in TownVoiceTokenServiceEndpointResolver.BuildCandidateBaseUrls(configuredBaseUrl, environmentOverride))
            {
                using var request = BuildGetRequest($"{baseUrl}{TrackerRoute}/sessions?limit={Mathf.Clamp(limit, 1, 50)}");
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    lastError = ReadErrorMessage(request);
                    continue;
                }

                var sessions = ParseSessionArray(request.downloadHandler?.text);
                onComplete?.Invoke(new GenerativeRuntimeTrackerSessionsPayload(baseUrl, sessions));
                yield break;
            }

            onComplete?.Invoke(new GenerativeRuntimeTrackerSessionsPayload(string.Empty, Array.Empty<GenerativeRuntimeTrackerSessionSummary>(), lastError ?? "Runtime tracker session list failed."));
        }

        public static IEnumerator GetSessionDetail(
            string baseUrl,
            string sessionId,
            Action<GenerativeRuntimeTrackerSessionDetailPayload> onComplete)
        {
            using var request = BuildGetRequest($"{baseUrl}{TrackerRoute}/sessions/{sessionId}");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(new GenerativeRuntimeTrackerSessionDetailPayload(baseUrl, null, ReadErrorMessage(request)));
                yield break;
            }

            var detail = JsonUtility.FromJson<GenerativeRuntimeTrackerSessionDetail>(request.downloadHandler?.text);
            onComplete?.Invoke(
                detail == null
                    ? new GenerativeRuntimeTrackerSessionDetailPayload(baseUrl, null, "Runtime tracker detail response was invalid.")
                    : new GenerativeRuntimeTrackerSessionDetailPayload(baseUrl, detail));
        }

        private static UnityWebRequest BuildGetRequest(string url)
        {
            var request = UnityWebRequest.Get(url);
            request.timeout = RequestTimeoutSeconds;
            return request;
        }

        private static GenerativeRuntimeTrackerSessionSummary[] ParseSessionArray(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Array.Empty<GenerativeRuntimeTrackerSessionSummary>();

            var wrapped = "{\"items\":" + json + "}";
            var container = JsonUtility.FromJson<SessionArrayWrapper>(wrapped);
            return container?.items ?? Array.Empty<GenerativeRuntimeTrackerSessionSummary>();
        }

        private static string ReadErrorMessage(UnityWebRequest request)
        {
            string body = request.downloadHandler?.text;
            if (!string.IsNullOrWhiteSpace(body))
                return body;

            return string.IsNullOrWhiteSpace(request.error)
                ? "Runtime tracker request failed."
                : request.error;
        }

        [Serializable]
        private sealed class SessionArrayWrapper
        {
            public GenerativeRuntimeTrackerSessionSummary[] items = Array.Empty<GenerativeRuntimeTrackerSessionSummary>();
        }
    }
}
