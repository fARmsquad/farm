using System;
using System.Collections.Generic;
using System.Reflection;
using FarmSimVR.Core;
using NUnit.Framework;
using UnityEngine;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class TownConversationMemoryTests
    {
        [Test]
        public void TownConversationMemoryStore_DefaultContext_UsesNewcomerBase()
        {
            var store = new TownConversationMemoryStore();

            TownConversationContextWindow context = store.BuildContextWindow("Old Garrett");

            Assert.That(context.AdditionalInstructions, Does.Contain("newcomer"));
            Assert.That(context.AdditionalInstructions, Does.Contain("piecing together"));
            Assert.That(context.RelayPrompts, Is.Empty);
        }

        [Test]
        public void TownConversationMemoryStore_RecordNpcResponse_UnlocksRelayPromptForOtherNpc()
        {
            var store = new TownConversationMemoryStore();
            store.RecordNpcResponse(
                "Young Pip",
                "I swear those old mill lights are real, even if Garrett says they're only fireflies!");

            TownConversationContextWindow context = store.BuildContextWindow("Old Garrett");

            Assert.That(context.AdditionalInstructions, Does.Contain("old mill"));
            Assert.That(
                Array.Exists(
                    context.RelayPrompts,
                    prompt => prompt.Contains("Pip", StringComparison.OrdinalIgnoreCase)
                        && prompt.Contains("old mill", StringComparison.OrdinalIgnoreCase)),
                Is.True);
        }

        [Test]
        public void TownConversationMemoryStore_RecordPlayerPrompt_AddsSharedFactToNpcContext()
        {
            var store = new TownConversationMemoryStore();
            store.RecordNpcResponse(
                "Young Pip",
                "I swear those old mill lights are real, even if Garrett says they're only fireflies!");
            store.RecordPlayerPrompt(
                "Old Garrett",
                "Pip mentioned strange lights at the old mill. What do you make of that?");

            TownConversationContextWindow context = store.BuildContextWindow("Old Garrett");

            Assert.That(context.AdditionalInstructions, Does.Contain("already relayed"));
            Assert.That(context.AdditionalInstructions, Does.Contain("Pip"));
            Assert.That(
                Array.Exists(
                    context.RelayPrompts,
                    prompt => prompt.Contains("old mill", StringComparison.OrdinalIgnoreCase)),
                Is.False);
        }

        [Test]
        public void TownDialogueOptionComposer_BuildOptions_UsesRelayPromptsFromMemoryContext()
        {
            var store = new TownConversationMemoryStore();
            store.RecordNpcResponse(
                "Young Pip",
                "I swear those old mill lights are real, even if Garrett says they're only fireflies!");

            TownConversationContextWindow context = store.BuildContextWindow("Old Garrett");
            string[] options = TownDialogueOptionComposer.BuildOptions(
                "Old Garrett",
                1,
                "The square feels different at dawn, and Miss Edna notices every change before breakfast.",
                Array.Empty<string>(),
                null,
                context);

            Assert.That(options, Has.Length.EqualTo(4));
            Assert.That(
                Array.Exists(
                    options,
                    option => option.Contains("Pip", StringComparison.OrdinalIgnoreCase)
                        && option.Contains("old mill", StringComparison.OrdinalIgnoreCase)),
                Is.True);
        }

        [Test]
        public void LLMConversationController_BuildRequestMessages_AppendsDynamicMemoryContext()
        {
            var gameObject = new GameObject("ConversationController");
            try
            {
                var controller = gameObject.AddComponent<FarmSimVR.MonoBehaviours.LLMConversationController>();
                var memoryStore = ReadPrivateField<TownConversationMemoryStore>(controller, "_conversationMemory");
                memoryStore.RecordNpcResponse(
                    "Young Pip",
                    "I swear those old mill lights are real, even if Garrett says they're only fireflies!");

                SetPrivateField(
                    controller,
                    "_history",
                    new List<ChatMessage>
                    {
                        new("system", NPCPersonaCatalog.GetSystemPrompt("Old Garrett")),
                        new("user", "Pip mentioned strange lights at the old mill. What do you make of that?")
                    });

                TownConversationContextWindow context = memoryStore.BuildContextWindow("Old Garrett");
                List<ChatMessage> requestMessages = InvokePrivateInstance<List<ChatMessage>>(
                    controller,
                    "BuildRequestMessages",
                    context);

                Assert.That(requestMessages, Has.Count.EqualTo(3));
                Assert.That(requestMessages[1].Role, Is.EqualTo("system"));
                Assert.That(requestMessages[1].Content, Does.Contain("newcomer"));
                Assert.That(requestMessages[1].Content, Does.Contain("old mill"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            field.SetValue(target, value);
        }

        private static T ReadPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            return (T)field.GetValue(target);
        }

        private static T InvokePrivateInstance<T>(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing private method '{methodName}'.");
            return (T)method.Invoke(target, args);
        }

        // ── Inventory context injection ───────────────────────────────────────

        [Test]
        public void BuildContextWindow_WithInventorySummary_IncludesSummaryInAdditionalInstructions()
        {
            var store = new TownConversationMemoryStore();
            const string summary = "Player currently has: 3 egg.";

            TownConversationContextWindow context = store.BuildContextWindow("Mira the Baker", summary);

            Assert.That(context.AdditionalInstructions, Does.Contain("3 egg"));
        }

        [Test]
        public void BuildContextWindow_WithNullInventorySummary_DoesNotMentionInventory()
        {
            var store = new TownConversationMemoryStore();

            TownConversationContextWindow context = store.BuildContextWindow("Mira the Baker", null);

            Assert.That(context.AdditionalInstructions, Does.Not.Contain("egg"));
            Assert.That(context.AdditionalInstructions, Does.Not.Contain("inventory"));
        }

        [Test]
        public void BuildContextWindow_WithEmptyInventorySummary_DoesNotMentionInventory()
        {
            var store = new TownConversationMemoryStore();

            TownConversationContextWindow context = store.BuildContextWindow("Mira the Baker", string.Empty);

            Assert.That(context.AdditionalInstructions, Does.Not.Contain("inventory"));
        }

        [Test]
        public void BuildContextWindow_ExistingCallWithoutInventory_StillWorks()
        {
            // Regression guard: callers that omit the inventory arg get unchanged behaviour.
            var store = new TownConversationMemoryStore();

            TownConversationContextWindow context = store.BuildContextWindow("Old Garrett");

            Assert.That(context.AdditionalInstructions, Does.Contain("newcomer"));
        }
    }
}
