using FarmSimVR.MonoBehaviours.Tutorial;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class TutorialDevShortcutsTests
    {
        [Test]
        public void ShortcutLabels_UseStableShiftBindings()
        {
            Assert.That(TutorialDevShortcuts.CompleteShortcutLabel, Is.EqualTo("Shift+Enter"));
            Assert.That(TutorialDevShortcuts.NextShortcutLabel, Is.EqualTo("Shift+."));
        }

        [Test]
        public void ShortcutSummary_ListsGlobalTutorialControls()
        {
            Assert.That(
                TutorialDevShortcuts.ShortcutSummary,
                Is.EqualTo("Shift+Enter Complete  Shift+. Next  Shift+, Back  Shift+/ Reload  Shift+1-7 Scene 01-07  Shift+0 Reset"));
        }
    }
}
