using System;
using FarmSimVR.Core;
using FarmSimVR.MonoBehaviours;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class TownConversationFlowTests
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
        public void TownDialogueOptionComposer_GarrettCookingReply_UsesCookingFollowUpsWithoutEarlyGoodbye()
        {
            string[] options = TownDialogueOptionComposer.BuildOptions(
                "Old Garrett",
                0,
                "Oh, bless my soul! I do love makin' a good tomato pie, all fresh and warm right outta the oven.",
                new[] { "Continue...", "Goodbye." });

            Assert.That(options, Has.Length.EqualTo(4));
            Assert.That(options, Contains.Item("Can you share the recipe?"));
            Assert.That(options, Contains.Item("What else do you like to make?"));
            Assert.That(options, Has.None.EqualTo("Continue..."));
            Assert.That(Array.Exists(options, option => option.Contains("Goodbye", StringComparison.OrdinalIgnoreCase)), Is.False);
        }

        [Test]
        public void TownDialogueOptionComposer_GarrettHistoryReply_UnlocksGoodbyeOnLaterTurns()
        {
            string[] options = TownDialogueOptionComposer.BuildOptions(
                "Old Garrett",
                2,
                "Oh, sixty-some years now. My granddad built the well right over there with his own two hands.",
                new[] { "Continue...", "Goodbye." });

            Assert.That(options, Has.Length.EqualTo(4));
            Assert.That(options, Has.None.EqualTo("Continue..."));
            Assert.That(Array.Exists(options, option => option.Contains("Goodbye", StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(
                Array.Exists(
                    options,
                    option => option.Contains("story", StringComparison.OrdinalIgnoreCase)
                        || option.Contains("town", StringComparison.OrdinalIgnoreCase)
                        || option.Contains("changed", StringComparison.OrdinalIgnoreCase)),
                Is.True);
        }

        [Test]
        public void TownScene_OpenAIClient_UsesFastTextModelBudget()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Town.unity", OpenSceneMode.Single);
            Assert.That(scene.IsValid(), Is.True);

            var client = UnityEngine.Object.FindFirstObjectByType<OpenAIClient>();
            Assert.That(client, Is.Not.Null);

            var serialized = new SerializedObject(client);
            Assert.That(serialized.FindProperty("model").stringValue, Is.EqualTo("gpt-4o-mini"));
            Assert.That(serialized.FindProperty("maxTokens").intValue, Is.LessThanOrEqualTo(140));
        }
    }
}
