using System.Reflection;
using FarmSimVR.MonoBehaviours;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class DialogueChoiceUITests
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
        public void StreamChunkDisplaysDirectDeltaText()
        {
            var ui = CreateUi(out var dialogue);

            InvokePrivateInstance(ui, "HandleStreamStarted", "Old Garrett");
            InvokePrivateInstance(ui, "HandleStreamChunk", "Howdy");
            Assert.That(dialogue.text, Is.EqualTo("Howdy"));

            InvokePrivateInstance(ui, "HandleStreamChunk", " traveler.");
            Assert.That(dialogue.text, Is.EqualTo("Howdy traveler."));
        }

        [Test]
        public void ResponseShowsLastQuestionAboveNpcReply()
        {
            var ui = CreateUi(out var dialogue);
            SetPrivateField(ui, "_lastPlayerPrompt", "Can you share the recipe?");

            InvokePrivateInstance(ui, "HandleStreamStarted", "Old Garrett");
            InvokePrivateInstance(ui, "HandleStreamChunk", "Oh, bless my soul!");

            Assert.That(
                dialogue.text,
                Is.EqualTo("You: Can you share the recipe?\n\nOh, bless my soul!"));

            InvokePrivateInstance(ui, "HandleResponse", "Old Garrett", "Oh, bless my soul!");

            Assert.That(
                dialogue.text,
                Is.EqualTo("You: Can you share the recipe?\n\nOh, bless my soul!"));
        }

        private static DialogueChoiceUI CreateUi(out TextMeshProUGUI dialogue)
        {
            var root = new GameObject("DialogueChoiceUITest");
            var ui = root.AddComponent<DialogueChoiceUI>();

            var canvas = new GameObject("Canvas").AddComponent<Canvas>();
            var panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);

            var speaker = new GameObject("Speaker").AddComponent<TextMeshProUGUI>();
            speaker.transform.SetParent(root.transform, false);

            dialogue = new GameObject("Dialogue").AddComponent<TextMeshProUGUI>();
            dialogue.transform.SetParent(root.transform, false);

            var choices = new GameObject("Choices").transform;
            choices.SetParent(root.transform, false);

            var loading = new GameObject("Loading").AddComponent<TextMeshProUGUI>();
            loading.transform.SetParent(root.transform, false);

            SetPrivateField(ui, "dialogueCanvas", canvas);
            SetPrivateField(ui, "dialoguePanel", panel);
            SetPrivateField(ui, "speakerNameText", speaker);
            SetPrivateField(ui, "dialogueText", dialogue);
            SetPrivateField(ui, "choiceContainer", choices);
            SetPrivateField(ui, "loadingText", loading);
            return ui;
        }

        private static void InvokePrivateInstance(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, $"Missing private method '{methodName}'.");
            method.Invoke(target, args);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            field.SetValue(target, value);
        }
    }
}
