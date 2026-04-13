namespace FarmSimVR.Core.Story
{
    [System.Serializable]
    public sealed class StoryPackageSnapshot
    {
        public string PackageId;
        public int SchemaVersion = 1;
        public int PackageVersion = 1;
        public string DisplayName;
        public StoryBeatSnapshot[] Beats = System.Array.Empty<StoryBeatSnapshot>();
    }
}
