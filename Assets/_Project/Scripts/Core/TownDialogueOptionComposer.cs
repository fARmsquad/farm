using System;
using System.Collections.Generic;

namespace FarmSimVR.Core
{
    /// <summary>
    /// Builds natural four-choice follow-ups for streamed Town dialogue.
    /// </summary>
    public static class TownDialogueOptionComposer
    {
        private const int DisplayOptionCount = 4;
        private const int RecentQuestionWindow = 4;

        private static readonly string[] CookingKeywords = { "recipe", "pie", "cook", "cooking", "oven", "chowder", "bake", "baking" };
        private static readonly string[] TownHistoryKeywords = { "town", "village", "market", "community", "history", "granddad", "years", "well", "home" };
        private static readonly string[] FarmingKeywords = { "farm", "field", "crop", "harvest", "tomato", "corn", "wheat" };
        private static readonly string[] BakeryKeywords = { "bakery", "bread", "dough", "flour", "berry", "tart" };
        private static readonly string[] AdventureKeywords = { "delivery", "deliveries", "gossip", "news", "mill", "ghost", "fireflies", "chicken", "tree", "adventure" };

        private static readonly string[] CookingPrompts =
        {
            "Can you share the recipe?",
            "What else do you like to make?",
            "Who taught you how to cook like that?"
        };

        private static readonly string[] TownHistoryPrompts =
        {
            "How has the town changed?",
            "What's your favorite story from back then?",
            "Who should I meet next?"
        };

        private static readonly string[] FarmingPrompts =
        {
            "What do you grow out on your farm?",
            "How's this season treating you?",
            "What's hardest to grow here?"
        };

        private static readonly string[] BakeryPrompts =
        {
            "What should I try first?",
            "How early do you start baking?",
            "Do you always use local ingredients?"
        };

        private static readonly string[] AdventurePrompts =
        {
            "What's the latest around town?",
            "Did you really see that yourself?",
            "What do you do when you're off deliveries?"
        };

        private static readonly string[] GenericPrompts =
        {
            "Who should I talk to next?",
            "What would you want a newcomer to notice here?",
            "Who sees this place differently than you do?",
            "What part of that matters most to you?",
            "What's been happening around town lately?",
            "What keeps people busy around here?"
        };

        public static string[] BuildOptions(string npcName, int turnIndex, string responseText, string[] modelOptions)
        {
            return BuildOptions(npcName, turnIndex, responseText, modelOptions, null, null);
        }

        public static string[] BuildOptions(
            string npcName,
            int turnIndex,
            string responseText,
            string[] modelOptions,
            IReadOnlyList<ChatMessage> history)
        {
            return BuildOptions(npcName, turnIndex, responseText, modelOptions, history, null);
        }

        public static string[] BuildOptions(
            string npcName,
            int turnIndex,
            string responseText,
            string[] modelOptions,
            IReadOnlyList<ChatMessage> history,
            TownConversationContextWindow contextWindow)
        {
            bool allowGoodbye = turnIndex >= TownConversationExitGate.GoodbyeUnlockTurn;
            var options = new List<string>(DisplayOptionCount);
            HashSet<string> recentQuestions = GetRecentUserQuestions(history);
            int rotationSeed = GetRotationSeed(turnIndex, history);

            AddOptions(options, modelOptions, allowGoodbye, recentQuestions);
            AddOptions(options, contextWindow?.RelayPrompts, allowGoodbye, recentQuestions);
            AppendEntityOptions(options, responseText, allowGoodbye, recentQuestions);
            AppendTopicOptions(options, responseText, allowGoodbye, recentQuestions);
            AddRotatedOptions(options, GetNpcPrompts(npcName), rotationSeed, allowGoodbye, recentQuestions);
            AddRotatedOptions(options, GenericPrompts, rotationSeed + 1, allowGoodbye, recentQuestions);
            EnsureDisplayOptions(options, turnIndex, allowGoodbye, recentQuestions);

            return options.ToArray();
        }

