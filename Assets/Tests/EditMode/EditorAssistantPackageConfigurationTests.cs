using System.IO;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    public sealed class EditorAssistantPackageConfigurationTests
    {
        [Test]
        public void EmbeddedBeziSidekickPackage_IsPresent()
        {
            Assert.That(Directory.Exists("Packages/com.bezi.sidekick"), Is.True,
                "The Bezi sidekick package should remain available when Bezi is the chosen Unity assistant.");
        }

        [Test]
        public void PackagesLock_DoesNotContainUnityAssistant()
        {
            var packagesLock = File.ReadAllText("Packages/packages-lock.json");

            Assert.That(packagesLock.Contains("\"com.unity.ai.assistant\""), Is.False,
                "packages-lock.json should not track Unity's assistant package when Bezi is the active editor assistant.");
        }

        [Test]
        public void Manifest_DoesNotContainUnityAssistant()
        {
            var manifest = File.ReadAllText("Packages/manifest.json");

            Assert.That(manifest.Contains("\"com.unity.ai.assistant\""), Is.False,
                "manifest.json should not declare Unity's assistant package when Bezi is installed.");
        }
    }
}
