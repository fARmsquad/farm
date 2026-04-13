using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Renders the LLM conversation UI with streaming support.
    /// Shows the NPC's response text token-by-token as it streams in,
    /// then displays clickable choice buttons once the stream completes.
    /// </summary>
    public class DialogueChoiceUI : MonoBehaviour
    {
        private const string RESPONSE_KEY = "\"response\":\"";

        [SerializeField] private LLMConversationController conversation;
        [SerializeField] private Canvas                    dialogueCanvas;
        [SerializeField] private GameObject                dialoguePanel;
        [SerializeField] private TextMeshProUGUI           speakerNameText;
        [SerializeField] private TextMeshProUGUI           dialogueText;
        [SerializeField] private Transform                 choiceContainer;
        [SerializeField] private TextMeshProUGUI           loadingText;

        private readonly List<GameObject> _buttons = new();
        private readonly StringBuilder _rawStream = new();
        private readonly StringBuilder _visibleText = new();
        private bool _insideResponseValue;
        private bool _isStreaming;

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
            ClearButtons();
        }

        // ── Streaming handlers ───────────────────────────────────────────────

        private void HandleStreamStarted(string npcName)
        {
            _rawStream.Clear();
            _visibleText.Clear();
            _insideResponseValue = false;
            _isStreaming = true;

            ShowCanvas(true);
            if (dialoguePanel   != null) dialoguePanel.SetActive(true);
            if (loadingText     != null) loadingText.gameObject.SetActive(false);
            if (speakerNameText != null) speakerNameText.text = npcName;
            if (dialogueText    != null)
            {
                dialogueText.text = "";
                dialogueText.maxVisibleCharacters = int.MaxValue;
            }

            ClearButtons();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        private void HandleStreamChunk(string token)
        {
            _rawStream.Append(token);

            // Extract visible text from the "response" JSON value as it streams
            string raw = _rawStream.ToString();

            if (!_insideResponseValue)
            {
                int keyIdx = raw.IndexOf(RESPONSE_KEY);
                if (keyIdx >= 0)
                {
                    _insideResponseValue = true;
                    // Extract everything after the key so far
                    string afterKey = raw.Substring(keyIdx + RESPONSE_KEY.Length);
                    string extracted = ExtractUntilClose(afterKey);
                    _visibleText.Clear();
                    _visibleText.Append(extracted);
                }
            }
            else
            {
                // We're already inside the response value — re-extract from the key
                int keyIdx = raw.IndexOf(RESPONSE_KEY);
                if (keyIdx >= 0)
                {
                    string afterKey = raw.Substring(keyIdx + RESPONSE_KEY.Length);
                    string extracted = ExtractUntilClose(afterKey);
                    _visibleText.Clear();
                    _visibleText.Append(extracted);
                }
            }

            if (dialogueText != null)
                dialogueText.text = _visibleText.ToString();
        }

        // ── Completion handlers ──────────────────────────────────────────────

        private void HandleResponse(string npcName, string responseText)
        {
            _isStreaming = false;

            // Show the final clean response text (parsed from complete JSON)
            if (speakerNameText != null) speakerNameText.text = npcName;
            if (dialogueText != null)
            {
                dialogueText.text = responseText;
                dialogueText.maxVisibleCharacters = int.MaxValue;
            }
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
            ShowCanvas(true);
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            ClearButtons();
            if (loadingText != null)
            {
                loadingText.gameObject.SetActive(true);
                loadingText.text = "...";
            }
        }

        private void HandleEnded()
        {
            _isStreaming = false;
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            ShowCanvas(false);
            ClearButtons();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void HandleError(string _)
        {
            _isStreaming = false;
            if (dialogueText != null)
                dialogueText.text = "(Something went wrong — try again later.)";
            ClearButtons();
        }

        // ── JSON response value extraction ───────────────────────────────────

        /// <summary>
        /// Extracts text from the start of a string until an unescaped closing quote.
        /// Handles escaped characters so \" doesn't terminate early.
        /// If no closing quote is found, returns everything (stream still in progress).
        /// </summary>
        private static string ExtractUntilClose(string s)
        {
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char next = s[i + 1];
                    switch (next)
                    {
                        case '"':  sb.Append('"');  i++; break;
                        case '\\': sb.Append('\\'); i++; break;
                        case 'n':  sb.Append('\n'); i++; break;
                        case 'r':  sb.Append('\r'); i++; break;
                        case 't':  sb.Append('\t'); i++; break;
                        default:   sb.Append(c);         break;
                    }
                }
                else if (c == '"')
                {
                    break; // End of value
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        // ── Canvas visibility ────────────────────────────────────────────────

        private void ShowCanvas(bool visible)
        {
            if (dialogueCanvas != null)
                dialogueCanvas.enabled = visible;
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
