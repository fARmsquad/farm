using System.IO;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class LocalStoryOrchestratorLauncherTests
    {
        [Test]
        public void LauncherScript_LogsStartupAndLoadsLocalEnvFile_FromSource()
        {
            var source = File.ReadAllText("backend/story-orchestrator/start_local_backend.sh");

            Assert.That(source, Does.Contain("exec >>\"$LOG_PATH\" 2>&1"));
            Assert.That(source, Does.Contain(".env.local"));
            Assert.That(source, Does.Contain("OPENAI_API_KEY"));
            Assert.That(source, Does.Contain("GEMINI_API_KEY"));
            Assert.That(source, Does.Contain("python3"));
            Assert.That(source, Does.Contain("-m venv"));
            Assert.That(source, Does.Contain("pip install -r"));
        }

        [Test]
        public void LocalStoryOrchestratorLauncher_PointsBootstrapFailuresAtEnvLocal_InSource()
        {
            var source = File.ReadAllText("Assets/_Project/Scripts/MonoBehaviours/Cinematics/LocalStoryOrchestratorLauncher.cs");

            Assert.That(source, Does.Contain("backend/story-orchestrator/.env.local"));
            Assert.That(source, Does.Contain("Do not hardcode provider keys into Unity scenes or scripts."));
            Assert.That(source, Does.Contain("pip install -r requirements.txt"));
        }
    }
}
