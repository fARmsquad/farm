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
        private const int GoodbyeUnlockTurn = 2;
        private const string ScriptedGoodbye = "I should get going. Goodbye.";

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

        private static readonly string[] GarrettPrompts =
        {
            "What can you tell me about the town?",
            "How long have you lived here?",
            "What do you grow out on your farm?",
            "What's your favorite story from back then?",
            "Who's someone in town I should meet?"
        };

        private static readonly string[] MiraPrompts =
        {
            "What should I try first?",
            "How early do you start baking?",
            "Do you always use local ingredients?",
            "What's your favorite thing to bake?",
            "What does Pip usually bring by?"
        };

        private static readonly string[] PipPrompts =
        {
            "What's the latest around town?",
            "Did you really see that yourself?",
            "What do you do when you're off deliveries?",
            "Who's the most interesting person here?",
            "Where do you go when you want an adventure?"
        };

        private static readonly string[] GenericPrompts =
        {
            "What should I know about this place?",
            "Who should I talk to next?",
            "What's been happening around town lately?",
            "What's the best part of living here?",
            "What would you recommend I do next?",
            "What keeps people busy around here?"
        };

        public static string[] BuildOptions(string npcName, int turnIndex, string responseText, string[] modelOptions)
        {
            bool allowGoodbye = turnIndex >= GoodbyeUnlockTurn;
            var options = new List<string>(DisplayOptionCount);

            AddOptions(options, modelOptions, allowGoodbye);
            AppendTopicOptions(options, responseText, allowGoodbye);
            AddRotatedOptions(options, GetNpcPrompts(npcName), turnIndex, allowGoodbye);
            AddRotatedOptions(options, GenericPrompts, turnIndex, allowGoodbye);
            EnsureDisplayOptions(options, turnIndex, allowGoodbye);

            return options.ToArray();
        }

        private static void AppendTopicOptions(List<string> options, string responseText, bool allowGoodbye)
        {
            AddKeywordOptions(options, responseText, CookingKeywords, CookingPrompts, allowGoodbye);
            AddKeywordOptions(options, responseText, TownHistoryKeywords, TownHistoryPrompts, allowGoodbye);
            AddKeywordOptions(options, responseText, FarmingKeywords, FarmingPrompts, allowGoodbye);
            AddKeywordOptions(options, responseText, BakeryKeywords, BakeryPrompts, allowGoodbye);
            AddKeywordOptions(options, responseText, AdventureKeywords, AdventurePrompts, allowGoodbye);
        }

        private static void AddKeywordOptions(
            List<string> options,
            string responseText,
            string[] keywords,
            string[] prompts,
            bool allowGoodbye)
        {
            if (!ContainsAny(responseText, keywords))
                return;

            AddOptions(options, prompts, allowGoodbye);
        }

        private static void AddOptions(List<string> options, string[] source, bool allowGoodbye)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Length && options.Count < DisplayOptionCount; i++)
                TryAddOption(options, source[i], allowGoodbye);
        }

        private static void AddRotatedOptions(List<string> options, string[] source, int offset, bool allowGoodbye)
        {
            if (source == null || source.Length == 0)
                return;

            int startIndex = offset % source.Length;
            for (int i = 0; i < source.Length && options.Count < DisplayOptionCount; i++)
            {
                string option = source[(startIndex + i) % source.Length];
                TryAddOption(options, option, allowGoodbye);
            }
        }

        private static void EnsureDisplayOptions(List<string> options, int turnIndex, bool allowGoodbye)
        {
            if (allowGoodbye)
                EnsureGoodbye(options);

            if (options.Count < DisplayOptionCount)
                AddRotatedOptions(options, GenericPrompts, turnIndex + 1, allowGoodbye);

            while (options.Count < DisplayOptionCount)
            {
                string fallback = GenericPrompts[(turnIndex + options.Count) % GenericPrompts.Length];
                if (!TryAddOption(options, fallback, allowGoodbye))
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
                options[DisplayOptionCount - 1] = ScriptedGoodbye;
                return;
            }

            options.Add(ScriptedGoodbye);
        }

        private static bool TryAddOption(List<string> options, string option, bool allowGoodbye)
        {
            string trimmed = option?.Trim();
            if (string.IsNullOrEmpty(trimmed) || IsContinueOption(trimmed) || IsGenericGoodbye(trimmed))
                return false;

            if (!allowGoodbye && IsGoodbye(trimmed))
                return false;

            if (ContainsOption(options, trimmed) || options.Count >= DisplayOptionCount)
                return false;

            options.Add(trimmed);
            return true;
        }

        private static string[] GetNpcPrompts(string npcName)
        {
            return npcName switch
            {
                "Old Garrett" => GarrettPrompts,
                "Mira the Baker" => MiraPrompts,
                "Young Pip" => PipPrompts,
                _ => GenericPrompts
            };
        }

        private static bool ContainsAny(string text, string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            for (int i = 0; i < keywords.Length; i++)
            {
                if (text.IndexOf(keywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static bool ContainsGoodbye(List<string> options)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (IsGoodbye(options[i]))
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

        private static bool IsGoodbye(string option)
        {
            if (string.IsNullOrWhiteSpace(option))
                return false;

            return option.IndexOf("goodbye", StringComparison.OrdinalIgnoreCase) >= 0
                || option.IndexOf("farewell", StringComparison.OrdinalIgnoreCase) >= 0
                || option.IndexOf("see you", StringComparison.OrdinalIgnoreCase) >= 0
                || option.IndexOf("take care", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
