using System;
using System.Collections.Generic;

namespace FarmSimVR.Core.Mailbox
{
    public class MailboxService
    {
        private readonly List<MailMessage> _mail = new();
        private readonly HashSet<string>   _contentKeys = new();

        public IReadOnlyList<MailMessage> AllMail => _mail;

        public int UnreadCount
        {
            get
            {
                int count = 0;
                foreach (var m in _mail)
                    if (!m.IsRead) count++;
                return count;
            }
        }

        public event Action OnMailAdded;

        public void AddMail(MailMessage message)
        {
            TryAddMail(message);
        }

        public bool TryAddMail(MailMessage message)
        {
            if (message == null) return false;

            string key = MakeContentKey(message);
            if (!_contentKeys.Add(key)) return false;

            _mail.Add(message);
            OnMailAdded?.Invoke();
            return true;
        }

        public void MarkRead(string id)
        {
            var msg = Find(id);
            msg?.MarkRead();
        }

        public void ClaimAttachment(string id)
        {
            var msg = Find(id);
            msg?.Attachment?.Claim();
        }

        private MailMessage Find(string id)
        {
            foreach (var m in _mail)
                if (m.Id == id) return m;
            return null;
        }

        private static string MakeContentKey(MailMessage m)
        {
            // Normalize lightly to avoid duplicates that only differ by whitespace/case.
            return $"{Normalize(m.Sender)}\n{Normalize(m.Subject)}\n{Normalize(m.Body)}\n{m.Type}";
        }

        private static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            return s.Trim().ToLowerInvariant();
        }
    }
}
