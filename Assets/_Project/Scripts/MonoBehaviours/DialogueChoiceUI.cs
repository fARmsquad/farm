using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Renders the LLM conversation UI:
    /// – NPC response text in the existing dialogue panel
    /// – Dynamically created choice bubbles the player can click
    /// </summary>
    public class DialogueChoiceUI : MonoBehaviour
    {
        [SerializeField] private LLMConversationController conversation;
        [SerializeField] private GameObject                dialoguePanel;
        [SerializeField] private TextMeshProUGUI           speakerNameText;
        [SerializeField] private TextMeshProUGUI           dialogueText;
        [SerializeField] private Transform                 choiceContainer;
        [SerializeField] private TextMeshProUGUI           loadingText;

        private readonly List<GameObject> _buttons = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (conversation == null) return;
            conversation.OnNPCResponse      += HandleResponse;
            conversation.OnOptionsReady     += HandleOptions;
            conversation.OnWaiting          += HandleWaiting;
            conversation.OnConversationEnded += HandleEnded;
            conversation.OnError            += HandleError;
        }

        private void OnDisable()
        {
            if (conversation == null) return;
            conversation.OnNPCResponse      -= HandleResponse;
            conversation.OnOptionsReady     -= HandleOptions;
            conversation.OnWaiting          -= HandleWaiting;
            conversation.OnConversationEnded -= HandleEnded;
            conversation.OnError            -= HandleError;
        }

        private void Start()
        {
            // Initial visibility is owned by DialogueManager.Awake() which calls Hide().
            // DialogueChoiceUI only takes over panel visibility during LLM conversations.
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void HandleResponse(string npcName, string text)
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            if (loadingText   != null) loadingText.gameObject.SetActive(false);
            if (speakerNameText != null) speakerNameText.text = npcName;
            if (dialogueText    != null) dialogueText.text    = text;
        }

        private void HandleOptions(string[] options)
        {
            ClearButtons();
            foreach (string opt in options)
            {
                var captured = opt;
                var btn = BuildButton(opt);
                btn.GetComponent<Button>().onClick.AddListener(
                    () => conversation.SelectOption(captured));
                _buttons.Add(btn);
            }
        }

        private void HandleWaiting()
        {
            ClearButtons();
            if (loadingText != null)
            {
                loadingText.gameObject.SetActive(true);
                loadingText.text = "...";
            }
        }

        private void HandleEnded()
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            ClearButtons();
        }

        private void HandleError(string _)
        {
            if (dialogueText != null)
                dialogueText.text = "(Something went wrong — try again later.)";
            ClearButtons();
        }

        // ── Button factory ────────────────────────────────────────────────────

        private GameObject BuildButton(string label)
        {
            var go   = new GameObject("ChoiceBtn");
            go.transform.SetParent(choiceContainer, false);

            var rect       = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 48f);

            var img   = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.16f, 0.26f, 0.92f);

            var btn    = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.18f, 0.28f, 0.45f, 1f);
            colors.pressedColor     = new Color(0.08f, 0.12f, 0.20f, 1f);
            btn.colors = colors;

            var textGo   = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);

            var textRect      = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20f,  4f);
            textRect.offsetMax = new Vector2(-20f, -4f);

            var tmp       = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 18f;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;

            return go;
        }

        private void ClearButtons()
        {
            foreach (var btn in _buttons) Destroy(btn);
            _buttons.Clear();
        }
    }
}
