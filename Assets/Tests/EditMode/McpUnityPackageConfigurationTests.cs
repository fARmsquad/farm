using System.IO;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    public sealed class McpUnityPackageConfigurationTests
    {
        [Test]
        public void EmbeddedMcpUnityPackage_IsPresent()
        {
            Assert.That(Directory.Exists("Packages/com.gamelovers.mcp-unity"), Is.True,
                "The MCP Unity package should be embedded locally so package metadata warnings can be fixed in-repo.");
        }

        [Test]
        public void EmbeddedMcpUnityPackage_TestScriptMeta_IsPresent()
        {
            Assert.That(File.Exists("Packages/com.gamelovers.mcp-unity/Editor/Tests/GetGameObjectResourceTests.cs.meta"), Is.True,
                "GetGameObjectResourceTests.cs should keep its .meta file when the MCP Unity package is embedded.");
        }
    }
}
