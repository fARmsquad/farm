using FarmSimVR.Core.Tutorial;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class TutorialFlowServiceTests
    {
        [Test]
        public void SceneOrder_MatchesLinearTutorialSpec()
        {
            CollectionAssert.AreEqual(
                new[]
                {
                    TutorialSceneCatalog.IntroSceneName,
                    TutorialSceneCatalog.ChickenGameSceneName,
                    TutorialSceneCatalog.PostChickenCutsceneSceneName,
                    TutorialSceneCatalog.CoreSceneSceneName,
                    TutorialSceneCatalog.FindToolsSceneName,
                    TutorialSceneCatalog.PreFarmCutsceneSceneName,
                    TutorialSceneCatalog.FarmTutorialSceneName,
                },
                TutorialSceneCatalog.SceneOrder);
        }

        [Test]
        public void CompleteCurrentStep_AdvancesThroughFullTutorialAndMarksFlags()
        {
            var service = new TutorialFlowService();

            service.EnterScene(TutorialSceneCatalog.IntroSceneName);
            Assert.That(service.CompleteCurrentStep(), Is.EqualTo(TutorialSceneCatalog.ChickenGameSceneName));
            Assert.That(service.State.IntroComplete, Is.True);

            service.EnterScene(TutorialSceneCatalog.ChickenGameSceneName);
            Assert.That(service.CompleteCurrentStep(), Is.EqualTo(TutorialSceneCatalog.PostChickenCutsceneSceneName));
            Assert.That(service.State.ChickenHuntComplete, Is.True);

            service.EnterScene(TutorialSceneCatalog.PostChickenCutsceneSceneName);
            Assert.That(service.CompleteCurrentStep(), Is.EqualTo(TutorialSceneCatalog.CoreSceneSceneName));
            Assert.That(service.State.PostChickenCutsceneComplete, Is.True);

            service.EnterScene(TutorialSceneCatalog.CoreSceneSceneName);
            Assert.That(service.State.PlaceholderCutsceneVisited, Is.True);
            Assert.That(service.CompleteCurrentStep(), Is.EqualTo(TutorialSceneCatalog.FindToolsSceneName));

            service.EnterScene(TutorialSceneCatalog.FindToolsSceneName);
            Assert.That(service.CompleteCurrentStep(), Is.EqualTo(TutorialSceneCatalog.PreFarmCutsceneSceneName));
            Assert.That(service.State.FindToolsComplete, Is.True);

            service.EnterScene(TutorialSceneCatalog.PreFarmCutsceneSceneName);
            Assert.That(service.CompleteCurrentStep(), Is.EqualTo(TutorialSceneCatalog.FarmTutorialSceneName));
            Assert.That(service.State.PreFarmCutsceneComplete, Is.True);

            service.EnterScene(TutorialSceneCatalog.FarmTutorialSceneName);
            Assert.That(service.CompleteCurrentStep(), Is.Null);
            Assert.That(service.State.FarmTutorialComplete, Is.True);
            Assert.That(service.State.IsTutorialComplete, Is.True);
        }

        [Test]
        public void GetPreviousScene_ForInteriorStep_ReturnsPreviousTutorialScene()
        {
            var service = new TutorialFlowService();

            service.EnterScene(TutorialSceneCatalog.FindToolsSceneName);

            Assert.That(
                service.GetPreviousScene(),
                Is.EqualTo(TutorialSceneCatalog.CoreSceneSceneName));
        }

        [Test]
        public void CatalogLookupMethods_ResolveStepSceneAndNeighbors()
        {
            Assert.That(
                TutorialSceneCatalog.GetSceneName(TutorialStep.ChickenHunt),
                Is.EqualTo(TutorialSceneCatalog.ChickenGameSceneName));
            Assert.That(
                TutorialSceneCatalog.GetStepForScene(TutorialSceneCatalog.FindToolsSceneName),
                Is.EqualTo(TutorialStep.FindTools));
            Assert.That(
                TutorialSceneCatalog.GetNextStep(TutorialStep.PostChickenCutscene),
                Is.EqualTo(TutorialStep.MidpointPlaceholder));
            Assert.That(
                TutorialSceneCatalog.GetPreviousStep(TutorialStep.PreFarmCutscene),
                Is.EqualTo(TutorialStep.FindTools));
        }

        [Test]
        public void GetNextScene_AfterEnteringScene_ReturnsExpectedNextTarget()
        {
            var service = new TutorialFlowService();

            service.EnterScene(TutorialSceneCatalog.ChickenGameSceneName);

            Assert.That(
                service.GetNextScene(),
                Is.EqualTo(TutorialSceneCatalog.PostChickenCutsceneSceneName));
        }

        [Test]
        public void JumpToStep_UpdatesCurrentStepAndReturnsSceneName()
        {
            var service = new TutorialFlowService();

            var sceneName = service.JumpToStep(TutorialStep.PreFarmCutscene);

            Assert.That(sceneName, Is.EqualTo(TutorialSceneCatalog.PreFarmCutsceneSceneName));
            Assert.That(service.State.CurrentStep, Is.EqualTo(TutorialStep.PreFarmCutscene));
            Assert.That(service.State.CurrentSceneName, Is.EqualTo(TutorialSceneCatalog.PreFarmCutsceneSceneName));
        }

        [Test]
        public void CreateSnapshotAndRestore_RoundTripsTutorialProgress()
        {
            var source = new TutorialFlowService();
            source.EnterScene(TutorialSceneCatalog.IntroSceneName);
            source.CompleteCurrentStep();
            source.EnterScene(TutorialSceneCatalog.ChickenGameSceneName);
            source.CompleteCurrentStep();
            source.JumpToStep(TutorialStep.FindTools);

            var snapshot = source.CreateSnapshot();
            var restored = new TutorialFlowService();

            restored.Restore(snapshot);

            Assert.That(restored.State.CurrentStep, Is.EqualTo(TutorialStep.FindTools));
            Assert.That(restored.State.CurrentSceneName, Is.EqualTo(TutorialSceneCatalog.FindToolsSceneName));
            Assert.That(restored.State.IntroComplete, Is.True);
            Assert.That(restored.State.ChickenHuntComplete, Is.True);
            Assert.That(restored.State.PostChickenCutsceneComplete, Is.False);
        }

        [Test]
        public void Reset_ReturnsStateToIntroDefaults()
        {
            var service = new TutorialFlowService();
            service.EnterScene(TutorialSceneCatalog.FarmTutorialSceneName);
            service.CompleteCurrentStep();

            service.Reset();

            Assert.That(service.State.CurrentStep, Is.EqualTo(TutorialStep.Intro));
            Assert.That(service.State.CurrentSceneName, Is.EqualTo(TutorialSceneCatalog.IntroSceneName));
            Assert.That(service.State.FarmTutorialComplete, Is.False);
        }
    }
}
