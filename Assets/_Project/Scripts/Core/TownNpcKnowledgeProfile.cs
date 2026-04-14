namespace FarmSimVR.Core
{
    /// <summary>
    /// Shared world-facing profile for a Town NPC.
    /// </summary>
    public sealed class TownNpcKnowledgeProfile
    {
        public TownNpcKnowledgeProfile(
            string npcName,
            string identitySummary,
            string speechStyle,
            string openingPrompt,
            string[] personalHistory,
            string[] relationshipFacts,
            string[] conversationThreads)
        {
            NpcName = npcName;
            IdentitySummary = identitySummary;
            SpeechStyle = speechStyle;
            OpeningPrompt = openingPrompt;
            PersonalHistory = personalHistory ?? System.Array.Empty<string>();
            RelationshipFacts = relationshipFacts ?? System.Array.Empty<string>();
            ConversationThreads = conversationThreads ?? System.Array.Empty<string>();
        }

        public string NpcName { get; }
        public string IdentitySummary { get; }
        public string SpeechStyle { get; }
        public string OpeningPrompt { get; }
        public string[] PersonalHistory { get; }
        public string[] RelationshipFacts { get; }
        public string[] ConversationThreads { get; }
    }
}
