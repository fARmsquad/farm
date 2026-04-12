using FarmSimVR.Core.Farming;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class FarmStageMinigameSessionTests
    {
        [Test]
        public void StopZone_OnlyCompletesWhenMarkerIsInsideSuccessBand()
        {
            var session = new FarmStageMinigameSession(
                FarmStageMinigameDefinition.StopZone("Pat Soil", "Hit the green band.", speed: 1f, successMin: 0.45f, successMax: 0.55f));

            session.HandleInput(FarmStageMinigameInput.Confirm);
            Assert.IsFalse(session.IsComplete);

            session.Tick(0.5f);
            session.HandleInput(FarmStageMinigameInput.Confirm);

            Assert.IsTrue(session.IsComplete);
            Assert.AreEqual(1f, session.Progress, 0.001f);
        }

        [Test]
        public void RapidTap_RequiresEnoughInputsDespiteDecay()
        {
            var session = new FarmStageMinigameSession(
                FarmStageMinigameDefinition.RapidTap("Clear Weeds", "Mash confirm.", requiredCount: 4, decayPerSecond: 0.25f));

            session.HandleInput(FarmStageMinigameInput.Confirm);
            session.HandleInput(FarmStageMinigameInput.Confirm);
            session.Tick(1f);

            Assert.Less(session.Progress, 0.5f);
            Assert.IsFalse(session.IsComplete);

            session.HandleInput(FarmStageMinigameInput.Confirm);
            session.HandleInput(FarmStageMinigameInput.Confirm);
            session.HandleInput(FarmStageMinigameInput.Confirm);

            Assert.IsTrue(session.IsComplete);
            Assert.AreEqual(1f, session.Progress, 0.001f);
        }

        [Test]
        public void Sequence_WrongInputResetsAndCorrectSequenceCompletes()
        {
            var session = new FarmStageMinigameSession(
                FarmStageMinigameDefinition.Sequence(
                    "Tie Vine",
                    "Follow the arrows.",
                    FarmStageMinigameInput.Left,
                    FarmStageMinigameInput.Up,
                    FarmStageMinigameInput.Right));

            session.HandleInput(FarmStageMinigameInput.Left);
            session.HandleInput(FarmStageMinigameInput.Down);

            Assert.AreEqual(0, session.SequenceIndex);
            Assert.AreEqual(0f, session.Progress, 0.001f);

            session.HandleInput(FarmStageMinigameInput.Left);
            session.HandleInput(FarmStageMinigameInput.Up);
            session.HandleInput(FarmStageMinigameInput.Right);

            Assert.IsTrue(session.IsComplete);
            Assert.AreEqual(1f, session.Progress, 0.001f);
        }

        [Test]
        public void Sequence_DefinitionPreservesProvidedPattern()
        {
            var definition = FarmStageMinigameDefinition.Sequence(
                "Tie Vine",
                "Follow the arrows.",
                FarmStageMinigameInput.Left,
                FarmStageMinigameInput.Up,
                FarmStageMinigameInput.Right);

            CollectionAssert.AreEqual(
                new[]
                {
                    FarmStageMinigameInput.Left,
                    FarmStageMinigameInput.Up,
                    FarmStageMinigameInput.Right,
                },
                definition.InputSequence);
        }

        [Test]
        public void Alternate_RequiresAlternatingLeftAndRight()
        {
            var session = new FarmStageMinigameSession(
                FarmStageMinigameDefinition.Alternate(
                    "Brush Blossoms",
                    "Alternate strokes.",
                    requiredCount: 4,
                    firstInput: FarmStageMinigameInput.Left));

            session.HandleInput(FarmStageMinigameInput.Left);
            session.HandleInput(FarmStageMinigameInput.Left);

            Assert.AreEqual(FarmStageMinigameInput.Right, session.NextAlternateInput);
            Assert.Less(session.Progress, 0.5f);
            Assert.IsFalse(session.IsComplete);

            session.HandleInput(FarmStageMinigameInput.Right);
            session.HandleInput(FarmStageMinigameInput.Left);
            session.HandleInput(FarmStageMinigameInput.Right);

            Assert.IsTrue(session.IsComplete);
            Assert.AreEqual(1f, session.Progress, 0.001f);
        }
    }
}
