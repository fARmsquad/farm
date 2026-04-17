using System;

namespace FarmSimVR.Core
{
    /// <summary>
    /// Shared Town rule for deciding when a goodbye-style prompt may end a conversation.
    /// </summary>
    public static class TownConversationExitGate
    {
        public const int GoodbyeUnlockTurn = 0;
        public const string ExitOptionLabel = "I should get going. Goodbye.";

        private const string EarlyExitBlockedMessage = "Let's talk a little longer before you head out.";

        public static TownConversationExitDecision Evaluate(string prompt, int assistantTurnCount)
        {
            if (!IsExitPrompt(prompt))
                return TownConversationExitDecision.Continue();

            if (assistantTurnCount >= GoodbyeUnlockTurn)
                return TownConversationExitDecision.EndConversation();

            return TownConversationExitDecision.Block(EarlyExitBlockedMessage);
        }

        public static bool IsExitPrompt(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return false;

            return prompt.IndexOf("goodbye", StringComparison.OrdinalIgnoreCase) >= 0
                || prompt.IndexOf("farewell", StringComparison.OrdinalIgnoreCase) >= 0
                || prompt.IndexOf("see you", StringComparison.OrdinalIgnoreCase) >= 0
                || prompt.IndexOf("take care", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    public readonly struct TownConversationExitDecision
    {
        private TownConversationExitDecision(bool isExitPrompt, bool shouldEndConversation, string blockedMessage)
        {
            IsExitPrompt = isExitPrompt;
            ShouldEndConversation = shouldEndConversation;
            BlockedMessage = blockedMessage;
        }

        public bool IsExitPrompt { get; }
        public bool ShouldEndConversation { get; }
        public string BlockedMessage { get; }

        internal static TownConversationExitDecision Continue()
        {
            return new TownConversationExitDecision(false, false, null);
        }

        internal static TownConversationExitDecision EndConversation()
        {
            return new TownConversationExitDecision(true, true, null);
        }

        internal static TownConversationExitDecision Block(string blockedMessage)
        {
            return new TownConversationExitDecision(true, false, blockedMessage);
        }
    }
}
