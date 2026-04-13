namespace FarmSimVR.Core.Story
{
    public enum StoryBeatKind
    {
        Unknown = 0,
        Cutscene = 1,
        Minigame = 2,
    }

    public static class StoryBeatKindParser
    {
        public static bool TryParse(string value, out StoryBeatKind kind)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                kind = StoryBeatKind.Unknown;
                return false;
            }

            switch (value.Trim().ToLowerInvariant())
            {
                case "cutscene":
                    kind = StoryBeatKind.Cutscene;
                    return true;
                case "minigame":
                    kind = StoryBeatKind.Minigame;
                    return true;
                default:
                    kind = StoryBeatKind.Unknown;
                    return false;
            }
        }
    }
}
