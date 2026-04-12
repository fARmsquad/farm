using FarmSimVR.MonoBehaviours.Tutorial;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class TutorialDevShortcutsTests
    {
        [Test]
        public void ShortcutSummary_ListsGlobalTutorialControls()
        {
            Assert.That(
                TutorialDevShortcuts.ShortcutSummary,
                Is.EqualTo("Shift+. Next  Shift+, Back  Shift+/ Reload  Shift+1-7 Jump  Shift+0 Reset"));
        }
    }
}
