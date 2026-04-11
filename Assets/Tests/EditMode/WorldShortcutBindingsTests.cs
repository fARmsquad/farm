using FarmSimVR.MonoBehaviours.Farming;
using FarmSimVR.MonoBehaviours.Hunting;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class WorldShortcutBindingsTests
    {
        [Test]
        public void FarmingWeatherShortcutSummary_UsesShiftModifiedBindings()
        {
            Assert.That(FarmWeatherDebugShortcuts.ShortcutSummary, Is.EqualTo(
                "Shift+Y Sun  Shift+U Cloud  Shift+I Rain  Shift+O Auto"));
        }

        [Test]
        public void WorldFarmSaveLoadShortcuts_UseShiftModifiedBindings()
        {
            Assert.That(WorldFarmDevShortcuts.SaveShortcutLabel, Is.EqualTo("Shift+P"));
            Assert.That(WorldFarmDevShortcuts.LoadShortcutLabel, Is.EqualTo("Shift+L"));
        }

        [Test]
        public void WorldPenShortcuts_UseShiftModifiedBindings()
        {
            Assert.That(WorldPenDevShortcuts.ExperienceShortcutLabel, Is.EqualTo("Shift+J"));
            Assert.That(WorldPenDevShortcuts.SkillShortcutLabel, Is.EqualTo("Shift+H"));
        }
    }
}
