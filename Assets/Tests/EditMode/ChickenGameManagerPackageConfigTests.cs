using FarmSimVR.Core.Story;
using FarmSimVR.MonoBehaviours.ChickenGame;
using NUnit.Framework;
using UnityEngine;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class ChickenGameManagerPackageConfigTests
    {
        private GameObject _managerObject;
        private GameObject _chickenObject;

        [TearDown]
        public void TearDown()
        {
            if (_managerObject != null)
                Object.DestroyImmediate(_managerObject);

            if (_chickenObject != null)
                Object.DestroyImmediate(_chickenObject);
        }

        [Test]
        public void ApplyPackageConfig_UsesGeneratedTimerArenaAndObjective()
        {
            _managerObject = new GameObject("ChickenGameManager");
            var manager = _managerObject.AddComponent<ChickenGameManager>();
            manager.timeLimit = 45f;

            _chickenObject = new GameObject("Chicken");
            _chickenObject.AddComponent<CharacterController>();
            var chicken = _chickenObject.AddComponent<ChickenAI>();
            manager.chicken = chicken;

            manager.ApplyPackageConfig(new StoryMinigameConfigSnapshot
            {
                AdapterId = "tutorial.chicken_chase",
                ObjectiveText = "Catch 2 of 3 chickens before sunset.",
                RequiredCount = 2,
                TimeLimitSeconds = 150f,
                ResolvedParameterEntries = new[]
                {
                    IntParameter("targetCaptureCount", 2),
                    IntParameter("chickenCount", 3),
                    StringParameter("arenaPresetId", "tutorial_pen_medium"),
                    StringParameter("guidanceLevel", "medium"),
                },
            });

            Assert.That(manager.RequiredCaptureCount, Is.EqualTo(2));
            Assert.That(manager.ConfiguredChickenCount, Is.EqualTo(3));
            Assert.That(manager.GuidanceLevel, Is.EqualTo("medium"));
            Assert.That(manager.ArenaPresetId, Is.EqualTo("tutorial_pen_medium"));
            Assert.That(manager.CurrentObjectiveText, Does.StartWith("Catch 2 of 3 chickens before sunset."));
            Assert.That(manager.CurrentObjectiveText, Does.Contain("0/2 secured."));
            Assert.That(manager.timeLimit, Is.EqualTo(150f));
            Assert.That(manager.TimeRemaining, Is.EqualTo(150f));
            Assert.That(chicken.arenaRadius, Is.EqualTo(13f).Within(0.01f));
        }

        private static StoryMinigameParameterSnapshot IntParameter(string name, int value)
        {
            return new StoryMinigameParameterSnapshot
            {
                Name = name,
                ValueType = "Int",
                IntValue = value,
            };
        }

        private static StoryMinigameParameterSnapshot StringParameter(string name, string value)
        {
            return new StoryMinigameParameterSnapshot
            {
                Name = name,
                ValueType = "String",
                StringValue = value,
            };
        }
    }
}
