using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Renders the Town dialogue HUD and adapts it to streaming, voice-input, and choice states.
    /// </summary>
    public class DialogueChoiceUI : MonoBehaviour
    {
        private static readonly Color WaitingColor = new(0.76f, 0.87f, 1f, 0.92f);
        private static readonly Color RecordingColor = new(1f, 0.72f, 0.54f, 1f);
        private static readonly Color TranscribingColor = new(0.73f, 0.96f, 1f, 1f);
        private static readonly Color WarningColor = new(1f, 0.86f, 0.64f, 1f);

        [SerializeField] private LLMConversationController conversation;
        [SerializeField] private Canvas dialogueCanvas;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private TextMeshProUGUI hintText;

        private readonly List<GameObject> _buttons = new();
        private readonly StringBuilder _visibleText = new();
        private string _defaultHintText;
        private string _lastPlayerPrompt;
        private bool _choicesVisible;
        private bool _layoutInitialized;
        private TownVoiceInputController _voiceInputController;
        private RectTransform _dialoguePanelRect;
        private RectTransform _choiceContainerRect;

        private void OnEnable()
        {
            InitializeAdaptiveHud();
            if (conversation == null)
                return;

            ResolveVoiceInputController();
            conversation.OnStreamStarted += HandleStreamStarted;
            conversation.OnStreamChunk += HandleStreamChunk;
            conversation.OnNPCResponse += HandleResponse;
            conversation.OnOptionsReady += HandleOptions;
            conversation.OnWaiting += HandleWaiting;
            conversation.OnConversationEnded += HandleEnded;
            conversation.OnError += HandleError;
            conversation.OnPlayerPromptSubmitted += HandlePlayerPromptSubmitted;
            conversation.OnExitBlocked += HandleExitBlocked;
            SubscribeVoiceInputStatus();
        }

        private void OnDisable()
        {
            UnsubscribeVoiceInputStatus();
            if (conversation == null)
                return;

            conversation.OnStreamStarted -= HandleStreamStarted;
            conversation.OnStreamChunk -= HandleStreamChunk;
            conversation.OnNPCResponse -= HandleResponse;
            conversation.OnOptionsReady -= HandleOptions;
            conversation.OnWaiting -= HandleWaiting;
            conversation.OnConversationEnded -= HandleEnded;
            conversation.OnError -= HandleError;
            conversation.OnPlayerPromptSubmitted -= HandlePlayerPromptSubmitted;
            conversation.OnExitBlocked -= HandleExitBlocked;
        }

        private void Start()
        {
            InitializeAdaptiveHud();
            ResolveVoiceInputController();
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            _lastPlayerPrompt = null;
            _choicesVisible = false;
            ClearButtons();
            HideStatusBadge();
            RestoreDefaultHint();
        }

        private void HandleStreamStarted(string npcName)
        {
            InitializeAdaptiveHud();
            _visibleText.Clear();
            _choicesVisible = false;

            ShowCanvas(true);
            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            if (speakerNameText != null)
                speakerNameText.text = npcName;

            ClearButtons();
            HideStatusBadge();
            SetHintText(string.Empty);
            SetDialogueText(string.Empty);
            UnlockCursor();
        }

        private void HandleStreamChunk(string token)
        {
            _visibleText.Append(token);
            SetDialogueText(_visibleText.ToString());
        }

        private void HandleResponse(string npcName, string responseText)
        {
            if (speakerNameText != null)
                speakerNameText.text = npcName;

            SetDialogueText(responseText);
        }

        private void HandleOptions(string[] options)
        {
            InitializeAdaptiveHud();
            ResolveVoiceInputController();
            ClearButtons();
            BuildOptionButtons(options);
            _choicesVisible = true;
            RefreshVoiceInputStatus();
            RefreshAdaptiveLayout();
        }

        private void HandleWaiting()
        {
            InitializeAdaptiveHud();
            ShowCanvas(true);
            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            ClearButtons();
            _choicesVisible = false;
            ShowStatusBadge("Thinking...", WaitingColor);
            SetHintText(string.Empty);

            if (!string.IsNullOrWhiteSpace(_lastPlayerPrompt))
                SetDialogueText(string.Empty);
        }

        private void HandleEnded()
        {
            InitializeAdaptiveHud();
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            ShowCanvas(false);
            ClearButtons();
            _lastPlayerPrompt = null;
            _visibleText.Clear();
            _choicesVisible = false;
            HideStatusBadge();
            RestoreDefaultHint();
            LockCursor();
        }

        private void HandleError(string _)
        {
            InitializeAdaptiveHud();
            SetDialogueText("(Something went wrong — try again later.)");
            ClearButtons();
            _choicesVisible = false;
            ShowStatusBadge("Conversation unavailable.", WarningColor);
            SetHintText(string.Empty);
        }

        private void HandlePlayerPromptSubmitted(string prompt)
        {
            _lastPlayerPrompt = prompt;
        }

        private void HandleExitBlocked(string message)
        {
            InitializeAdaptiveHud();
            if (string.IsNullOrWhiteSpace(message))
                return;

            ShowStatusBadge(message, WarningColor);
            if (_choicesVisible)
                RefreshVoiceInputStatus();
        }

        private void ShowCanvas(bool visible)
        {
            if (dialogueCanvas != null)
                dialogueCanvas.enabled = visible;
        }

        private void SetDialogueText(string responseText)
        {
            InitializeAdaptiveHud();
            if (dialogueText == null)
                return;

            dialogueText.text = BuildDialogueBody(responseText);
            dialogueText.maxVisibleCharacters = int.MaxValue;
            RefreshAdaptiveLayout();
        }

        private string BuildDialogueBody(string responseText)
        {
            if (string.IsNullOrWhiteSpace(_lastPlayerPrompt))
                return responseText ?? string.Empty;

            if (string.IsNullOrWhiteSpace(responseText))
                return $"You: {_lastPlayerPrompt}";

            return $"You: {_lastPlayerPrompt}\n\n{responseText}";
        }

        private void BuildOptionButtons(string[] options)
        {
            if (options == null)
                return;

            foreach (string option in options)
            {
                string captured = option;
                GameObject button = BuildButton(option);
                button.GetComponent<Button>().onClick.AddListener(() => conversation.SelectOption(captured));
                _buttons.Add(button);
            }
        }

        private GameObject BuildButton(string label)
        {
            var go = new GameObject("ChoiceBtn");
            go.transform.SetParent(choiceContainer, false);
            TownDialogueHudLayout.ConfigureChoiceButton(go, label);
            return go;
        }

        private void ClearButtons()
        {
            foreach (GameObject button in _buttons)
                Destroy(button);

            _buttons.Clear();
        }

        private void InitializeAdaptiveHud()
        {
            bool hasResolvedLayout = _layoutInitialized
                && _dialoguePanelRect != null
                && _choiceContainerRect != null;
            if (hasResolvedLayout)
                return;

            ResolveHintText();
            _dialoguePanelRect = dialoguePanel != null ? dialoguePanel.GetComponent<RectTransform>() : null;
            _choiceContainerRect = choiceContainer as RectTransform;
            _defaultHintText = hintText != null ? hintText.text : string.Empty;

            TownDialogueHudLayout.ConfigureStatusText(loadingText);
            TownDialogueHudLayout.ConfigureHintText(hintText);
            TownDialogueHudLayout.ConfigureChoiceContainer(choiceContainer);
            _layoutInitialized = true;
            RefreshAdaptiveLayout();
        }

        private void ResolveHintText()
        {
            if (hintText != null || dialoguePanel == null)
                return;

            foreach (TextMeshProUGUI candidate in dialoguePanel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (candidate.name != "HintLabel")
                    continue;

                hintText = candidate;
                break;
            }
        }

        private void ResolveVoiceInputController()
        {
            if (_voiceInputController != null || conversation == null)
                return;

            _voiceInputController = conversation.GetComponent<TownVoiceInputController>();
            SubscribeVoiceInputStatus();
        }

        private void SubscribeVoiceInputStatus()
        {
            if (_voiceInputController == null)
                return;

            _voiceInputController.OnStatusChanged -= HandleVoiceInputStatusChanged;
            _voiceInputController.OnStatusChanged += HandleVoiceInputStatusChanged;
        }

        private void UnsubscribeVoiceInputStatus()
        {
            if (_voiceInputController == null)
                return;

            _voiceInputController.OnStatusChanged -= HandleVoiceInputStatusChanged;
        }

        private void HandleVoiceInputStatusChanged(string status)
        {
            InitializeAdaptiveHud();
            if (!_choicesVisible)
                return;

            ApplyVoiceHudPresentation(_voiceInputController?.CurrentStatusPhase ?? TownVoiceInputStatusPhase.Hidden, status);
        }

        private void RefreshVoiceInputStatus()
        {
            InitializeAdaptiveHud();
            if (!_choicesVisible)
            {
                HideStatusBadge();
                return;
            }

            TownVoiceInputStatusPhase phase = _voiceInputController?.CurrentStatusPhase ?? TownVoiceInputStatusPhase.Hidden;
            string status = _voiceInputController?.CurrentStatus;
            ApplyVoiceHudPresentation(phase, status);
        }

        private void ApplyVoiceHudPresentation(TownVoiceInputStatusPhase phase, string status)
        {
            switch (phase)
            {
                case TownVoiceInputStatusPhase.Idle:
                    HideStatusBadge();
                    SetHintText("Hold V to speak or choose a reply");
                    break;

                case TownVoiceInputStatusPhase.Recording:
                    ShowStatusBadge(status, RecordingColor);
                    SetHintText("Release V to send");
                    break;

                case TownVoiceInputStatusPhase.Transcribing:
                    ShowStatusBadge(status, TranscribingColor);
                    SetHintText("Preparing your reply...");
                    break;

                case TownVoiceInputStatusPhase.Warning:
                    ShowStatusBadge(status, WarningColor);
                    SetHintText("Choose a reply below");
                    break;

                default:
                    HideStatusBadge();
                    SetHintText("Choose a reply below");
                    break;
            }
        }

        private void ShowStatusBadge(string message, Color color)
        {
            if (loadingText == null)
                return;

            if (string.IsNullOrWhiteSpace(message))
            {
                HideStatusBadge();
                return;
            }

            loadingText.gameObject.SetActive(true);
            loadingText.color = color;
            loadingText.text = message;
            RefreshAdaptiveLayout();
        }

        private void HideStatusBadge()
        {
            if (loadingText == null)
                return;

            loadingText.text = string.Empty;
            loadingText.gameObject.SetActive(false);
            RefreshAdaptiveLayout();
        }

        private void SetHintText(string text)
        {
            if (hintText == null)
                return;

            hintText.text = text ?? string.Empty;
            RefreshAdaptiveLayout();
        }

        private void RestoreDefaultHint()
        {
            SetHintText(_defaultHintText);
        }

        private void RefreshAdaptiveLayout()
        {
            if (!_layoutInitialized)
                return;

            TownDialogueHudLayout.RefreshLayout(
                _dialoguePanelRect,
                _choiceContainerRect,
                speakerNameText,
                dialogueText,
                loadingText,
                hintText);
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
