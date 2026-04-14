using System;
using System.Collections.Generic;

namespace FarmSimVR.Core
{
    /// <summary>
    /// A reusable town fact that can be discovered, remembered, and relayed.
    /// </summary>
    public sealed class TownKnowledgeFact
    {
        private readonly Dictionary<string, string> _relayPromptTemplates;
        private readonly string _playerSummaryTemplate;

        public TownKnowledgeFact(
            string id,
            string topicSummary,
            string playerSummaryTemplate,
            string[] keywords,
            Dictionary<string, string> relayPromptTemplates)
        {
            Id = id;
            TopicSummary = topicSummary;
            _playerSummaryTemplate = playerSummaryTemplate ?? topicSummary ?? string.Empty;
            Keywords = keywords ?? Array.Empty<string>();
            _relayPromptTemplates = relayPromptTemplates ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string Id { get; }
        public string TopicSummary { get; }
        public string[] Keywords { get; }

        public string FormatPlayerSummary(string sourceNpcName)
        {
            return ReplaceSource(_playerSummaryTemplate, sourceNpcName);
        }

        public bool TryGetRelayPrompt(string targetNpcName, string sourceNpcName, out string prompt)
        {
            prompt = null;
            if (string.IsNullOrWhiteSpace(targetNpcName))
                return false;

            if (!_relayPromptTemplates.TryGetValue(targetNpcName, out string template) || string.IsNullOrWhiteSpace(template))
                return false;

            prompt = ReplaceSource(template, sourceNpcName);
            return !string.IsNullOrWhiteSpace(prompt);
        }

        private static string ReplaceSource(string template, string sourceNpcName)
        {
            string source = string.IsNullOrWhiteSpace(sourceNpcName) ? "Someone" : sourceNpcName;
            return template.Replace("{source}", source);
        }
    }
}
