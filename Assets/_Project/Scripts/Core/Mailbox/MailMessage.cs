using System;

namespace FarmSimVR.Core.Mailbox
{
    public class MailMessage
    {
        public string         Id         { get; }
        public string         Sender     { get; }
        public string         Subject    { get; }
        public string         Body       { get; }
        public MailType       Type       { get; }
        public MailAttachment Attachment { get; }
        public bool           IsRead     { get; private set; }

        public MailMessage(string sender, string subject, string body, MailType type, MailAttachment attachment = null)
        {
            Id         = Guid.NewGuid().ToString();
            Sender     = sender;
            Subject    = subject;
            Body       = body;
            Type       = type;
            Attachment = attachment;
        }

        public void MarkRead() => IsRead = true;
    }
}
