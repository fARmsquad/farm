namespace FarmSimVR.Core.Tutorial
{
    public sealed class TutorialFlowService
    {
        public TutorialFlowService()
        {
            State = new TutorialFlowState();
            Reset();
        }

        public TutorialFlowState State { get; }

        public void Reset()
        {
            State.CurrentStep = TutorialStep.Intro;
            State.CurrentSceneName = TutorialSceneCatalog.IntroSceneName;
            State.IntroComplete = false;
            State.ChickenHuntComplete = false;
            State.PostChickenCutsceneComplete = false;
            State.PlaceholderCutsceneVisited = false;
            State.FindToolsComplete = false;
            State.PreFarmCutsceneComplete = false;
            State.FarmTutorialStarted = false;
            State.FarmTutorialComplete = false;
        }

        public void EnterScene(string sceneName)
        {
            var step = TutorialSceneCatalog.GetStepForScene(sceneName);
            if (step == TutorialStep.None)
                return;

            State.CurrentStep = step;
            State.CurrentSceneName = sceneName;

            if (step == TutorialStep.MidpointPlaceholder)
                State.PlaceholderCutsceneVisited = true;

            if (step == TutorialStep.FarmTutorial)
                State.FarmTutorialStarted = true;
        }

        public string CompleteCurrentStep()
        {
            MarkCurrentStepComplete();

            var nextStep = TutorialSceneCatalog.GetNextStep(State.CurrentStep);
            if (nextStep == TutorialStep.None)
                return null;

            State.CurrentStep = nextStep;
            State.CurrentSceneName = TutorialSceneCatalog.GetSceneName(nextStep);
            return State.CurrentSceneName;
        }

        public string GetNextScene()
        {
            var nextStep = TutorialSceneCatalog.GetNextStep(State.CurrentStep);
            return TutorialSceneCatalog.GetSceneName(nextStep);
        }

        public string GetPreviousScene()
        {
            var previousStep = TutorialSceneCatalog.GetPreviousStep(State.CurrentStep);
            return TutorialSceneCatalog.GetSceneName(previousStep);
        }

        public string JumpToStep(TutorialStep step)
        {
            var sceneName = TutorialSceneCatalog.GetSceneName(step);
            State.CurrentStep = step;
            State.CurrentSceneName = sceneName;
            return sceneName;
        }

        public TutorialFlowSnapshot CreateSnapshot()
        {
            return new TutorialFlowSnapshot(
                State.CurrentStep,
                State.CurrentSceneName,
                State.IntroComplete,
                State.ChickenHuntComplete,
                State.PostChickenCutsceneComplete,
                State.PlaceholderCutsceneVisited,
                State.FindToolsComplete,
                State.PreFarmCutsceneComplete,
                State.FarmTutorialStarted,
                State.FarmTutorialComplete);
        }

        public void Restore(TutorialFlowSnapshot snapshot)
        {
            State.CurrentStep = snapshot.CurrentStep;
            State.CurrentSceneName = snapshot.CurrentSceneName;
            State.IntroComplete = snapshot.IntroComplete;
            State.ChickenHuntComplete = snapshot.ChickenHuntComplete;
            State.PostChickenCutsceneComplete = snapshot.PostChickenCutsceneComplete;
            State.PlaceholderCutsceneVisited = snapshot.PlaceholderCutsceneVisited;
            State.FindToolsComplete = snapshot.FindToolsComplete;
            State.PreFarmCutsceneComplete = snapshot.PreFarmCutsceneComplete;
            State.FarmTutorialStarted = snapshot.FarmTutorialStarted;
            State.FarmTutorialComplete = snapshot.FarmTutorialComplete;
        }

        private void MarkCurrentStepComplete()
        {
            switch (State.CurrentStep)
            {
                case TutorialStep.Intro:
                    State.IntroComplete = true;
                    break;
                case TutorialStep.ChickenHunt:
                    State.ChickenHuntComplete = true;
                    break;
                case TutorialStep.PostChickenCutscene:
                    State.PostChickenCutsceneComplete = true;
                    break;
                case TutorialStep.MidpointPlaceholder:
                    State.PlaceholderCutsceneVisited = true;
                    break;
                case TutorialStep.FindTools:
                    State.FindToolsComplete = true;
                    break;
                case TutorialStep.PreFarmCutscene:
                    State.PreFarmCutsceneComplete = true;
                    break;
                case TutorialStep.FarmTutorial:
                    State.FarmTutorialComplete = true;
                    break;
            }
        }
    }
}
