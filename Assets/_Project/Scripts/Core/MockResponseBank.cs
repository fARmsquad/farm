using System.Collections.Generic;

namespace FarmSimVR.Core
{
    /// <summary>
    /// Pre-written mock responses for each NPC, indexed by conversation turn.
    /// Each response is a JSON string matching the LLMResponse format:
    ///   { "response": "...", "options": ["...", "...", "..."] }
    /// </summary>
    public static class MockResponseBank
    {
        private static readonly Dictionary<string, string[]> Responses = new()
        {
            ["Old Garrett"] = new[]
            {
                "{\"response\":\"Well now, ain't seen you around these parts before! Name's Garrett. Been farmin' this land since before your parents were born, I reckon. What brings you to our little village?\",\"options\":[\"I'm new here. What can you tell me about the town?\",\"I'm looking for work. Know of any?\",\"Just passing through, old timer.\",\"Goodbye.\"]}",
                "{\"response\":\"This here's a good town, full of good folk. We've got Mira's bakery — best bread this side of the river. Young Pip runs errands for everyone, always got the latest gossip. And the market stalls? Fresh produce every mornin'.\",\"options\":[\"Sounds like a lovely place. How long have you lived here?\",\"Any trouble around here I should know about?\",\"Where can I get something to eat?\",\"Goodbye.\"]}",
                "{\"response\":\"Oh, sixty-some years now. My granddad built the well right over there with his own two hands. Watched this town grow from three shacks to what you see today. It ain't much, but it's home.\",\"options\":[\"That's really impressive! You must have so many stories.\",\"What do you grow on your farm?\",\"I should get going. Nice meeting you!\",\"Goodbye.\"]}",
                "{\"response\":\"Stories? Ha! I could talk your ear off 'til the cows come home. But I'll save the good ones for another day. You seem like good people — come find me anytime you want to chat.\",\"options\":[\"I'd like that. Thanks, Garrett!\",\"Take care of yourself.\",\"Goodbye.\"]}"
            },

            ["Mira the Baker"] = new[]
            {
                "{\"response\":\"Welcome, welcome! Fresh bread just came out of the oven — can you smell it? I'm Mira, the baker. Everything here is made from local grain. What can I do for you, dear?\",\"options\":[\"It smells amazing! What do you have today?\",\"How did you become a baker?\",\"Do you know Old Garrett?\",\"Goodbye.\"]}",
                "{\"response\":\"Today I've got sourdough, honey wheat, and my famous cinnamon raisin loaf. Oh, and some berry tarts — Young Pip brought me the berries just this morning. Fresh as can be!\",\"options\":[\"The cinnamon raisin sounds incredible.\",\"Where does Pip find the berries?\",\"I'll take a sourdough loaf!\",\"Goodbye.\"]}",
                "{\"response\":\"My mother taught me, and her mother before her. Been kneading dough since I could reach the counter! The secret is patience — you can't rush good bread. Just like you can't rush anything worth doing.\",\"options\":[\"That's beautiful advice.\",\"Do you ever want to do something else?\",\"I bet the whole town loves your bread.\",\"Goodbye.\"]}",
                "{\"response\":\"Ha! Well, they'd better — I'm the only baker in town! But truly, seeing folks smile when they take that first bite... that's why I wake up at four in the morning. Now, don't be a stranger!\",\"options\":[\"I won't! Thanks, Mira.\",\"See you around!\",\"Goodbye.\"]}"
            },

            ["Young Pip"] = new[]
            {
                "{\"response\":\"Hey hey! You're new! I can tell because I know EVERYONE in this town and I definitely don't know you! I'm Pip! I do deliveries and stuff. Want to hear the latest news?\",\"options\":[\"Sure, what's the latest?\",\"You seem to know a lot about this place.\",\"Where do you find those berries for Mira?\",\"Goodbye.\"]}",
                "{\"response\":\"Okay so get THIS — someone saw lights in the old mill last night! Spooky, right?! Old Garrett says it's just fireflies but I think it's WAY more interesting than that. Maybe ghosts!\",\"options\":[\"Ghosts?! That's exciting!\",\"Probably just fireflies like Garrett said.\",\"Have you checked it out yourself?\",\"Goodbye.\"]}",
                "{\"response\":\"I wanted to but Mira said I can't go out past dark. She's not my mom but she kinda acts like it, you know? She makes sure I eat and stuff. She's pretty cool actually.\",\"options\":[\"Mira sounds really kind.\",\"Maybe I'll check out the old mill sometime.\",\"What else do you do around here for fun?\",\"Goodbye.\"]}",
                "{\"response\":\"Fun? I race the chickens! Well, I try to. They don't really cooperate. Oh, and I climb the big trees — you can see the whole valley from the top! You should try it sometime!\",\"options\":[\"Racing chickens sounds hilarious.\",\"I'll definitely try the tree climbing!\",\"See you later, Pip!\",\"Goodbye.\"]}"
            }
        };

        private const string FALLBACK_RESPONSE =
            "{\"response\":\"Hmm, I don't have much to say right now. Maybe come back later?\",\"options\":[\"Alright, see you later.\",\"Goodbye.\"]}";

        /// <summary>
        /// Returns the mock JSON response for the given NPC at the given turn.
        /// Falls back to a generic response if the NPC or turn is unknown.
        /// </summary>
        public static string GetResponse(string npcName, int turnIndex)
        {
            if (Responses.TryGetValue(npcName, out string[] lines))
            {
                int clampedIndex = turnIndex < lines.Length ? turnIndex : lines.Length - 1;
                return lines[clampedIndex];
            }

            return FALLBACK_RESPONSE;
        }
    }
}
