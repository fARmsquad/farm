namespace FarmSimVR.Core.Mailbox
{
    public class MailAttachment
    {
        public string ItemId   { get; }
        public int    Quantity { get; }
        public bool   IsClaimed { get; private set; }

        public MailAttachment(string itemId, int quantity)
        {
            ItemId   = itemId;
            Quantity = quantity;
        }

        public void Claim() => IsClaimed = true;
    }
}
