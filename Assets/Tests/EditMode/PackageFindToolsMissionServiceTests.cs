using System.Linq;
using FarmSimVR.Core.Tutorial;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class PackageFindToolsMissionServiceTests
    {
        [Test]
        public void Configure_StarterToolSet_ResolvesRequestedToolCount()
        {
            var service = new PackageFindToolsMissionService();
            service.Configure(
                objectiveText: "Find 2 starter tools around the yard in 4 minutes.",
                targetToolSet: "starter",
                requiredCount: 2,
                searchZone: "yard",
                hintStrength: "strong",
                timeLimitSeconds: 240f);

            Assert.That(service.RequiredCount, Is.EqualTo(2));
            Assert.That(service.SearchZone, Is.EqualTo("yard"));
            Assert.That(service.HintStrength, Is.EqualTo("strong"));
            Assert.That(service.ToolDisplayNames, Has.Length.EqualTo(2));
            Assert.That(service.ToolDisplayNames.SequenceEqual(new[] { "Hoe", "Watering Can" }), Is.True);
        }

        [Test]
        public void Observe_ReachingRequiredCount_CompletesMission()
        {
            var service = new PackageFindToolsMissionService();
            service.Configure(
                objectiveText: "Find 2 starter tools around the yard in 4 minutes.",
                targetToolSet: "starter",
                requiredCount: 2,
                searchZone: "yard",
                hintStrength: "strong",
                timeLimitSeconds: 240f);

            service.Observe(collectedCount: 1, deltaTime: 10f);
            Assert.That(service.IsComplete, Is.False);
            Assert.That(service.CurrentObjective, Does.Contain("1/2"));

            service.Observe(collectedCount: 2, deltaTime: 10f);
            Assert.That(service.IsComplete, Is.True);
            Assert.That(service.CurrentObjective, Is.EqualTo("Tools recovered."));
        }
    }
}
