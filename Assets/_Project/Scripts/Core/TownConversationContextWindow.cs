using System;

namespace FarmSimVR.Core
{
    /// <summary>
    /// Dynamic per-NPC session context appended at request time.
    /// </summary>
    public sealed class TownConversationContextWindow
    {
        public TownConversationContextWindow(string additionalInstructions, string[] relayPrompts)
        {
            AdditionalInstructions = additionalInstructions ?? string.Empty;
            RelayPrompts = relayPrompts ?? Array.Empty<string>();
        }

        public string AdditionalInstructions { get; }
        public string[] RelayPrompts { get; }
    }
}
