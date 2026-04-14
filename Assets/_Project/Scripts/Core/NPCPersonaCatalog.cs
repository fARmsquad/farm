namespace FarmSimVR.Core
{
    /// <summary>
    /// Provides system prompts for NPC personas used in LLM conversations.
    /// Each NPC has a unique personality and backstory that shapes their dialogue.
    /// All prompts instruct the model to answer with direct spoken dialogue only.
    /// </summary>
    public static class NPCPersonaCatalog
    {
        private const string DirectReplyInstruction =
            "\n\nRespond only with the NPC's spoken reply. No JSON, no markdown, " +
            "no speaker labels, and no choice list. Keep the wording natural and " +
            "short enough to read while it streams. This must be the spoken reply only.";

        /// <summary>
        /// Returns the system prompt for the given NPC.
        /// Falls back to a generic prompt if no custom persona is defined.
        /// </summary>
        public static string GetSystemPrompt(string npcName)
        {
            return TownKnowledgeGraph.BuildSystemPrompt(npcName, DirectReplyInstruction);
        }
    }
}
