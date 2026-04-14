using System.Collections.Generic;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Queues PCM16 samples into a looping streaming clip for low-latency playback.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class StreamingPcmAudioPlayer : MonoBehaviour
    {
        private const int ChannelCount = 1;
        private const int ClipLengthSeconds = 2;

        private readonly Queue<float> _samples = new();
        private readonly object _sampleLock = new();

        private AudioSource _audioSource;
        private AudioClip _streamingClip;
        private int _sampleRate = 16000;
        private int _bufferedSamples;
        private int _prebufferSamples;
        private bool _inputComplete;

        private void Awake()
        {
            EnsureAudioSource();
        }

        private void Update()
        {
            if (_streamingClip == null)
                return;

            if (!_audioSource.isPlaying && GetBufferedSampleCount() >= _prebufferSamples)
                _audioSource.Play();

            if (_audioSource.isPlaying && _inputComplete && GetBufferedSampleCount() == 0)
                _audioSource.Stop();
        }

        private void OnDestroy()
        {
            if (_streamingClip != null)
                Destroy(_streamingClip);
        }

        public void Prepare(int sampleRate)
        {
            _sampleRate = Mathf.Max(8000, sampleRate);
            _prebufferSamples = Mathf.Max(1024, _sampleRate / 5);
            EnsureAudioSource();
            ResetBuffer();
            EnsureStreamingClip();
        }

        public void EnqueuePcm16(byte[] pcmBytes)
        {
            if (pcmBytes == null || pcmBytes.Length < 2)
                return;

            lock (_sampleLock)
            {
                for (int i = 0; i + 1 < pcmBytes.Length; i += 2)
                {
                    short sample = (short)(pcmBytes[i] | (pcmBytes[i + 1] << 8));
                    _samples.Enqueue(sample / 32768f);
                    _bufferedSamples++;
                }
            }
        }

        public void MarkInputComplete()
        {
            _inputComplete = true;
        }

        public void Clear()
        {
            ResetBuffer();
            if (_audioSource != null)
                _audioSource.Stop();
        }

        private void EnsureAudioSource()
        {
            if (_audioSource != null)
                return;

            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = true;
            _audioSource.spatialBlend = 0f;
        }

        private void EnsureStreamingClip()
        {
            if (_streamingClip != null && _streamingClip.frequency == _sampleRate)
                return;

            if (_streamingClip != null)
                Destroy(_streamingClip);

            _streamingClip = AudioClip.Create(
                "TownVoiceStream",
                _sampleRate * ClipLengthSeconds,
                ChannelCount,
                _sampleRate,
                true,
                OnAudioRead);

            _audioSource.clip = _streamingClip;
        }

        private void OnAudioRead(float[] data)
        {
            lock (_sampleLock)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (_samples.Count == 0)
                    {
                        data[i] = 0f;
                        continue;
                    }

                    data[i] = _samples.Dequeue();
                    _bufferedSamples--;
                }
            }
        }

        private int GetBufferedSampleCount()
        {
            lock (_sampleLock)
                return _bufferedSamples;
        }

        private void ResetBuffer()
        {
            lock (_sampleLock)
            {
                _samples.Clear();
                _bufferedSamples = 0;
            }

            _inputComplete = false;
        }
    }
}
