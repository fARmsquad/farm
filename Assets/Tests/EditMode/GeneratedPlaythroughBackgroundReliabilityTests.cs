using System.IO;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class GeneratedPlaythroughBackgroundReliabilityTests
    {
        [Test]
        public void TitleScreenManager_Start_EnablesRunInBackground_FromSource()
        {
            var source = File.ReadAllText("Assets/_Project/Scripts/MonoBehaviours/TitleScreenManager.cs");

            Assert.That(source, Does.Contain("Application.runInBackground = true;"));
        }

        [Test]
        public void ProjectSettings_EnableRunInBackground_ForGeneratedPlaythroughReliability()
        {
            var projectSettings = File.ReadAllText("ProjectSettings/ProjectSettings.asset");

            Assert.That(projectSettings, Does.Contain("runInBackground: 1"));
        }

        [Test]
        public void GenerativePlaythroughController_DefaultsToProductionOrchestratorBaseUrl_FromSource()
        {
            var source = File.ReadAllText("Assets/_Project/Scripts/MonoBehaviours/Cinematics/GenerativePlaythroughController.cs");

            Assert.That(source, Does.Contain("TownVoiceTokenServiceEndpointResolver.ProductionBaseUrl"));
        }
    }
}
