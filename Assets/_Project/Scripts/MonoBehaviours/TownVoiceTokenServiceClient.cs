using System;
using System.Collections;
using FarmSimVR.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace FarmSimVR.MonoBehaviours
{
    internal sealed class TownVoiceTokenRequestResult
    {
        public TownVoiceTokenRequestResult(string baseUrl, string token, string errorMessage)
        {
            BaseUrl = baseUrl;
            Token = token;
            ErrorMessage = errorMessage;
        }

        public string BaseUrl { get; }
        public string Token { get; }
        public string ErrorMessage { get; }
        public bool Success => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(Token);
    }

    internal sealed class TownVoiceTokenAttemptResult
    {
        public TownVoiceTokenAttemptResult(
            TownVoiceTokenRequestResult successResult,
            bool shouldRetry,
            string errorMessage)
        {
            SuccessResult = successResult;
            ShouldRetry = shouldRetry;
            ErrorMessage = errorMessage;
        }

        public TownVoiceTokenRequestResult SuccessResult { get; }
        public bool ShouldRetry { get; }
        public string ErrorMessage { get; }
        public bool Success => SuccessResult?.Success == true;
    }

    internal static class TownVoiceTokenServiceClient
    {
        private const int RequestTimeoutSeconds = 15;
        private const int RetryableAttemptCount = 2;
        private const float RetryDelaySeconds = 0.25f;
        private const string TokenRoute = "/api/v1/elevenlabs/tts-websocket-token";

        public static IEnumerator RequestToken(
            string configuredBaseUrl,
            Action<TownVoiceTokenRequestResult> onComplete)
        {
            string environmentOverride = Environment.GetEnvironmentVariable(
                TownVoiceTokenServiceEndpointResolver.EnvironmentVariableName);
            string lastError = null;

            foreach (string baseUrl in TownVoiceTokenServiceEndpointResolver.BuildCandidateBaseUrls(
                         configuredBaseUrl,
                         environmentOverride))
            {
                for (int attempt = 1; attempt <= RetryableAttemptCount; attempt++)
                {
                    TownVoiceTokenAttemptResult attemptResult = null;
                    yield return RequestTokenAttempt(
                        baseUrl,
                        attempt,
                        result => attemptResult = result);

                    if (attemptResult != null && attemptResult.Success)
                    {
                        onComplete?.Invoke(attemptResult.SuccessResult);
                        yield break;
                    }

                    lastError = attemptResult?.ErrorMessage;
                    if (attempt >= RetryableAttemptCount || attemptResult == null || !attemptResult.ShouldRetry)
                        break;

                    yield return new WaitForSecondsRealtime(RetryDelaySeconds);
                }
            }

            onComplete?.Invoke(
                new TownVoiceTokenRequestResult(
                    null,
                    null,
                    string.IsNullOrWhiteSpace(lastError)
                        ? "Voice streaming is unavailable; continuing with text only."
                        : $"Voice streaming is unavailable; continuing with text only. {lastError}"));
        }

        private static IEnumerator RequestTokenAttempt(
            string baseUrl,
            int attempt,
            Action<TownVoiceTokenAttemptResult> onComplete)
        {
            using UnityWebRequest request = BuildRequest(baseUrl);
            yield return request.SendWebRequest();
            onComplete?.Invoke(BuildAttemptResult(baseUrl, attempt, request));
        }

        private static UnityWebRequest BuildRequest(string baseUrl)
        {
            var request = new UnityWebRequest(baseUrl + TokenRoute, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = RequestTimeoutSeconds;
            return request;
        }

        private static TownVoiceTokenAttemptResult BuildAttemptResult(
            string baseUrl,
            int attempt,
            UnityWebRequest request)
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                TokenResponse payload = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
                if (payload != null && !string.IsNullOrWhiteSpace(payload.token))
                {
                    return new TownVoiceTokenAttemptResult(
                        new TownVoiceTokenRequestResult(baseUrl, payload.token, null),
                        shouldRetry: false,
                        errorMessage: null);
                }
            }

            return new TownVoiceTokenAttemptResult(
                successResult: null,
                shouldRetry: ShouldRetry(request),
                errorMessage: BuildErrorMessage(baseUrl, attempt, ResolveAttemptDetail(request)));
        }

        private static bool ShouldRetry(UnityWebRequest request)
        {
            if (request == null)
                return false;

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.DataProcessingError)
                return true;

            long responseCode = request.responseCode;
            return responseCode == 0 || responseCode == 502 || responseCode == 503 || responseCode == 504;
        }

        private static string ResolveAttemptDetail(UnityWebRequest request)
        {
            if (request == null)
                return "unknown error";

            if (request.result == UnityWebRequest.Result.Success)
                return "Voice token response was empty.";

            return request.downloadHandler?.text ?? request.error ?? request.result.ToString();
        }

        private static string BuildErrorMessage(string baseUrl, int attempt, string detail)
        {
            string suffix = string.IsNullOrWhiteSpace(detail) ? "unknown error" : detail.Trim();
            return $"{baseUrl} attempt {attempt} failed: {suffix}";
        }

        [Serializable]
        private sealed class TokenResponse
        {
            public string token;
        }
    }
}