        private static void AppendEntityOptions(
            List<string> options,
            string responseText,
            bool allowGoodbye,
            HashSet<string> recentQuestions)
        {
            AddKeywordOptions(
                options,
                responseText,
                new[] { "Miss Edna", "Edna", "general store", "store" },
                new[]
                {
                    "What does Miss Edna know before anyone else does?",
                    "How did Miss Edna end up running the general store?"
                },
                allowGoodbye,
                recentQuestions);

            AddKeywordOptions(
                options,
                responseText,
                new[] { "schoolhouse", "school house" },
                new[]
                {
                    "How did the schoolhouse bring people together?",
                    "Who showed up when the schoolhouse was built?"
                },
                allowGoodbye,
                recentQuestions);

            AddKeywordOptions(
                options,
                responseText,
                new[] { "Mira", "bakery" },
                new[]
                {
                    "How did Mira earn the town's trust?",
                    "What keeps Mira's bakery running every morning?"
                },
                allowGoodbye,
                recentQuestions);

            AddKeywordOptions(
                options,
                responseText,
                new[] { "Pip", "delivery", "deliveries" },
                new[]
                {
                    "What kind of trouble does Pip find?",
                    "Why does Pip know so much about everybody?"
                },
                allowGoodbye,
                recentQuestions);

            AddKeywordOptions(
                options,
                responseText,
                new[] { "old mill", "mill lights", "mill" },
                new[]
                {
                    "What's really going on at the old mill?",
                    "Who keeps talking about the old mill lights?"
                },
                allowGoodbye,
                recentQuestions);

            AddKeywordOptions(
                options,
                responseText,
                new[] { "well", "stone well" },
                new[]
                {
                    "Why does the old well matter so much here?",
                    "What happened when the well was first built?"
                },
                allowGoodbye,
                recentQuestions);
        }

        private static void AppendTopicOptions(
            List<string> options,
            string responseText,
            bool allowGoodbye,
            HashSet<string> recentQuestions)
        {
            AddKeywordOptions(options, responseText, CookingKeywords, CookingPrompts, allowGoodbye, recentQuestions);
            AddKeywordOptions(options, responseText, TownHistoryKeywords, TownHistoryPrompts, allowGoodbye, recentQuestions);
            AddKeywordOptions(options, responseText, FarmingKeywords, FarmingPrompts, allowGoodbye, recentQuestions);
            AddKeywordOptions(options, responseText, BakeryKeywords, BakeryPrompts, allowGoodbye, recentQuestions);
            AddKeywordOptions(options, responseText, AdventureKeywords, AdventurePrompts, allowGoodbye, recentQuestions);
        }

        private static void AddKeywordOptions(
            List<string> options,
            string responseText,
            string[] keywords,
            string[] prompts,
            bool allowGoodbye,
            HashSet<string> recentQuestions)
        {
            if (!ContainsAny(responseText, keywords))
                return;

            AddOptions(options, prompts, allowGoodbye, recentQuestions);
        }

