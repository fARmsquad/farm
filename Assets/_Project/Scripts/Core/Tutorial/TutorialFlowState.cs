namespace FarmSimVR.Core.Tutorial
{
    public sealed class TutorialFlowState
    {
        public TutorialStep CurrentStep { get; internal set; }
        public string CurrentSceneName { get; internal set; }

        public bool IntroComplete { get; internal set; }
        public bool ChickenHuntComplete { get; internal set; }
        public bool PostChickenCutsceneComplete { get; internal set; }
        public bool PlaceholderCutsceneVisited { get; internal set; }
        public bool FindToolsComplete { get; internal set; }
        public bool PreFarmCutsceneComplete { get; internal set; }
        public bool FarmTutorialComplete { get; internal set; }
        public bool FarmTutorialStarted { get; internal set; }

        public bool IsTutorialComplete => FarmTutorialComplete;
    }
}
