namespace FarmSimVR.Core.Story
{
    [System.Serializable]
    public sealed class StoryStoryboardSnapshot
    {
        public string StylePresetId;
        public StoryStoryboardShotSnapshot[] Shots = System.Array.Empty<StoryStoryboardShotSnapshot>();
    }
}
