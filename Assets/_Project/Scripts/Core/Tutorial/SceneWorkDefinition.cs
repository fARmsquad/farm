namespace FarmSimVR.Core.Tutorial
{
    public readonly struct SceneWorkDefinition
    {
        public SceneWorkDefinition(
            int number,
            string sceneName,
            string scenePath,
            string displayName,
            string focusDescription,
            SceneWorkKind kind,
            string nextSceneName)
        {
            Number = number;
            SceneName = sceneName;
            ScenePath = scenePath;
            DisplayName = displayName;
            FocusDescription = focusDescription;
            Kind = kind;
            NextSceneName = nextSceneName;
        }

        public int Number { get; }
        public string SceneName { get; }
        public string ScenePath { get; }
        public string DisplayName { get; }
        public string FocusDescription { get; }
        public SceneWorkKind Kind { get; }
        public string NextSceneName { get; }

        public string NumberLabel => $"Scene {Number:00}";
        public bool HasNextScene => !string.IsNullOrWhiteSpace(NextSceneName);
    }
}
