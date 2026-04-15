using System;
using System.Text;
using FarmSimVR.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class GenerativeRuntimeRailwaySmokeTests
    {
        private const string LiveSmokeEnvVar = "FARMSIM_ENABLE_LIVE_RAILWAY_SMOKE";
        private const string RuntimeRoute = "/api/runtime/v1";

        [UnityTest]
        [Timeout(420000)]
        public System.Collections.IEnumerator RailwayRuntime_CreateSession_ToReadyTurn_AndFetchArtifact()
        {
            if (!string.Equals(Environment.GetEnvironmentVariable(LiveSmokeEnvVar), "1", StringComparison.Ordinal))
                Assert.Ignore($"Set {LiveSmokeEnvVar}=1 to enable the live Railway smoke test.");

            var baseUrl = TownVoiceTokenServiceEndpointResolver.ProductionBaseUrl;
            Assert.That(baseUrl, Does.StartWith("https://"));

            using (var createRequest = BuildJsonPostRequest($"{baseUrl}{RuntimeRoute}/sessions", "{}"))
            {
                yield return createRequest.SendWebRequest();
                Assert.That(createRequest.result, Is.EqualTo(UnityWebRequest.Result.Success), ReadError(createRequest));

                var createResponse = JsonUtility.FromJson<GenerativeRuntimeSessionCreateResponse>(
                    createRequest.downloadHandler?.text ?? string.Empty);
                Assert.That(createResponse, Is.Not.Null);
                Assert.That(createResponse.session_id, Is.Not.Empty);
                Assert.That(createResponse.job_id, Is.Not.Empty);

                GenerativeRuntimeJobSnapshot job = null;
                var deadline = EditorApplication.timeSinceStartup + 330d;

                while (EditorApplication.timeSinceStartup < deadline)
                {
                    using var jobRequest = UnityWebRequest.Get($"{baseUrl}{RuntimeRoute}/jobs/{createResponse.job_id}");
                    jobRequest.timeout = 60;
                    yield return jobRequest.SendWebRequest();

                    Assert.That(jobRequest.result, Is.EqualTo(UnityWebRequest.Result.Success), ReadError(jobRequest));
                    job = JsonUtility.FromJson<GenerativeRuntimeJobSnapshot>(jobRequest.downloadHandler?.text ?? string.Empty);
                    Assert.That(job, Is.Not.Null);
                    Assert.That(job.session_id, Is.EqualTo(createResponse.session_id));

                    if (string.Equals(job.status, "ready", StringComparison.Ordinal))
                        break;

                    if (string.Equals(job.status, "failed", StringComparison.Ordinal) ||
                        string.Equals(job.status, "cancelled", StringComparison.Ordinal))
                    {
                        Assert.Fail($"Railway runtime job ended in '{job.status}': {job.error_message}");
                    }

                    yield return null;
                }

                Assert.That(job, Is.Not.Null);
                Assert.That(job.status, Is.EqualTo("ready"), "Timed out waiting for Railway runtime job readiness.");
                Assert.That(job.turn_id, Is.Not.Empty);

                using var turnRequest = UnityWebRequest.Get(
                    $"{baseUrl}{RuntimeRoute}/sessions/{createResponse.session_id}/turns/{job.turn_id}");
                turnRequest.timeout = 60;
                yield return turnRequest.SendWebRequest();

                Assert.That(turnRequest.result, Is.EqualTo(UnityWebRequest.Result.Success), ReadError(turnRequest));
                var envelope = JsonUtility.FromJson<GenerativePlayableTurnEnvelope>(turnRequest.downloadHandler?.text ?? string.Empty);
                Assert.That(envelope, Is.Not.Null);
                Assert.That(envelope.contract_version, Is.EqualTo("runtime/v1"));
                Assert.That(envelope.session_id, Is.EqualTo(createResponse.session_id));
                Assert.That(envelope.turn_id, Is.EqualTo(job.turn_id));
                Assert.That(envelope.cutscene, Is.Not.Null);
                Assert.That(envelope.minigame, Is.Not.Null);
                Assert.That(envelope.cutscene.shots, Is.Not.Null);
                Assert.That(envelope.cutscene.shots.Length, Is.GreaterThan(0));
                Assert.That(envelope.artifacts, Is.Not.Null);
                Assert.That(envelope.artifacts.Length, Is.GreaterThan(0));

                var firstImage = Array.Find(envelope.artifacts, artifact => artifact != null && artifact.artifact_type == "image");
                Assert.That(firstImage, Is.Not.Null);
                Assert.That(firstImage.asset_id, Is.Not.Empty);

                using var artifactRequest = UnityWebRequest.Get($"{baseUrl}{RuntimeRoute}/artifacts/{firstImage.asset_id}/content");
                artifactRequest.timeout = 60;
                yield return artifactRequest.SendWebRequest();

                Assert.That(artifactRequest.result, Is.EqualTo(UnityWebRequest.Result.Success), ReadError(artifactRequest));
                Assert.That(artifactRequest.downloadHandler?.data, Is.Not.Null);
                Assert.That(artifactRequest.downloadHandler.data.Length, Is.GreaterThan(128));
            }
        }

        private static UnityWebRequest BuildJsonPostRequest(string url, string jsonBody)
        {
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody ?? "{}"));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 330;
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }

        private static string ReadError(UnityWebRequest request)
        {
            var body = request.downloadHandler?.text;
            if (!string.IsNullOrWhiteSpace(body))
                return body;

            return request.error ?? "Unknown UnityWebRequest error.";
        }
    }
}
