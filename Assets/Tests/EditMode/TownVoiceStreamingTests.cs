using FarmSimVR.Core;
using FarmSimVR.MonoBehaviours;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

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
        public void TownScene_ContainsVoiceStreamingController()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Town.unity", OpenSceneMode.Single);
            Assert.That(scene.IsValid(), Is.True);

            Assert.That(Object.FindFirstObjectByType<TownNpcVoiceStreamController>(), Is.Not.Null);
        }
    }
}
