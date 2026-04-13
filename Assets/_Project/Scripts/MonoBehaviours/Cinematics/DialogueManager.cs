using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Manages dialogue playback with typewriter effect, input advance, and auto-advance.
    /// Singleton — access via DialogueManager.Instance.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private Canvas dialogueCanvas;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private Image panelBackground;

        [Header("Typewriter")]
        [SerializeField] private float charsPerSecond = 30f;

        [Header("Events")]
        public UnityEvent OnDialogueComplete;

        #endregion

        #region Internal State

        private DialogueData currentData;
        private Coroutine typewriterCoroutine;
        private Coroutine autoAdvanceCoroutine;
        private bool isTypewriterComplete;

        #endregion

        #region Public State

        /// <summary>
        /// True while a dialogue sequence is actively playing.
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// Index of the currently displayed dialogue line, or -1 if idle.
        /// </summary>
        public int CurrentLineIndex { get; private set; } = -1;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (!IsPlaying) return;
            if (dialogueCanvas == null || !dialogueCanvas.gameObject.activeSelf) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)
            {
                HandleInputAdvance();
            }
        }

        #endregion

        #region Show / Hide

        /// <summary>
        /// Shows the dialogue canvas by enabling the Canvas component.
        /// The GameObject stays active so sibling components (e.g. DialogueChoiceUI) keep running.
        /// </summary>
        public void Show()
        {
            if (dialogueCanvas != null)
                dialogueCanvas.enabled = true;
        }

        /// <summary>
        /// Hides the dialogue canvas by disabling the Canvas component.
        /// The GameObject stays active so sibling components (e.g. DialogueChoiceUI) keep running.
        /// </summary>
        public void Hide()
        {
            if (dialogueCanvas != null)
                dialogueCanvas.enabled = false;
        }

        #endregion

        #region Dialogue Playback

        /// <summary>
        /// Begins playback of the given dialogue data from line 0.
        /// If already playing, resets to the new data.
        /// </summary>
        public void StartDialogue(DialogueData data)
        {
            StopAllPlayback();

            currentData = data;

            if (data == null || data.lines == null || data.lines.Length == 0)
            {
                IsPlaying = false;
                CurrentLineIndex = -1;
                OnDialogueComplete?.Invoke();
                return;
            }

            IsPlaying = true;
            CurrentLineIndex = 0;
            Show();
            DisplayLine(CurrentLineIndex);
        }

        /// <summary>
        /// Advances to the next line, or completes the dialogue if all lines are exhausted.
        /// </summary>
        public void AdvanceToNextLine()
        {
            if (!IsPlaying || currentData == null) return;

            StopLineCoroutines();

            CurrentLineIndex++;

            if (CurrentLineIndex >= currentData.lines.Length)
            {
                CompleteDialogue();
                return;
            }

            DisplayLine(CurrentLineIndex);
        }

        #endregion

        #region Input Handling

        private void HandleInputAdvance()
        {
            if (!isTypewriterComplete)
            {
                // Typewriter is mid-line: complete it instantly
                CompleteTypewriterInstantly();
            }
            else
            {
                // Line is complete: advance to next line
                AdvanceToNextLine();
            }
        }

        private void CompleteTypewriterInstantly()
        {
            if (typewriterCoroutine != null)
                StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;

            if (dialogueText != null)
                dialogueText.maxVisibleCharacters = dialogueText.text.Length;

            isTypewriterComplete = true;

            // Start auto-advance if applicable
            if (currentData != null && CurrentLineIndex >= 0 && CurrentLineIndex < currentData.lines.Length)
            {
                DialogueLine line = currentData.lines[CurrentLineIndex];
                if (line.autoAdvance)
                {
                    autoAdvanceCoroutine = StartCoroutine(AutoAdvanceCoroutine(line.duration));
                }
            }
        }

        #endregion

        #region Line Display

        private void DisplayLine(int index)
        {
            StopLineCoroutines();

            DialogueLine line = currentData.lines[index];

            if (speakerNameText != null)
            {
                speakerNameText.text = line.speakerName;
                speakerNameText.color = line.speakerColor;
            }

            if (dialogueText != null)
            {
                dialogueText.text = line.text;
                dialogueText.maxVisibleCharacters = 0;
            }

            isTypewriterComplete = false;
            typewriterCoroutine = StartCoroutine(TypewriterCoroutine(line));
        }

        #endregion

        #region Typewriter Effect

        private IEnumerator TypewriterCoroutine(DialogueLine line)
        {
            if (dialogueText == null)
            {
                isTypewriterComplete = true;
                yield break;
            }

            int totalCharacters = dialogueText.text.Length;

            if (totalCharacters == 0)
            {
                isTypewriterComplete = true;
                yield break;
            }

            float charTimer = 0f;
            int visibleCount = 0;

            while (visibleCount < totalCharacters)
            {
                charTimer += Time.unscaledDeltaTime * charsPerSecond;
                int charsToShow = Mathf.FloorToInt(charTimer);

                if (charsToShow > visibleCount)
                {
                    visibleCount = Mathf.Min(charsToShow, totalCharacters);
                    dialogueText.maxVisibleCharacters = visibleCount;
                }

                yield return null;
            }

            dialogueText.maxVisibleCharacters = totalCharacters;
            isTypewriterComplete = true;
            typewriterCoroutine = null;

            // Start auto-advance if applicable
            if (line.autoAdvance)
            {
                autoAdvanceCoroutine = StartCoroutine(AutoAdvanceCoroutine(line.duration));
            }
        }

        #endregion

        #region Auto-Advance

        private IEnumerator AutoAdvanceCoroutine(float delay)
        {
            float elapsed = 0f;
            while (elapsed < delay)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            autoAdvanceCoroutine = null;
            AdvanceToNextLine();
        }

        #endregion

        #region Utility

        private void StopLineCoroutines()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }

            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = null;
            }
        }

        private void StopAllPlayback()
        {
            StopLineCoroutines();
            IsPlaying = false;
            CurrentLineIndex = -1;
            isTypewriterComplete = false;
        }

        private void CompleteDialogue()
        {
            IsPlaying = false;
            CurrentLineIndex = -1;
            Hide();
            OnDialogueComplete?.Invoke();
        }

        #endregion
    }
}
