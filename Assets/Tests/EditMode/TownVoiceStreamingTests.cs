using System;
using FarmSimVR.Core;
using FarmSimVR.MonoBehaviours;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Reflection;
using Object = UnityEngine.Object;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class TownVoiceStreamingTests
    {
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void TownNpcVoiceProfileCatalog_MapsTownNpcsToStableProfiles()
        {
            Assert.That(TownNpcVoiceProfileCatalog.GetProfile("Old Garrett").VoiceId, Is.EqualTo("pqHfZKP75CvOlQylNhV4"));
            Assert.That(TownNpcVoiceProfileCatalog.GetProfile("Old Garrett").ModelId, Is.EqualTo("eleven_flash_v2_5"));

            Assert.That(TownNpcVoiceProfileCatalog.GetProfile("Mira the Baker").VoiceId, Is.EqualTo("hpp4J3VqNfWAUOO0d1Us"));
            Assert.That(TownNpcVoiceProfileCatalog.GetProfile("Young Pip").VoiceId, Is.EqualTo("TX3LPaxmHKxFdv7VOQHJ"));
        }

        [Test]
        public void TownVoiceTextChunker_EmitsClauseBoundaryAndFlushesTrailingText()
        {
            var chunker = new TownVoiceTextChunker();

            Assert.That(chunker.Append("Well now, ain't seen you around "), Is.Empty);

            var completed = chunker.Append("these parts before!");
            Assert.That(completed, Is.EqualTo(new[] { "Well now, ain't seen you around these parts before!" }));

            Assert.That(chunker.Append("What brings you to town"), Is.Empty);
            Assert.That(chunker.Flush(), Is.EqualTo("What brings you to town"));
        }

        [Test]
        public void TownVoiceTextChunker_DoesNotEmitTinyFragmentsOnShortCommaPause()
        {
            var chunker = new TownVoiceTextChunker();

            Assert.That(chunker.Append("Well now, ain't seen you"), Is.Empty);
        }

        [Test]
        public void TownVoiceTextChunker_EmitsLongClauseAtCommaBeforeSentenceEnd()
        {
            var chunker = new TownVoiceTextChunker();

            var completed = chunker.Append("The market's lively today, and Miss Edna says the pies are nearly ready");

            Assert.That(completed, Is.EqualTo(new[] { "The market's lively today," }));
            Assert.That(chunker.Flush(), Is.EqualTo("and Miss Edna says the pies are nearly ready"));
        }

        [Test]
        public void TownVoiceTokenServiceEndpointResolver_PrefersEnvironmentOverrideAndDedupesTrailingSlash()
        {
            var candidates = TownVoiceTokenServiceEndpointResolver.BuildCandidateBaseUrls(
                "http://127.0.0.1:8000/",
                "http://127.0.0.1:8011/");

            Assert.That(
                candidates,
                Is.EqualTo(new[]
                {
                    "http://127.0.0.1:8011",
                    "http://127.0.0.1:8000"
                }));
        }

        [Test]
        public void TownVoiceTokenServiceEndpointResolver_AddsKnownLocalFallbackPort()
        {
            var candidates = TownVoiceTokenServiceEndpointResolver.BuildCandidateBaseUrls(
                "http://127.0.0.1:8000",
                null);

            Assert.That(candidates, Does.Contain("http://127.0.0.1:8011"));
        }

        [Test]
        public void TownVoiceTokenServiceClient_TimeoutBudget_IsLongerThanLegacyThreeSecondLimit()
        {
            Type clientType = typeof(TownNpcVoiceStreamController).Assembly
                .GetType("FarmSimVR.MonoBehaviours.TownVoiceTokenServiceClient");

            Assert.That(clientType, Is.Not.Null);

            FieldInfo timeoutField = clientType.GetField(
                "RequestTimeoutSeconds",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(timeoutField, Is.Not.Null);
            Assert.That((int)timeoutField.GetRawConstantValue(), Is.GreaterThan(3));
        }

        [Test]
        public void TownVoiceTokenServiceClient_RetryBudget_AllowsMoreThanOneAttempt()
        {
            Type clientType = typeof(TownNpcVoiceStreamController).Assembly
                .GetType("FarmSimVR.MonoBehaviours.TownVoiceTokenServiceClient");

            Assert.That(clientType, Is.Not.Null);

            FieldInfo retryField = clientType.GetField(
                "RetryableAttemptCount",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(retryField, Is.Not.Null);
            Assert.That((int)retryField.GetRawConstantValue(), Is.GreaterThan(1));
        }

        [Test]
        public void TownScene_ContainsVoiceStreamingController()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Town.unity", OpenSceneMode.Single);
            Assert.That(scene.IsValid(), Is.True);

            Assert.That(Object.FindFirstObjectByType<TownNpcVoiceStreamController>(), Is.Not.Null);
        }
    }
}