        private static void AddOptions(
            List<string> options,
            string[] source,
            bool allowGoodbye,
            HashSet<string> recentQuestions)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Length && options.Count < DisplayOptionCount; i++)
                TryAddOption(options, source[i], allowGoodbye, recentQuestions);
        }

        private static void AddRotatedOptions(
            List<string> options,
            string[] source,
            int offset,
            bool allowGoodbye,
            HashSet<string> recentQuestions)
        {
            if (source == null || source.Length == 0)
                return;

            int startIndex = offset % source.Length;
            for (int i = 0; i < source.Length && options.Count < DisplayOptionCount; i++)
            {
                string option = source[(startIndex + i) % source.Length];
                TryAddOption(options, option, allowGoodbye, recentQuestions);
            }
        }

        private static void EnsureDisplayOptions(
            List<string> options,
            int turnIndex,
            bool allowGoodbye,
            HashSet<string> recentQuestions)
        {
            if (allowGoodbye)
                EnsureGoodbye(options);

            if (options.Count < DisplayOptionCount)
                AddRotatedOptions(options, GenericPrompts, turnIndex + 1, allowGoodbye, recentQuestions);

            while (options.Count < DisplayOptionCount)
            {
                string fallback = GenericPrompts[(turnIndex + options.Count) % GenericPrompts.Length];
                if (!TryAddOption(options, fallback, allowGoodbye, recentQuestions))
                    break;
            }

            while (options.Count > DisplayOptionCount)
                options.RemoveAt(options.Count - 1);
        }

        private static void EnsureGoodbye(List<string> options)
        {
            if (ContainsGoodbye(options))
                return;

            if (options.Count >= DisplayOptionCount)
            {
                options[DisplayOptionCount - 1] = TownConversationExitGate.ExitOptionLabel;
                return;
            }

            options.Add(TownConversationExitGate.ExitOptionLabel);
        }

        private static bool TryAddOption(
            List<string> options,
            string option,
            bool allowGoodbye,
            HashSet<string> recentQuestions)
        {
            string trimmed = option?.Trim();
            if (string.IsNullOrEmpty(trimmed) || IsContinueOption(trimmed) || IsGenericGoodbye(trimmed))
                return false;

            if (!allowGoodbye && TownConversationExitGate.IsExitPrompt(trimmed))
                return false;

            if (recentQuestions != null && recentQuestions.Contains(NormalizeForComparison(trimmed)))
                return false;

            if (ContainsOption(options, trimmed) || options.Count >= DisplayOptionCount)
                return false;

            options.Add(trimmed);
            return true;
        }

        private static string[] GetNpcPrompts(string npcName)
        {
            return TownKnowledgeGraph.GetProfile(npcName).ConversationThreads;
        }

        private static bool ContainsAny(string text, string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(text) || keywords == null)
                return false;

            for (int i = 0; i < keywords.Length; i++)
            {
                if (text.IndexOf(keywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static int GetRotationSeed(int turnIndex, IReadOnlyList<ChatMessage> history)
        {
            return turnIndex + CountMeaningfulUserMessages(history);
        }

        private static int CountMeaningfulUserMessages(IReadOnlyList<ChatMessage> history)
        {
            if (history == null)
                return 0;

            int count = 0;
            for (int i = 0; i < history.Count; i++)
            {
                if (!string.Equals(history[i].Role, "user", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (IsMeaningfulPlayerQuestion(history[i].Content))
                    count++;
            }

            return count;
        }

        private static HashSet<string> GetRecentUserQuestions(IReadOnlyList<ChatMessage> history)
        {
            var recentQuestions = new HashSet<string>(StringComparer.Ordinal);
            if (history == null)
                return recentQuestions;

            int added = 0;
            for (int i = history.Count - 1; i >= 0 && added < RecentQuestionWindow; i--)
            {
                ChatMessage message = history[i];
                if (!string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!IsMeaningfulPlayerQuestion(message.Content))
                    continue;

                recentQuestions.Add(NormalizeForComparison(message.Content));
                added++;
            }

            return recentQuestions;
        }

        private static bool IsMeaningfulPlayerQuestion(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            if (content.StartsWith("Opening cue:", StringComparison.OrdinalIgnoreCase))
                return false;

            return !TownConversationExitGate.IsExitPrompt(content);
        }

        private static string NormalizeForComparison(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var buffer = new char[text.Length];
            int count = 0;
            bool previousWasSpace = false;

            for (int i = 0; i < text.Length; i++)
            {
                char current = char.ToLowerInvariant(text[i]);
                if (char.IsLetterOrDigit(current))
                {
                    buffer[count++] = current;
                    previousWasSpace = false;
                    continue;
                }

                if (previousWasSpace)
                    continue;

                if (count == 0)
                    continue;

                buffer[count++] = ' ';
                previousWasSpace = true;
            }

            while (count > 0 && buffer[count - 1] == ' ')
                count--;

            return new string(buffer, 0, count);
        }

        private static bool ContainsGoodbye(List<string> options)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (TownConversationExitGate.IsExitPrompt(options[i]))
                    return true;
            }

            return false;
        }

        private static bool ContainsOption(List<string> options, string candidate)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (string.Equals(options[i], candidate, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool IsContinueOption(string option)
        {
            return string.Equals(option, "Continue...", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsGenericGoodbye(string option)
        {
            return string.Equals(option, "Goodbye.", StringComparison.OrdinalIgnoreCase)
                || string.Equals(option, "Goodbye", StringComparison.OrdinalIgnoreCase);
        }

    }
}
