using System.Collections.Generic;
using FarmSimVR.Core.Mailbox;
using FarmSimVR.MonoBehaviours.Farming;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmSimVR.MonoBehaviours.Mailbox
{
    /// <summary>
    /// Full inbox panel: list of messages on the left, detail view on the right.
    /// Marks messages as read when selected, and lets the player claim attachments
    /// which are sent directly to the inventory.
    /// </summary>
    public class MailboxPanelController : MonoBehaviour
    {
        [Header("Panel Root")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button     closeButton;

        [Header("Message List")]
        [SerializeField] private Transform  listContainer;
        [SerializeField] private GameObject messageRowPrefab;

        [Header("Detail View")]
        [SerializeField] private GameObject detailRoot;
        [SerializeField] private TMP_Text   detailSender;
        [SerializeField] private TMP_Text   detailSubject;
        [SerializeField] private TMP_Text   detailBody;
        [SerializeField] private Button     claimButton;
        [SerializeField] private TMP_Text   claimLabel;

        public bool IsOpen { get; private set; }

        private MailMessage               _selected;
        private int                       _selectedIndex = -1;
        private readonly List<GameObject> _rows = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            panelRoot?.SetActive(false);
            detailRoot?.SetActive(false);
            claimButton?.gameObject.SetActive(false);
            closeButton?.onClick.AddListener(Close);
            claimButton?.onClick.AddListener(OnClaim);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            var allMail = MailGeneratorDriver.MailboxService?.AllMail;
            if (allMail == null || allMail.Count == 0) return;

            if (kb[Key.DownArrow].wasPressedThisFrame)
                NavigateTo((_selectedIndex + 1) % allMail.Count);
            else if (kb[Key.UpArrow].wasPressedThisFrame)
                NavigateTo(((_selectedIndex - 1) + allMail.Count) % allMail.Count);
        }

        private void OnDestroy()
        {
            closeButton?.onClick.RemoveListener(Close);
            claimButton?.onClick.RemoveListener(OnClaim);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public void Open()
        {
            IsOpen = true;
            panelRoot?.SetActive(true);
            _selectedIndex = -1;
            RefreshList();
            FindAnyObjectByType<TownPlayerController>()?.SuspendControl();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            // Auto-select first mail so the player can start reading immediately
            var allMail = MailGeneratorDriver.MailboxService?.AllMail;
            if (allMail != null && allMail.Count > 0)
                NavigateTo(0);
        }

        public void Close()
        {
            IsOpen = false;
            _selected      = null;
            _selectedIndex = -1;
            panelRoot?.SetActive(false);
            detailRoot?.SetActive(false);
            FindAnyObjectByType<TownPlayerController>()?.ResumeControl();
        }

        // ── List ──────────────────────────────────────────────────────────────

        private void RefreshList()
        {
            foreach (var row in _rows) Destroy(row);
            _rows.Clear();

            var service = MailGeneratorDriver.MailboxService;
            if (service == null || listContainer == null || messageRowPrefab == null) return;

            foreach (var msg in service.AllMail)
            {
                var row = Instantiate(messageRowPrefab, listContainer);
                row.SetActive(true);
                _rows.Add(row);
                PopulateRow(row, msg);
            }

            UpdateRowHighlights();
        }

        private void NavigateTo(int index)
        {
            var allMail = MailGeneratorDriver.MailboxService?.AllMail;
            if (allMail == null || index < 0 || index >= allMail.Count) return;
            _selectedIndex = index;
            SelectMessage(allMail[index]);  // calls RefreshList → UpdateRowHighlights
        }

        private void UpdateRowHighlights()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                var img = _rows[i].GetComponent<Image>();
                if (img == null) continue;
                img.color = i == _selectedIndex
                    ? new Color(0.25f, 0.42f, 0.25f, 1f)   // selected: green tint
                    : new Color(0.15f, 0.13f, 0.11f, 1f);  // default dark
            }
        }

        private void PopulateRow(GameObject row, MailMessage msg)
        {
            // Hierarchy: row → Texts → [SubjectLabel, SenderLabel]
            var labels = row.GetComponentsInChildren<TMP_Text>(true);
            if (labels.Length >= 1)
            {
                labels[0].text      = msg.Subject;
                labels[0].fontStyle = msg.IsRead ? FontStyles.Normal : FontStyles.Bold;
            }
            if (labels.Length >= 2)
            {
                labels[1].text      = msg.Sender;
                labels[1].fontStyle = FontStyles.Normal;
            }

            // Unread dot lives directly on the row (not inside Texts group)
            var dot = row.transform.Find("UnreadDot");
            if (dot != null) dot.gameObject.SetActive(!msg.IsRead);

            var btn = row.GetComponent<Button>();
            if (btn != null)
            {
                var captured = msg;
                btn.onClick.AddListener(() => SelectMessage(captured));
            }
        }

        // ── Detail ────────────────────────────────────────────────────────────

        private void SelectMessage(MailMessage msg)
        {
            _selected = msg;
            MailGeneratorDriver.MailboxService?.MarkRead(msg.Id);
            RefreshList();

            detailRoot?.SetActive(true);
            if (detailSender  != null) detailSender.text  = $"From: {msg.Sender}";
            if (detailSubject != null) detailSubject.text = msg.Subject;
            if (detailBody    != null) detailBody.text    = msg.Body;

            RefreshClaimButton();
        }

        private void RefreshClaimButton()
        {
            bool hasUnclaimed = _selected?.Attachment != null && !_selected.Attachment.IsClaimed;
            if (claimButton != null) claimButton.gameObject.SetActive(hasUnclaimed);
            if (claimLabel  != null && hasUnclaimed)
                claimLabel.text = $"Claim: {_selected.Attachment.ItemId} x{_selected.Attachment.Quantity}";
        }

        private void OnClaim()
        {
            if (_selected?.Attachment == null || _selected.Attachment.IsClaimed) return;

            MailGeneratorDriver.MailboxService?.ClaimAttachment(_selected.Id);

            var driver = FindAnyObjectByType<FarmSimDriver>();
            driver?.Inventory.AddItem(_selected.Attachment.ItemId, _selected.Attachment.Quantity);

            RefreshClaimButton();
        }
    }
}
