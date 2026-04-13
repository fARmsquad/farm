namespace FarmSimVR.Core.Story
{
    [System.Serializable]
    public sealed class StoryBeatSnapshot
    {
        public string BeatId;
        public string DisplayName;
        public string Kind;
        public string SceneName;
        public string NextSceneName;
        public StorySequenceStepSnapshot[] SequenceSteps = System.Array.Empty<StorySequenceStepSnapshot>();
        public StoryMinigameConfigSnapshot Minigame;
    }
}
