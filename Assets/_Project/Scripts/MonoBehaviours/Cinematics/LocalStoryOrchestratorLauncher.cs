using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using FarmSimVR.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    public sealed class LocalStoryOrchestratorReadyResult
    {
        public LocalStoryOrchestratorReadyResult(
            string baseUrl,
            bool success,
            bool launched,
            string errorMessage)
        {
            BaseUrl = baseUrl ?? string.Empty;
            Success = success;
            Launched = launched;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public string BaseUrl { get; }
        public bool Success { get; }
        public bool Launched { get; }
        public string ErrorMessage { get; }
    }

    internal static class LocalStoryOrchestratorLauncher
    {
        private const string HealthRoute = "/health";
        private const int HealthRequestTimeoutSeconds = 2;
        private const int LaunchPollAttemptCount = 20;
        private const float LaunchPollDelaySeconds = 0.5f;
        private const string LauncherScriptRelativePath = "backend/story-orchestrator/start_local_backend.sh";

        public static IEnumerator EnsureReady(
            string configuredBaseUrl,
            Action<LocalStoryOrchestratorReadyResult> onComplete)
        {
            string environmentOverride = Environment.GetEnvironmentVariable(
                TownVoiceTokenServiceEndpointResolver.EnvironmentVariableName);
            var candidates = TownVoiceTokenServiceEndpointResolver.BuildCandidateBaseUrls(
                configuredBaseUrl,
                environmentOverride);

            for (int i = 0; i < candidates.Count; i++)
            {
                var candidateBaseUrl = candidates[i];
                bool isHealthy = false;
                yield return CheckHealth(candidateBaseUrl, healthy => isHealthy = healthy);
                if (!isHealthy)
                    continue;

                onComplete?.Invoke(new LocalStoryOrchestratorReadyResult(candidateBaseUrl, true, false, string.Empty));
                yield break;
            }

            string launchBaseUrl = candidates.Count > 0
                ? candidates[0]
                : string.Empty;
            if (!TryLaunchLocalBackend(launchBaseUrl, out var logPath, out var launchError))
            {
                GeneratedStorySliceDiagnostics.LogWarning(
                    nameof(LocalStoryOrchestratorLauncher),
                    $"Local story-orchestrator launch failed for '{launchBaseUrl}': {launchError}");
                onComplete?.Invoke(new LocalStoryOrchestratorReadyResult(
                    launchBaseUrl,
                    false,
                    false,
                    string.IsNullOrWhiteSpace(logPath)
                        ? launchError
                        : $"{launchError} Check {logPath}."));
                yield break;
            }

            GeneratedStorySliceDiagnostics.Log(
                nameof(LocalStoryOrchestratorLauncher),
                $"Launching local story-orchestrator for '{launchBaseUrl}'.");

            for (int attempt = 0; attempt < LaunchPollAttemptCount; attempt++)
            {
                bool isHealthy = false;
                yield return CheckHealth(launchBaseUrl, healthy => isHealthy = healthy);
                if (isHealthy)
                {
                    GeneratedStorySliceDiagnostics.Log(
                        nameof(LocalStoryOrchestratorLauncher),
                        $"Local story-orchestrator is healthy at '{launchBaseUrl}'.");
                    onComplete?.Invoke(new LocalStoryOrchestratorReadyResult(
                        launchBaseUrl,
                        true,
                        true,
                        string.Empty));
                    yield break;
                }

                yield return new WaitForSecondsRealtime(LaunchPollDelaySeconds);
            }

            onComplete?.Invoke(new LocalStoryOrchestratorReadyResult(
                launchBaseUrl,
                false,
                true,
                $"Local story-orchestrator did not become healthy at '{launchBaseUrl}'. Check {logPath}."));
            GeneratedStorySliceDiagnostics.LogWarning(
                nameof(LocalStoryOrchestratorLauncher),
                $"Local story-orchestrator did not become healthy at '{launchBaseUrl}'. Check {logPath}.");
        }

        private static IEnumerator CheckHealth(string baseUrl, Action<bool> onComplete)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                onComplete?.Invoke(false);
                yield break;
            }

            using var request = UnityWebRequest.Get(baseUrl + HealthRoute);
            request.timeout = HealthRequestTimeoutSeconds;
            yield return request.SendWebRequest();

            onComplete?.Invoke(
                request.result == UnityWebRequest.Result.Success &&
                request.responseCode >= 200 &&
                request.responseCode < 300);
        }

        private static bool TryLaunchLocalBackend(
            string baseUrl,
            out string logPath,
            out string errorMessage)
        {
            logPath = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                errorMessage = "No local story-orchestrator base URL is configured.";
                return false;
            }

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            {
                errorMessage = $"Invalid story-orchestrator base URL '{baseUrl}'.";
                return false;
            }

            string projectRoot = ResolveProjectRootPath();
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                errorMessage = "Could not resolve the project root for local backend bootstrap.";
                return false;
            }

            string launcherScriptPath = Path.Combine(projectRoot, LauncherScriptRelativePath);
            if (!File.Exists(launcherScriptPath))
            {
                errorMessage = $"Missing local backend launcher script at '{launcherScriptPath}'.";
                return false;
            }

            const string bashPath = "/bin/bash";
            if (!File.Exists(bashPath))
            {
                errorMessage = $"Missing shell at '{bashPath}'.";
                return false;
            }

            logPath = $"/tmp/story-orchestrator-{uri.Port}.log";
            var startInfo = new ProcessStartInfo
            {
                FileName = bashPath,
                Arguments = $"{QuoteArgument(launcherScriptPath)} {QuoteArgument(uri.Host)} {QuoteArgument(uri.Port.ToString())} {QuoteArgument(logPath)}",
                WorkingDirectory = Path.GetDirectoryName(launcherScriptPath),
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            {
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    errorMessage = "Local backend launcher did not start.";
                    return false;
                }
            }
            catch (Exception error)
            {
                errorMessage = $"Failed to launch the local backend: {error.Message}";
                return false;
            }

            return true;
        }

        private static string ResolveProjectRootPath()
        {
            if (string.IsNullOrWhiteSpace(Application.dataPath))
                return string.Empty;

            return Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
        }

        private static string QuoteArgument(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }
    }
}
