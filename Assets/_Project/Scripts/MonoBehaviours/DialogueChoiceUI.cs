using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Renders the LLM conversation UI with streaming support.
    /// Shows the NPC's response text delta-by-delta as it streams in,
    /// then displays clickable choice buttons once the stream completes.
    /// </summary>
    public class DialogueChoiceUI : MonoBehaviour
    {
        [SerializeField] private LLMConversationController conversation;
        [SerializeField] private Canvas                    dialogueCanvas;
        [SerializeField] private GameObject                dialoguePanel;
        [SerializeField] private TextMeshProUGUI           speakerNameText;
        [SerializeField] private TextMeshProUGUI           dialogueText;
        [SerializeField] private Transform                 choiceContainer;
        [SerializeField] private TextMeshProUGUI           loadingText;

        private readonly List<GameObject> _buttons = new();
        private readonly StringBuilder _visibleText = new();
        private string _lastPlayerPrompt;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (conversation == null) return;
            conversation.OnStreamStarted     += HandleStreamStarted;
            conversation.OnStreamChunk       += HandleStreamChunk;
            conversation.OnNPCResponse       += HandleResponse;
            conversation.OnOptionsReady      += HandleOptions;
            conversation.OnWaiting           += HandleWaiting;
            conversation.OnConversationEnded += HandleEnded;
            conversation.OnError             += HandleError;
        }

        private void OnDisable()
        {
            if (conversation == null) return;
            conversation.OnStreamStarted     -= HandleStreamStarted;
            conversation.OnStreamChunk       -= HandleStreamChunk;
            conversation.OnNPCResponse       -= HandleResponse;
            conversation.OnOptionsReady      -= HandleOptions;
            conversation.OnWaiting           -= HandleWaiting;
            conversation.OnConversationEnded -= HandleEnded;
            conversation.OnError             -= HandleError;
        }

        private void Start()
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            _lastPlayerPrompt = null;
            ClearButtons();
        }

        // ── Streaming handlers ───────────────────────────────────────────────

        private void HandleStreamStarted(string npcName)
        {
            _visibleText.Clear();

            ShowCanvas(true);
            if (dialoguePanel   != null) dialoguePanel.SetActive(true);
            if (loadingText     != null) loadingText.gameObject.SetActive(false);
            if (speakerNameText != null) speakerNameText.text = npcName;
            if (dialogueText    != null)
            {
                dialogueText.text = BuildDialogueBody(string.Empty);
                dialogueText.maxVisibleCharacters = int.MaxValue;
            }

            ClearButtons();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        private void HandleStreamChunk(string token)
        {
            _visibleText.Append(token);
            SetDialogueText(_visibleText.ToString());
        }

        // ── Completion handlers ──────────────────────────────────────────────

        private void HandleResponse(string npcName, string responseText)
        {
            // Show the final clean response text once the streamed turn completes.
            if (speakerNameText != null) speakerNameText.text = npcName;
            SetDialogueText(responseText);
        }

        private void HandleOptions(string[] options)
        {
            ClearButtons();
            foreach (string opt in options)
            {
                var captured = opt;
                var btn = BuildButton(opt);
                btn.GetComponent<Button>().onClick.AddListener(
                    () =>
                    {
                        _lastPlayerPrompt = captured;
                        conversation.SelectOption(captured);
                    });
                _buttons.Add(btn);
            }
        }

        private void HandleWaiting()
        {
            ShowCanvas(true);
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            ClearButtons();
            if (loadingText != null)
            {
                loadingText.gameObject.SetActive(true);
                loadingText.text = "...";
            }

            if (!string.IsNullOrWhiteSpace(_lastPlayerPrompt))
                SetDialogueText(string.Empty);
        }

        private void HandleEnded()
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            ShowCanvas(false);
            ClearButtons();
            _lastPlayerPrompt = null;
            _visibleText.Clear();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void HandleError(string _)
        {
            SetDialogueText("(Something went wrong — try again later.)");
            ClearButtons();
        }

        // ── Canvas visibility ────────────────────────────────────────────────

        private void ShowCanvas(bool visible)
        {
            if (dialogueCanvas != null)
                dialogueCanvas.enabled = visible;
        }

        private void SetDialogueText(string responseText)
        {
            if (dialogueText == null)
                return;

            dialogueText.text = BuildDialogueBody(responseText);
            dialogueText.maxVisibleCharacters = int.MaxValue;
        }

        private string BuildDialogueBody(string responseText)
        {
            if (string.IsNullOrWhiteSpace(_lastPlayerPrompt))
                return responseText ?? string.Empty;

            if (string.IsNullOrWhiteSpace(responseText))
                return $"You: {_lastPlayerPrompt}";

            return $"You: {_lastPlayerPrompt}\n\n{responseText}";
        }

        // ── Button factory ────────────────────────────────────────────────────

        private GameObject BuildButton(string label)
        {
            var go = new GameObject("ChoiceBtn");
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

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);

            var textRect       = textGo.AddComponent<RectTransform>();
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
