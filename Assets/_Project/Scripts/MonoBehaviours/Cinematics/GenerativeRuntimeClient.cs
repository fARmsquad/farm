using System;
using System.Collections;
using System.Text;
using FarmSimVR.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    internal sealed class GenerativeRuntimeCreateSessionPayload
    {
        public GenerativeRuntimeCreateSessionPayload(
            string baseUrl,
            GenerativeRuntimeSessionCreateResponse response,
            string errorMessage = "")
        {
            BaseUrl = baseUrl ?? string.Empty;
            Response = response;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public string BaseUrl { get; }
        public GenerativeRuntimeSessionCreateResponse Response { get; }
        public string ErrorMessage { get; }
        public bool Success =>
            !string.IsNullOrWhiteSpace(BaseUrl) &&
            Response != null &&
            !string.IsNullOrWhiteSpace(Response.session_id) &&
            !string.IsNullOrWhiteSpace(Response.job_id) &&
            string.IsNullOrWhiteSpace(ErrorMessage);
    }

    internal sealed class GenerativeRuntimeSessionPayload
    {
        public GenerativeRuntimeSessionPayload(
            string baseUrl,
            GenerativeRuntimeSessionDetail detail,
            string errorMessage = "")
        {
            BaseUrl = baseUrl ?? string.Empty;
            Detail = detail;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public string BaseUrl { get; }
        public GenerativeRuntimeSessionDetail Detail { get; }
        public string ErrorMessage { get; }
        public bool Success => !string.IsNullOrWhiteSpace(BaseUrl) && Detail != null && string.IsNullOrWhiteSpace(ErrorMessage);
    }

    internal sealed class GenerativeRuntimeJobPayload
    {
        public GenerativeRuntimeJobPayload(
            string baseUrl,
            GenerativeRuntimeJobSnapshot job,
            string errorMessage = "")
        {
            BaseUrl = baseUrl ?? string.Empty;
            Job = job;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public string BaseUrl { get; }
        public GenerativeRuntimeJobSnapshot Job { get; }
        public string ErrorMessage { get; }
        public bool Success => !string.IsNullOrWhiteSpace(BaseUrl) && Job != null && string.IsNullOrWhiteSpace(ErrorMessage);
    }

    internal sealed class GenerativeRuntimeTurnPayload
    {
        public GenerativeRuntimeTurnPayload(
            string baseUrl,
            GenerativePlayableTurnEnvelope envelope,
            string errorMessage = "")
        {
            BaseUrl = baseUrl ?? string.Empty;
            Envelope = envelope;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public string BaseUrl { get; }
        public GenerativePlayableTurnEnvelope Envelope { get; }
        public string ErrorMessage { get; }
        public bool Success => !string.IsNullOrWhiteSpace(BaseUrl) && Envelope != null && string.IsNullOrWhiteSpace(ErrorMessage);
    }

    internal sealed class GenerativeRuntimeOutcomePayload
    {
        public GenerativeRuntimeOutcomePayload(
            string baseUrl,
            GenerativeOutcomeResponse response,
            string errorMessage = "")
        {
            BaseUrl = baseUrl ?? string.Empty;
            Response = response;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public string BaseUrl { get; }
        public GenerativeOutcomeResponse Response { get; }
        public string ErrorMessage { get; }
        public bool Success => !string.IsNullOrWhiteSpace(BaseUrl) && Response != null && string.IsNullOrWhiteSpace(ErrorMessage);
    }

    internal static class GenerativeRuntimeClient
    {
        private const int RequestTimeoutSeconds = 240;
        private const string RuntimeRoute = "/api/runtime/v1";

        public static IEnumerator CreateSession(
            string configuredBaseUrl,
            Action<GenerativeRuntimeCreateSessionPayload> onComplete)
        {
            string environmentOverride = Environment.GetEnvironmentVariable(TownVoiceTokenServiceEndpointResolver.EnvironmentVariableName);
            string lastError = null;

            foreach (string baseUrl in TownVoiceTokenServiceEndpointResolver.BuildCandidateBaseUrls(configuredBaseUrl, environmentOverride))
            {
                using var request = BuildJsonPostRequest($"{baseUrl}{RuntimeRoute}/sessions", "{}");
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    lastError = ReadErrorMessage(request);
                    continue;
                }

                var response = JsonUtility.FromJson<GenerativeRuntimeSessionCreateResponse>(request.downloadHandler?.text);
                if (response == null || string.IsNullOrWhiteSpace(response.session_id) || string.IsNullOrWhiteSpace(response.job_id))
                {
                    lastError = "Runtime session create response was invalid.";
                    continue;
                }

                onComplete?.Invoke(new GenerativeRuntimeCreateSessionPayload(baseUrl, response));
                yield break;
            }

            onComplete?.Invoke(new GenerativeRuntimeCreateSessionPayload(string.Empty, null, lastError ?? "Runtime session create failed."));
        }

        public static IEnumerator GetSession(
            string baseUrl,
            string sessionId,
            Action<GenerativeRuntimeSessionPayload> onComplete)
        {
            using var request = BuildGetRequest($"{baseUrl}{RuntimeRoute}/sessions/{sessionId}");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(new GenerativeRuntimeSessionPayload(baseUrl, null, ReadErrorMessage(request)));
                yield break;
            }

            var response = JsonUtility.FromJson<GenerativeRuntimeSessionDetail>(request.downloadHandler?.text);
            onComplete?.Invoke(
                response == null
                    ? new GenerativeRuntimeSessionPayload(baseUrl, null, "Runtime session response was invalid.")
                    : new GenerativeRuntimeSessionPayload(baseUrl, response));
        }

        public static IEnumerator GetJob(
            string baseUrl,
            string jobId,
            Action<GenerativeRuntimeJobPayload> onComplete)
        {
            using var request = BuildGetRequest($"{baseUrl}{RuntimeRoute}/jobs/{jobId}");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(new GenerativeRuntimeJobPayload(baseUrl, null, ReadErrorMessage(request)));
                yield break;
            }

            var response = JsonUtility.FromJson<GenerativeRuntimeJobSnapshot>(request.downloadHandler?.text);
            onComplete?.Invoke(
                response == null
                    ? new GenerativeRuntimeJobPayload(baseUrl, null, "Runtime job response was invalid.")
                    : new GenerativeRuntimeJobPayload(baseUrl, response));
        }

        public static IEnumerator GetTurn(
            string baseUrl,
            string sessionId,
            string turnId,
            Action<GenerativeRuntimeTurnPayload> onComplete)
        {
            using var request = BuildGetRequest($"{baseUrl}{RuntimeRoute}/sessions/{sessionId}/turns/{turnId}");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(new GenerativeRuntimeTurnPayload(baseUrl, null, ReadErrorMessage(request)));
                yield break;
            }

            var response = JsonUtility.FromJson<GenerativePlayableTurnEnvelope>(request.downloadHandler?.text);
            onComplete?.Invoke(
                response == null
                    ? new GenerativeRuntimeTurnPayload(baseUrl, null, "Runtime turn response was invalid.")
                    : new GenerativeRuntimeTurnPayload(baseUrl, response));
        }

        public static IEnumerator SubmitOutcome(
            string baseUrl,
            string sessionId,
            string turnId,
            bool success,
            Action<GenerativeRuntimeOutcomePayload> onComplete)
        {
            string requestBody = success
                ? "{\"result\":\"success\",\"score\":1.0,\"completed_objective_count\":1,\"notes\":\"Unity runtime completed the generated objective.\"}"
                : "{\"result\":\"failure\",\"score\":0.0,\"completed_objective_count\":0,\"notes\":\"Unity runtime reported a generated objective failure.\"}";
            using var request = BuildJsonPostRequest($"{baseUrl}{RuntimeRoute}/sessions/{sessionId}/turns/{turnId}/outcome", requestBody);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(new GenerativeRuntimeOutcomePayload(baseUrl, null, ReadErrorMessage(request)));
                yield break;
            }

            var response = JsonUtility.FromJson<GenerativeOutcomeResponse>(request.downloadHandler?.text);
            onComplete?.Invoke(
                response == null
                    ? new GenerativeRuntimeOutcomePayload(baseUrl, null, "Runtime outcome response was invalid.")
                    : new GenerativeRuntimeOutcomePayload(baseUrl, response));
        }

        public static string BuildArtifactContentUrl(string baseUrl, string assetId)
        {
            return $"{baseUrl}{RuntimeRoute}/artifacts/{assetId}/content";
        }

        private static UnityWebRequest BuildGetRequest(string url)
        {
            var request = UnityWebRequest.Get(url);
            request.timeout = RequestTimeoutSeconds;
            return request;
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

        private static string ReadErrorMessage(UnityWebRequest request)
        {
            string body = request.downloadHandler?.text;
            if (!string.IsNullOrWhiteSpace(body))
                return body;

            return string.IsNullOrWhiteSpace(request.error)
                ? "Generated runtime request failed."
                : request.error;
        }
    }
}
