using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using FarmSimVR.Core;
using FarmSimVR.MonoBehaviours;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class TownVoiceInputTests
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
        public void TownConversationExitGate_BlocksGoodbyeBeforeUnlockTurn()
        {
            var decision = TownConversationExitGate.Evaluate("I should get going. Goodbye.", 1);

            Assert.That(decision.IsExitPrompt, Is.True);
            Assert.That(decision.ShouldEndConversation, Is.False);
            Assert.That(decision.BlockedMessage, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void TownConversationExitGate_AllowsGoodbyeAfterUnlockTurn()
        {
            var decision = TownConversationExitGate.Evaluate("Take care for now.", 2);

            Assert.That(decision.IsExitPrompt, Is.True);
            Assert.That(decision.ShouldEndConversation, Is.True);
            Assert.That(decision.BlockedMessage, Is.Null);
        }

        [Test]
        public void TownPcm16WavEncoder_WritesRiffHeaderAndExpectedPayloadLength()
        {
            byte[] wav = TownPcm16WavEncoder.Encode(
                new[] { 0f, 1f, -1f, 0.5f },
                sampleRate: 16000,
                channels: 1,
                frameCount: 4);

            Assert.That(Encoding.ASCII.GetString(wav, 0, 4), Is.EqualTo("RIFF"));
            Assert.That(Encoding.ASCII.GetString(wav, 8, 4), Is.EqualTo("WAVE"));
            Assert.That(BitConverter.ToInt32(wav, 40), Is.EqualTo(8));
            Assert.That(BitConverter.ToInt16(wav, 44), Is.EqualTo(0));
            Assert.That(BitConverter.ToInt16(wav, 46), Is.EqualTo(short.MaxValue));
            Assert.That(BitConverter.ToInt16(wav, 48), Is.EqualTo(short.MinValue));
        }

        [Test]
        public void LlmConversationController_SubmitPlayerPrompt_BlocksEarlyExitPrompt()
        {
            var controller = CreateConversationController();
            SetPrivateField(controller, "_activeNpc", "Old Garrett");
            SetPrivateField(controller, "_history", new List<ChatMessage>
            {
                new("system", "sys"),
                new("assistant", "Howdy there.")
            });
            SetAutoProperty(controller, nameof(LLMConversationController.IsInConversation), true);

            string blocked = null;
            controller.OnExitBlocked += message => blocked = message;
            LogAssert.Expect(LogType.Log, "[Chat] [Player] Goodbye for now.");

            controller.SubmitPlayerPrompt("Goodbye for now.");

            Assert.That(controller.IsInConversation, Is.True);
            Assert.That(blocked, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void LlmConversationController_Awake_AddsOptionalVoiceInputController()
        {
            var gameObject = new GameObject("Conversation");
            var controller = gameObject.AddComponent<LLMConversationController>();
            InvokePrivateInstance(controller, "Awake");

            Assert.That(gameObject.GetComponent<TownVoiceInputController>(), Is.Not.Null);
        }

        private static LLMConversationController CreateConversationController()
        {
            var gameObject = new GameObject("Conversation");
            return gameObject.AddComponent<LLMConversationController>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            field.SetValue(target, value);
        }

        private static void SetAutoProperty(object target, string propertyName, object value)
        {
            string fieldName = $"<{propertyName}>k__BackingField";
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing backing field '{fieldName}'.");
            field.SetValue(target, value);
        }

        private static void InvokePrivateInstance(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing private method '{methodName}'.");
            method.Invoke(target, args);
        }
    }
}
