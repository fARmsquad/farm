using FarmSimVR.Core;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    public sealed class TownKnowledgeGraphVendorPersonaTests
    {
        [Test]
        public void GetSystemPrompt_OldGarrett_MentionsLivestockSellerAndFourAnimalTypes()
        {
            string prompt = NPCPersonaCatalog.GetSystemPrompt("Old Garrett");

            Assert.That(prompt, Does.Contain("livestock").IgnoreCase);
            Assert.That(prompt, Does.Contain("cow").IgnoreCase);
            Assert.That(prompt, Does.Contain("pig").IgnoreCase);
            Assert.That(prompt, Does.Contain("sheep").IgnoreCase);
            Assert.That(prompt, Does.Contain("horse").IgnoreCase);
        }

        [Test]
        public void GetSystemPrompt_YoungPip_MentionsProduceGrocerAndStall()
        {
            string prompt = NPCPersonaCatalog.GetSystemPrompt("Young Pip");

            Assert.That(prompt, Does.Contain("fruit").IgnoreCase);
            Assert.That(prompt, Does.Contain("vegetable").IgnoreCase);
            Assert.That(prompt, Does.Contain("grocer").IgnoreCase);
            Assert.That(prompt, Does.Contain("stall").IgnoreCase);
        }

        [Test]
        public void GetSystemPrompt_MiraTheBaker_UnchangedBakerCore_NotLivestockVendor()
        {
            string prompt = NPCPersonaCatalog.GetSystemPrompt("Mira the Baker");

            Assert.That(prompt, Does.Contain("baker").IgnoreCase);
            Assert.That(prompt, Does.Not.Contain("cow"));
        }
    }
}
