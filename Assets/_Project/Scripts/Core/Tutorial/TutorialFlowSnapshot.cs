namespace FarmSimVR.Core.Tutorial
{
    public readonly struct TutorialFlowSnapshot
    {
        public TutorialFlowSnapshot(
            TutorialStep currentStep,
            string currentSceneName,
            bool introComplete,
            bool chickenHuntComplete,
            bool postChickenCutsceneComplete,
            bool placeholderCutsceneVisited,
            bool findToolsComplete,
            bool preFarmCutsceneComplete,
            bool farmTutorialStarted,
            bool farmTutorialComplete)
        {
            CurrentStep = currentStep;
            CurrentSceneName = currentSceneName;
            IntroComplete = introComplete;
            ChickenHuntComplete = chickenHuntComplete;
            PostChickenCutsceneComplete = postChickenCutsceneComplete;
            PlaceholderCutsceneVisited = placeholderCutsceneVisited;
            FindToolsComplete = findToolsComplete;
            PreFarmCutsceneComplete = preFarmCutsceneComplete;
            FarmTutorialStarted = farmTutorialStarted;
            FarmTutorialComplete = farmTutorialComplete;
        }

        public TutorialStep CurrentStep { get; }
        public string CurrentSceneName { get; }
        public bool IntroComplete { get; }
        public bool ChickenHuntComplete { get; }
        public bool PostChickenCutsceneComplete { get; }
        public bool PlaceholderCutsceneVisited { get; }
        public bool FindToolsComplete { get; }
        public bool PreFarmCutsceneComplete { get; }
        public bool FarmTutorialStarted { get; }
        public bool FarmTutorialComplete { get; }
    }
}
