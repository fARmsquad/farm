using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmSimVR.MonoBehaviours.Mailbox
{
    /// <summary>
    /// HUD badge that shows the unread mail count.
    /// Hides itself when there is no unread mail and the panel is closed.
    /// Click opens/closes the MailboxPanelController.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MailboxNotificationBadge : MonoBehaviour
    {
        [SerializeField] private TMP_Text              countLabel;
        [SerializeField] private MailboxPanelController panel;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy() => _button.onClick.RemoveListener(OnClick);

        private void Update()
        {
            var service = MailGeneratorDriver.MailboxService;
            int unread  = service?.UnreadCount ?? 0;

            // Always visible so the player can always open their mailbox
            gameObject.SetActive(true);

            if (countLabel != null)
                countLabel.text = unread > 0 ? unread.ToString() : string.Empty;

            if (Keyboard.current != null && Keyboard.current[Key.M].wasPressedThisFrame)
                panel?.Toggle();
        }

        private void OnClick() => panel?.Toggle();
    }
}
