using System;
using System.Collections.Generic;
using System.Text;

namespace FarmSimVR.Core
{
    /// <summary>
    /// Session-scoped player and NPC conversation memory for the Town slice.
    /// </summary>
    public sealed class TownConversationMemoryStore
    {
        private const int MaxKnownFactsInContext = 3;
        private const int MaxNpcFactsInContext = 2;
        private const int MaxRelayPrompts = 2;
        private const string PlayerBaseContext =
            "The player is a newcomer piecing together how the town's people, places, and histories connect.";

        private readonly List<PlayerKnownFact> _knownFacts = new();
        private readonly HashSet<string> _knownFactIds = new(StringComparer.Ordinal);
        private readonly Dictionary<string, NpcMemoryState> _npcStates =
            new(StringComparer.OrdinalIgnoreCase);

        public void RecordNpcResponse(string npcName, string responseText)
        {
            if (string.IsNullOrWhiteSpace(npcName) || string.IsNullOrWhiteSpace(responseText))
                return;

            NpcMemoryState state = GetState(npcName);
            IReadOnlyList<TownKnowledgeFact> facts = TownKnowledgeGraph.MatchFacts(responseText);
            for (int i = 0; i < facts.Count; i++)
            {
                TownKnowledgeFact fact = facts[i];
                state.ExplainedFactIds.Add(fact.Id);
                if (_knownFactIds.Add(fact.Id))
                    _knownFacts.Add(new PlayerKnownFact(fact.Id, npcName));
            }
        }

        public void RecordPlayerPrompt(string npcName, string promptText)
        {
            if (string.IsNullOrWhiteSpace(npcName) || string.IsNullOrWhiteSpace(promptText))
                return;

            NpcMemoryState state = GetState(npcName);
            IReadOnlyList<TownKnowledgeFact> facts = TownKnowledgeGraph.MatchFacts(promptText);
            for (int i = 0; i < facts.Count; i++)
            {
                if (_knownFactIds.Contains(facts[i].Id))
                    state.RelayedFactIds.Add(facts[i].Id);
            }
        }

        public TownConversationContextWindow BuildContextWindow(string npcName)
        {
            NpcMemoryState state = GetState(npcName);
            string instructions = BuildInstructions(npcName, state);
            string[] relayPrompts = BuildRelayPrompts(npcName, state);
            return new TownConversationContextWindow(instructions, relayPrompts);
        }

        private string BuildInstructions(string npcName, NpcMemoryState state)
        {
            var builder = new StringBuilder(768);
            builder.Append("Dynamic session context:\n");
            builder.Append("- ").Append(PlayerBaseContext).Append('\n');

            AppendKnownFacts(builder);
            AppendNpcExplainedFacts(builder, state);
            AppendRelayedFacts(builder, state);

            builder.Append("- If the player references something learned elsewhere, respond as if it is shared context and build on it.\n");

            if (string.IsNullOrWhiteSpace(npcName))
                return builder.ToString();

            builder.Append("- Keep this grounded in what ")
                .Append(npcName)
                .Append(" would genuinely know or care about.\n");

            return builder.ToString();
        }

        private void AppendKnownFacts(StringBuilder builder)
        {
            if (_knownFacts.Count == 0)
                return;

            builder.Append("- The player currently knows:\n");
            AppendRecentKnownFacts(builder, _knownFacts, MaxKnownFactsInContext, includeSourceSummary: true);
        }

        private void AppendNpcExplainedFacts(StringBuilder builder, NpcMemoryState state)
        {
            if (state.ExplainedFactIds.Count == 0)
                return;

            builder.Append("- You already discussed with the player:\n");
            AppendMatchingKnownFacts(builder, state.ExplainedFactIds, MaxNpcFactsInContext, includeSourceSummary: false);
        }

        private void AppendRelayedFacts(StringBuilder builder, NpcMemoryState state)
        {
            if (state.RelayedFactIds.Count == 0)
                return;

            builder.Append("- The player already relayed to you:\n");
            AppendMatchingKnownFacts(builder, state.RelayedFactIds, MaxNpcFactsInContext, includeSourceSummary: true);
            builder.Append("- Treat those relayed details as already shared.\n");
        }

        private void AppendRecentKnownFacts(
            StringBuilder builder,
            List<PlayerKnownFact> facts,
            int maxCount,
            bool includeSourceSummary)
        {
            int appended = 0;
            for (int i = facts.Count - 1; i >= 0 && appended < maxCount; i--)
            {
                if (!TryAppendFactLine(builder, facts[i], includeSourceSummary))
                    continue;

                appended++;
            }
        }

        private void AppendMatchingKnownFacts(
            StringBuilder builder,
            HashSet<string> factIds,
            int maxCount,
            bool includeSourceSummary)
        {
            int appended = 0;
            for (int i = _knownFacts.Count - 1; i >= 0 && appended < maxCount; i--)
            {
                PlayerKnownFact fact = _knownFacts[i];
                if (!factIds.Contains(fact.FactId))
                    continue;

                if (!TryAppendFactLine(builder, fact, includeSourceSummary))
                    continue;

                appended++;
            }
        }

        private bool TryAppendFactLine(
            StringBuilder builder,
            PlayerKnownFact knownFact,
            bool includeSourceSummary)
        {
            TownKnowledgeFact fact = TownKnowledgeGraph.GetFact(knownFact.FactId);
            if (fact == null)
                return false;

            string line = includeSourceSummary
                ? fact.FormatPlayerSummary(knownFact.SourceNpcName)
                : fact.TopicSummary;

            if (string.IsNullOrWhiteSpace(line))
                return false;

            builder.Append("  - ").Append(line).Append('\n');
            return true;
        }

        private string[] BuildRelayPrompts(string npcName, NpcMemoryState state)
        {
            if (string.IsNullOrWhiteSpace(npcName) || _knownFacts.Count == 0)
                return Array.Empty<string>();

            var relayPrompts = new List<string>(MaxRelayPrompts);
            for (int i = _knownFacts.Count - 1; i >= 0 && relayPrompts.Count < MaxRelayPrompts; i--)
            {
                PlayerKnownFact knownFact = _knownFacts[i];
                if (string.Equals(knownFact.SourceNpcName, npcName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (state.RelayedFactIds.Contains(knownFact.FactId))
                    continue;

                TownKnowledgeFact fact = TownKnowledgeGraph.GetFact(knownFact.FactId);
                if (fact == null || !fact.TryGetRelayPrompt(npcName, knownFact.SourceNpcName, out string relayPrompt))
                    continue;

                relayPrompts.Add(relayPrompt);
            }

            return relayPrompts.ToArray();
        }

        private NpcMemoryState GetState(string npcName)
        {
            string key = string.IsNullOrWhiteSpace(npcName) ? string.Empty : npcName.Trim();
            if (_npcStates.TryGetValue(key, out NpcMemoryState state))
                return state;

            state = new NpcMemoryState();
            _npcStates[key] = state;
            return state;
        }

        private sealed class PlayerKnownFact
        {
            public PlayerKnownFact(string factId, string sourceNpcName)
            {
                FactId = factId;
                SourceNpcName = sourceNpcName;
            }

            public string FactId { get; }
            public string SourceNpcName { get; }
        }

        private sealed class NpcMemoryState
        {
            public HashSet<string> ExplainedFactIds { get; } = new(StringComparer.Ordinal);
            public HashSet<string> RelayedFactIds { get; } = new(StringComparer.Ordinal);
        }
    }
}
