using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FarmSimVR.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Bridges streamed OpenAI text into ElevenLabs streaming voice playback.
    /// </summary>
    public sealed class TownNpcVoiceStreamController : MonoBehaviour
    {
        private const string LogPrefix = "[TownVoice]";

        [SerializeField] private LLMConversationController conversation;
        [SerializeField] private string tokenServiceBaseUrl = "http://127.0.0.1:8000";
        [SerializeField] private bool voiceEnabled = true;
        [SerializeField] private StreamingPcmAudioPlayer pcmPlayer;

        private readonly object _pendingLock = new();
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly Queue<string> _pendingTextChunks = new();

        private CancellationTokenSource _sessionCts;
        private ClientWebSocket _socket;
        private TownVoiceTextChunker _chunker;
        private string _pendingWarning;
        private bool _responseCompleted;
        private bool _sessionReady;
        private bool _stopRequested;
        private int _sessionVersion;

        private void Awake()
        {
            conversation ??= GetComponent<LLMConversationController>();
            pcmPlayer ??= GetComponent<StreamingPcmAudioPlayer>();
            pcmPlayer ??= gameObject.AddComponent<StreamingPcmAudioPlayer>();
        }

        private void OnEnable()
        {
            if (conversation == null)
                return;

            conversation.OnStreamStarted += HandleStreamStarted;
            conversation.OnStreamChunk += HandleStreamChunk;
            conversation.OnNPCResponse += HandleResponseCompleted;
            conversation.OnConversationEnded += HandleConversationEnded;
            conversation.OnError += HandleError;
        }

        private void OnDisable()
        {
            if (conversation != null)
            {
                conversation.OnStreamStarted -= HandleStreamStarted;
                conversation.OnStreamChunk -= HandleStreamChunk;
                conversation.OnNPCResponse -= HandleResponseCompleted;
                conversation.OnConversationEnded -= HandleConversationEnded;
                conversation.OnError -= HandleError;
            }

            StopVoiceSession(clearPlayer: true);
        }

        private void Update()
        {
            if (_stopRequested)
            {
                _stopRequested = false;
                StopVoiceSession(clearPlayer: true);
            }

            if (string.IsNullOrEmpty(_pendingWarning))
                return;

            Debug.LogWarning($"{LogPrefix} {_pendingWarning}");
            _pendingWarning = null;
        }

        private void HandleStreamStarted(string npcName)
        {
            if (!voiceEnabled || string.IsNullOrWhiteSpace(npcName))
                return;

            StopVoiceSession(clearPlayer: true);
            _sessionVersion++;
            TownNpcVoiceProfile profile = TownNpcVoiceProfileCatalog.GetProfile(npcName);
            _chunker = new TownVoiceTextChunker();
            _sessionCts = new CancellationTokenSource();
            _responseCompleted = false;
            _sessionReady = false;
            ClearPendingChunks();
            pcmPlayer.Prepare(profile.SampleRate);
            StartCoroutine(BeginVoiceSession(_sessionVersion, profile, _sessionCts.Token));
        }

        private void HandleStreamChunk(string delta)
        {
            if (!IsSessionActive())
                return;

            IReadOnlyList<string> completed = _chunker.Append(delta);
            for (int i = 0; i < completed.Count; i++)
                QueueOrSendChunk(completed[i]);
        }

        private void HandleResponseCompleted(string npcName, string responseText)
        {
            if (!IsSessionActive())
                return;

            string trailingText = _chunker.Flush();
            if (!string.IsNullOrWhiteSpace(trailingText))
                QueueOrSendChunk(trailingText);

            _responseCompleted = true;
            _ = SendCloseSignalAsync(_sessionVersion, _sessionCts.Token);
        }

        private void HandleConversationEnded()
        {
            StopVoiceSession(clearPlayer: true);
        }

        private void HandleError(string _)
        {
            StopVoiceSession(clearPlayer: true);
        }

        private IEnumerator BeginVoiceSession(
            int version,
            TownNpcVoiceProfile profile,
            CancellationToken cancellationToken)
        {
            string url = $"{tokenServiceBaseUrl.TrimEnd('/')}/api/v1/elevenlabs/tts-websocket-token";
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (!IsCurrentSession(version) || cancellationToken.IsCancellationRequested)
                yield break;

            if (request.result != UnityWebRequest.Result.Success)
            {
                SetPendingWarning("Voice streaming is unavailable; continuing with text only.");
                StopVoiceSession(clearPlayer: true);
                yield break;
            }

            TokenResponse payload = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
            if (payload == null || string.IsNullOrWhiteSpace(payload.token))
            {
                SetPendingWarning("Voice token response was empty; continuing with text only.");
                StopVoiceSession(clearPlayer: true);
                yield break;
            }

            _ = ConnectAndReceiveAsync(version, profile, payload.token, cancellationToken);
        }

        private async Task ConnectAndReceiveAsync(
            int version,
            TownNpcVoiceProfile profile,
            string token,
            CancellationToken cancellationToken)
        {
            try
            {
                var socket = new ClientWebSocket();
                _socket = socket;
                await socket.ConnectAsync(BuildSocketUri(profile, token), cancellationToken);
                await SendInitializationAsync(version, profile, cancellationToken);
                _sessionReady = true;
                await DrainPendingChunksAsync(version, cancellationToken);

                if (_responseCompleted)
                    await SendCloseSignalAsync(version, cancellationToken);

                await ReceiveAudioLoopAsync(version, socket, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested || !IsCurrentSession(version))
                    return;

                SetPendingWarning($"Voice stream stopped: {ex.Message}");
                _stopRequested = true;
            }
        }

        private async Task ReceiveAudioLoopAsync(
            int version,
            ClientWebSocket socket,
            CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            using var messageStream = new MemoryStream();

            while (IsCurrentSession(version) && socket.State == WebSocketState.Open)
            {
                var segment = new ArraySegment<byte>(buffer);
                WebSocketReceiveResult result = await socket.ReceiveAsync(segment, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                messageStream.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage)
                    continue;

                if (!IsCurrentSession(version))
                    break;

                string message = Encoding.UTF8.GetString(messageStream.ToArray());
                messageStream.SetLength(0);
                HandleAudioMessage(message);
            }

            if (IsCurrentSession(version))
                pcmPlayer.MarkInputComplete();
        }

        private void HandleAudioMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            string audioBase64 = ExtractJsonStringValue(message, "audio");
            if (!string.IsNullOrWhiteSpace(audioBase64))
                pcmPlayer.EnqueuePcm16(Convert.FromBase64String(audioBase64));

            if (ExtractJsonBoolValue(message, "isFinal"))
                pcmPlayer.MarkInputComplete();
        }

        private void QueueOrSendChunk(string chunk)
        {
            if (string.IsNullOrWhiteSpace(chunk))
                return;

            if (!_sessionReady)
            {
                lock (_pendingLock)
                    _pendingTextChunks.Enqueue(chunk);
                return;
            }

            _ = SendTextChunkAsync(_sessionVersion, chunk, _sessionCts.Token);
        }

        private async Task DrainPendingChunksAsync(int version, CancellationToken cancellationToken)
        {
            while (TryDequeueChunk(out string chunk))
                await SendTextChunkAsync(version, chunk, cancellationToken);
        }

        private async Task SendInitializationAsync(
            int version,
            TownNpcVoiceProfile profile,
            CancellationToken cancellationToken)
        {
            string payload =
                "{" +
                "\"text\":\" \"," +
                "\"voice_settings\":{" +
                $"\"speed\":{FormatFloat(profile.Speed)}," +
                $"\"stability\":{FormatFloat(profile.Stability)}," +
                $"\"similarity_boost\":{FormatFloat(profile.SimilarityBoost)}," +
                $"\"style\":{FormatFloat(profile.Style)}," +
                $"\"use_speaker_boost\":{FormatBool(profile.UseSpeakerBoost)}" +
                "}" +
                "}";

            await SendJsonAsync(version, payload, cancellationToken);
        }

        private async Task SendTextChunkAsync(int version, string chunk, CancellationToken cancellationToken)
        {
            string payload = "{\"text\":\"" + EscapeJson(chunk) + " \"}";
            await SendJsonAsync(version, payload, cancellationToken);
        }

        private async Task SendCloseSignalAsync(int version, CancellationToken cancellationToken)
        {
            if (!IsCurrentSession(version) || !_sessionReady)
                return;

            await SendJsonAsync(version, "{\"text\":\"\"}", cancellationToken);
        }

        private async Task SendJsonAsync(int version, string payload, CancellationToken cancellationToken)
        {
            if (!IsCurrentSession(version) || _socket == null || _socket.State != WebSocketState.Open)
                return;

            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            await _sendLock.WaitAsync(cancellationToken);

            try
            {
                if (!IsCurrentSession(version) || _socket == null || _socket.State != WebSocketState.Open)
                    return;

                var segment = new ArraySegment<byte>(bytes);
                await _socket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private void StopVoiceSession(bool clearPlayer)
        {
            _sessionReady = false;
            _responseCompleted = false;
            _chunker = null;
            ClearPendingChunks();
            _sessionCts?.Cancel();
            _sessionCts?.Dispose();
            _sessionCts = null;
            _socket?.Dispose();
            _socket = null;

            if (clearPlayer && pcmPlayer != null)
                pcmPlayer.Clear();
        }

        private bool IsSessionActive()
        {
            return voiceEnabled && _chunker != null && _sessionCts != null;
        }

        private bool IsCurrentSession(int version)
        {
            return version == _sessionVersion && _sessionCts != null;
        }

        private bool TryDequeueChunk(out string chunk)
        {
            lock (_pendingLock)
            {
                if (_pendingTextChunks.Count == 0)
                {
                    chunk = null;
                    return false;
                }

                chunk = _pendingTextChunks.Dequeue();
                return true;
            }
        }

        private void ClearPendingChunks()
        {
            lock (_pendingLock)
                _pendingTextChunks.Clear();
        }

        private void SetPendingWarning(string message)
        {
            _pendingWarning = message;
        }

        private Uri BuildSocketUri(TownNpcVoiceProfile profile, string token)
        {
            string baseUri = $"wss://api.elevenlabs.io/v1/text-to-speech/{profile.VoiceId}/stream-input";
            string query =
                $"model_id={Uri.EscapeDataString(profile.ModelId)}" +
                $"&output_format={Uri.EscapeDataString(profile.OutputFormat)}" +
                $"&auto_mode=true" +
                $"&single_use_token={Uri.EscapeDataString(token)}";

            return new Uri($"{baseUri}?{query}");
        }

        private static string ExtractJsonStringValue(string json, string key)
        {
            int valueStart = FindJsonValueStart(json, key);
            if (valueStart < 0 || valueStart >= json.Length || json[valueStart] != '"')
                return null;

            var result = new StringBuilder();
            for (int i = valueStart + 1; i < json.Length; i++)
            {
                char current = json[i];
                if (current == '\\' && i + 1 < json.Length)
                {
                    char escaped = json[++i];
                    result.Append(escaped switch
                    {
                        '"' => '"',
                        '\\' => '\\',
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        _ => escaped
                    });
                    continue;
                }

                if (current == '"')
                    return result.ToString();

                result.Append(current);
            }

            return null;
        }

        private static bool ExtractJsonBoolValue(string json, string key)
        {
            int valueStart = FindJsonValueStart(json, key);
            if (valueStart < 0)
                return false;

            if (json.IndexOf("true", valueStart, StringComparison.Ordinal) == valueStart)
                return true;

            return false;
        }

        private static int FindJsonValueStart(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
                return -1;

            string quotedKey = $"\"{key}\"";
            int keyIndex = json.IndexOf(quotedKey, StringComparison.Ordinal);
            if (keyIndex < 0)
                return -1;

            int colonIndex = json.IndexOf(':', keyIndex + quotedKey.Length);
            if (colonIndex < 0)
                return -1;

            int valueStart = colonIndex + 1;
            while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
                valueStart++;

            return valueStart;
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
        }

        private static string FormatBool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        [Serializable]
        private sealed class TokenResponse
        {
            public string token;
        }
    }
}
