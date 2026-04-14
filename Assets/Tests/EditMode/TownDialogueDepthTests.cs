using System;
using System.Collections.Generic;
using FarmSimVR.Core;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class TownDialogueDepthTests
    {
        [Test]
        public void NPCPersonaCatalog_GarrettPrompt_ContainsSharedTownKnowledgeAndAntiRepetitionGuidance()
        {
            string prompt = NPCPersonaCatalog.GetSystemPrompt("Old Garrett");

            Assert.That(prompt, Does.Contain("Miss Edna"));
            Assert.That(prompt, Does.Contain("schoolhouse"));
            Assert.That(prompt, Does.Contain("fresh angle"));
            Assert.That(prompt, Does.Contain("new concrete detail"));
            Assert.That(prompt, Does.Contain("spoken reply"));
        }

        [Test]
        public void TownDialogueOptionComposer_BuildOptions_AvoidsRecentQuestionRepeats()
        {
            var history = new List<ChatMessage>
            {
                new("system", "You are Old Garrett."),
                new("user", "Start the conversation naturally."),
                new("assistant", "Well howdy there. The market square still wakes up before the sun."),
                new("user", "How has the town changed?")
            };

            string[] options = TownDialogueOptionComposer.BuildOptions(
                "Old Garrett",
                1,
                "The schoolhouse pulled folks together, and Miss Edna still keeps the market gossip moving.",
                Array.Empty<string>(),
                history);

            Assert.That(options, Has.Length.EqualTo(4));
            Assert.That(options, Has.None.EqualTo("How has the town changed?"));
            Assert.That(
                Array.Exists(
                    options,
                    option => option.Contains("schoolhouse", StringComparison.OrdinalIgnoreCase)
                        || option.Contains("Miss Edna", StringComparison.OrdinalIgnoreCase)
                        || option.Contains("market", StringComparison.OrdinalIgnoreCase)),
                Is.True);
        }

        [Test]
        public void TownDialogueOptionComposer_BuildOptions_UsesNpcRelationshipThreadsFromLatestReply()
        {
            var history = new List<ChatMessage>
            {
                new("system", "You are Mira the Baker."),
                new("user", "Start the conversation naturally."),
                new("assistant", "The ovens are warm and Pip already ran berries over."),
                new("user", "What keeps the bakery running?")
            };

            string[] options = TownDialogueOptionComposer.BuildOptions(
                "Mira the Baker",
                1,
                "Garrett keeps me in good flour and produce, and Pip knows every shortcut between the bakery and the market.",
                Array.Empty<string>(),
                history);

            Assert.That(options, Has.Length.EqualTo(4));
            Assert.That(options, Has.None.EqualTo("What keeps the bakery running?"));
            Assert.That(
                Array.Exists(
                    options,
                    option => option.Contains("Garrett", StringComparison.OrdinalIgnoreCase)
                        || option.Contains("Pip", StringComparison.OrdinalIgnoreCase)
                        || option.Contains("market", StringComparison.OrdinalIgnoreCase)),
                Is.True);
        }
    }
}
