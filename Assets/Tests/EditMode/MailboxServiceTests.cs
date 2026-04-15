using NUnit.Framework;
using FarmSimVR.Core.Mailbox;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class MailboxServiceTests
    {
        private MailboxService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new MailboxService();
        }

        [Test]
        public void AddMail_WithNewMessage_IncreasesUnreadCount()
        {
            var msg = new MailMessage("Farmer Joe", "Hello!", "Hope your crops are well.", MailType.Real);

            _service.AddMail(msg);

            Assert.AreEqual(1, _service.UnreadCount);
        }

        [Test]
        public void MarkRead_WithUnreadMessage_DecrementsUnreadCount()
        {
            var msg = new MailMessage("Farmer Joe", "Hello!", "Hope your crops are well.", MailType.Real);
            _service.AddMail(msg);

            _service.MarkRead(msg.Id);

            Assert.AreEqual(0, _service.UnreadCount);
        }

        [Test]
        public void AddMail_WithAttachment_AttachmentIsUnclaimedByDefault()
        {
            var attachment = new MailAttachment("seed_carrot", quantity: 3);
            var msg = new MailMessage("Travelling Merchant", "A gift!", "Found these on my travels.", MailType.Real, attachment);

            _service.AddMail(msg);

            Assert.IsFalse(msg.Attachment.IsClaimed);
        }

        [Test]
        public void ClaimAttachment_SetsIsClaimedTrue()
        {
            var attachment = new MailAttachment("seed_carrot", quantity: 3);
            var msg = new MailMessage("Travelling Merchant", "A gift!", "Found these on my travels.", MailType.Real, attachment);
            _service.AddMail(msg);

            _service.ClaimAttachment(msg.Id);

            Assert.IsTrue(msg.Attachment.IsClaimed);
        }

        [Test]
        public void AddMail_MultipleMessages_AllMessagesStored()
        {
            _service.AddMail(new MailMessage("Joe", "Hi", "Body", MailType.Real));
            _service.AddMail(new MailMessage("Ads Inc", "BUY SEEDS", "THE BEST SEEDS", MailType.Junk));
            _service.AddMail(new MailMessage("Mayor", "Town news", "All is well.", MailType.Real));

            Assert.AreEqual(3, _service.AllMail.Count);
        }

        [Test]
        public void AddMail_WithDuplicateContent_DoesNotStoreDuplicate()
        {
            _service.AddMail(new MailMessage("A Friendly Stranger", "Hello", "Same body.", MailType.Real));
            _service.AddMail(new MailMessage("A Friendly Stranger", "Hello", "Same body.", MailType.Real));

            Assert.AreEqual(1, _service.AllMail.Count);
        }
    }
}
