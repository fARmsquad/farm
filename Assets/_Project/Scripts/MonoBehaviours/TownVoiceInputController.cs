using System;
using System.Collections;
using FarmSimVR.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours
{
    public enum TownVoiceInputStatusPhase
    {
        Hidden,
        Idle,
        Recording,
        Transcribing,
        Warning
    }

    /// <summary>
    /// Optional push-to-talk Town voice input that feeds transcripts into the normal prompt flow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TownVoiceInputController : MonoBehaviour
    {
        private const string RecordingStatus = "Listening...";
        private const string TranscribingStatus = "Transcribing...";
        private const string UnavailableStatus = "Voice input unavailable.";
        private const string EmptyCaptureStatus = "Didn't catch that.";

        [SerializeField] private LLMConversationController conversation;
        [SerializeField] private OpenAITranscriptionClient transcriptionClient;
        [SerializeField] private bool voiceInputEnabled = true;
        [SerializeField] private Key pushToTalkKey = Key.V;
        [SerializeField] [Range(1, 20)] private int maxRecordingSeconds = 8;
        [SerializeField] private int recordingSampleRate = 16000;
        [SerializeField] [TextArea] private string transcriptionPrompt =
            "This is a short player reply in the farming town of Willowbrook. Preserve punctuation and names like Garrett, Mira, and Pip.";

        private AudioClip _recordingClip;
        private bool _isRecording;
        private bool _isTranscribing;
        private int _transcriptionRequestId;

        public event Action<string> OnStatusChanged;

        public string CurrentStatus { get; private set; }
        public TownVoiceInputStatusPhase CurrentStatusPhase { get; private set; }

        private void Awake()
        {
            ResolveDependencies();
            SetStatus(TownVoiceInputStatusPhase.Hidden, string.Empty);
        }

        private void OnEnable()
        {
            ResolveDependencies();
            SubscribeConversationEvents();
            RefreshIdleStatus();
        }

        private void OnDisable()
        {
            UnsubscribeConversationEvents();
            StopRecordingIfNeeded();
            _isTranscribing = false;
            _transcriptionRequestId++;
            SetStatus(TownVoiceInputStatusPhase.Hidden, string.Empty);
        }

        private void Update()
        {
            if (!ShouldHandleVoiceInput())
            {
                StopRecordingIfNeeded();
                RefreshIdleStatus();
                return;
            }

            if (_isTranscribing)
                return;

            RefreshIdleStatus();
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (!_isRecording && keyboard[pushToTalkKey].wasPressedThisFrame)
            {
                StartRecording();
                return;
            }

            if (_isRecording && keyboard[pushToTalkKey].wasReleasedThisFrame)
                StopRecordingAndSubmit();
        }

        private bool ShouldHandleVoiceInput()
        {
            return voiceInputEnabled
                && conversation != null
                && conversation.IsInConversation;
        }

        private void ResolveDependencies()
        {
            conversation ??= GetComponent<LLMConversationController>();
            transcriptionClient ??= GetComponent<OpenAITranscriptionClient>();
            if (transcriptionClient == null)
                transcriptionClient = gameObject.AddComponent<OpenAITranscriptionClient>();
        }

        private void SubscribeConversationEvents()
        {
            if (conversation == null)
                return;

            conversation.OnConversationEnded -= HandleConversationClosed;
            conversation.OnError -= HandleConversationError;
            conversation.OnConversationEnded += HandleConversationClosed;
            conversation.OnError += HandleConversationError;
        }

        private void UnsubscribeConversationEvents()
        {
            if (conversation == null)
                return;

            conversation.OnConversationEnded -= HandleConversationClosed;
            conversation.OnError -= HandleConversationError;
        }

        private void StartRecording()
        {
            if (!HasMicrophoneDevice())
            {
                SetStatus(TownVoiceInputStatusPhase.Warning, UnavailableStatus);
                return;
            }

            _recordingClip = Microphone.Start(null, false, maxRecordingSeconds, recordingSampleRate);
            if (_recordingClip == null)
            {
                SetStatus(TownVoiceInputStatusPhase.Warning, UnavailableStatus);
                return;
            }

            _isRecording = true;
            SetStatus(TownVoiceInputStatusPhase.Recording, RecordingStatus);
        }

        private void StopRecordingAndSubmit()
        {
            int recordedFrames = Microphone.GetPosition(null);
            Microphone.End(null);
            _isRecording = false;

            if (!TryEncodeRecording(recordedFrames, out byte[] wavBytes))
            {
                _recordingClip = null;
                SetStatus(TownVoiceInputStatusPhase.Warning, EmptyCaptureStatus);
                return;
            }

            _recordingClip = null;
            _isTranscribing = true;
            SetStatus(TownVoiceInputStatusPhase.Transcribing, TranscribingStatus);

            int requestId = ++_transcriptionRequestId;
            StartCoroutine(SubmitRecordingForTranscription(wavBytes, requestId));
        }

        private bool TryEncodeRecording(int recordedFrames, out byte[] wavBytes)
        {
            wavBytes = null;
            if (_recordingClip == null || recordedFrames <= 0)
                return false;

            int sampleCount = recordedFrames * _recordingClip.channels;
            var interleavedSamples = new float[sampleCount];
            if (!_recordingClip.GetData(interleavedSamples, 0))
                return false;

            wavBytes = TownPcm16WavEncoder.Encode(
                interleavedSamples,
                _recordingClip.frequency,
                _recordingClip.channels,
                recordedFrames);
            return true;
        }

        private IEnumerator SubmitRecordingForTranscription(byte[] wavBytes, int requestId)
        {
            string transcript = null;
            string error = null;

            yield return transcriptionClient.TranscribeWav(
                wavBytes,
                transcriptionPrompt,
                onComplete: text => transcript = text,
                onError: message => error = message);

            if (!IsCurrentTranscriptionRequest(requestId))
                yield break;

            _isTranscribing = false;
            if (!string.IsNullOrWhiteSpace(error))
            {
                SetStatus(TownVoiceInputStatusPhase.Warning, error);
                yield break;
            }

            transcript = transcript?.Trim();
            if (string.IsNullOrWhiteSpace(transcript))
            {
                SetStatus(TownVoiceInputStatusPhase.Warning, EmptyCaptureStatus);
                yield break;
            }

            conversation.SubmitPlayerPrompt(transcript);
            RefreshIdleStatus();
        }

        private bool IsCurrentTranscriptionRequest(int requestId)
        {
            if (requestId != _transcriptionRequestId)
                return false;

            if (conversation == null || !conversation.IsInConversation)
            {
                SetStatus(TownVoiceInputStatusPhase.Hidden, string.Empty);
                return false;
            }

            return true;
        }

        private void HandleConversationClosed()
        {
            _transcriptionRequestId++;
            StopRecordingIfNeeded();
            _isTranscribing = false;
            SetStatus(TownVoiceInputStatusPhase.Hidden, string.Empty);
        }

        private void HandleConversationError(string _)
        {
            HandleConversationClosed();
        }

        private void StopRecordingIfNeeded()
        {
            if (!_isRecording)
                return;

            Microphone.End(null);
            _isRecording = false;
            _recordingClip = null;
        }

        private void RefreshIdleStatus()
        {
            if (_isRecording || _isTranscribing)
                return;

            if (!ShouldHandleVoiceInput())
            {
                SetStatus(TownVoiceInputStatusPhase.Hidden, string.Empty);
                return;
            }

            if (!HasMicrophoneDevice())
            {
                SetStatus(TownVoiceInputStatusPhase.Warning, UnavailableStatus);
                return;
            }

            SetStatus(TownVoiceInputStatusPhase.Idle, BuildIdleStatus());
        }

        private void SetStatus(TownVoiceInputStatusPhase phase, string status)
        {
            status ??= string.Empty;
            bool unchanged = CurrentStatusPhase == phase
                && string.Equals(CurrentStatus, status, StringComparison.Ordinal);
            if (unchanged)
                return;

            CurrentStatusPhase = phase;
            CurrentStatus = status;
            OnStatusChanged?.Invoke(CurrentStatus);
        }

        private string BuildIdleStatus()
        {
            return $"Hold {pushToTalkKey.ToString().ToUpperInvariant()} to speak";
        }

        private static bool HasMicrophoneDevice()
        {
            return Microphone.devices != null && Microphone.devices.Length > 0;
        }
    }
}
