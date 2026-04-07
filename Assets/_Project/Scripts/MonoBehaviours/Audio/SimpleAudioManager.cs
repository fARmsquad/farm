using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Audio
{
    /// <summary>
    /// Singleton audio manager handling music playback with fade/crossfade and one-shot SFX.
    /// Persists across scenes via DontDestroyOnLoad.
    /// </summary>
    public class SimpleAudioManager : MonoBehaviour
    {
        public static SimpleAudioManager Instance { get; private set; }

        [Header("Audio Library")]
        [SerializeField] private AudioLibrary audioLibrary;

        // Internal state
        private AudioSource musicSource;
        private AudioSource sfxSource;
        private Coroutine musicFadeCoroutine;

        // Public state
        public bool IsMusicPlaying => musicSource != null && musicSource.isPlaying;
        public AudioClip CurrentMusicClip => musicSource != null ? musicSource.clip : null;

        #region Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Create music AudioSource
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.playOnAwake = false;

            // Create SFX AudioSource
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 0f;
            sfxSource.playOnAwake = false;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Music Playback

        /// <summary>
        /// Stops current music and plays the given clip, fading in over the specified duration.
        /// Duration of 0 starts playback instantly.
        /// </summary>
        public void PlayMusic(AudioClip clip, float fadeInDuration)
        {
            if (musicFadeCoroutine != null)
                StopCoroutine(musicFadeCoroutine);

            musicSource.Stop();
            musicSource.clip = clip;
            musicSource.volume = 0f;
            musicSource.Play();

            if (fadeInDuration <= 0f)
            {
                musicSource.volume = 1f;
                return;
            }

            musicFadeCoroutine = StartCoroutine(FadeCoroutine(musicSource, 0f, 1f, fadeInDuration));
        }

        /// <summary>
        /// Fades out the currently playing music over the specified duration.
        /// No-op if music is not playing. Duration of 0 stops instantly.
        /// </summary>
        public void StopMusic(float fadeOutDuration)
        {
            if (!musicSource.isPlaying)
                return;

            if (musicFadeCoroutine != null)
                StopCoroutine(musicFadeCoroutine);

            if (fadeOutDuration <= 0f)
            {
                musicSource.Stop();
                musicSource.volume = 0f;
                return;
            }

            musicFadeCoroutine = StartCoroutine(FadeOutAndStopCoroutine(musicSource, fadeOutDuration));
        }

        /// <summary>
        /// Crossfades from the current music to a new clip over the specified duration.
        /// Same clip as current results in a no-op.
        /// </summary>
        public void CrossfadeMusic(AudioClip newClip, float duration)
        {
            if (musicSource.clip == newClip && musicSource.isPlaying)
                return;

            if (musicFadeCoroutine != null)
                StopCoroutine(musicFadeCoroutine);

            musicFadeCoroutine = StartCoroutine(CrossfadeCoroutine(newClip, duration));
        }

        /// <summary>
        /// Looks up a music clip by key from the AudioLibrary and plays it with a fade-in.
        /// Logs a warning if the key is not found.
        /// </summary>
        public void PlayMusicByKey(string key, float fadeInDuration)
        {
            if (audioLibrary == null)
            {
                Debug.LogWarning($"[SimpleAudioManager] No AudioLibrary assigned.");
                return;
            }

            AudioClip clip = audioLibrary.GetClip(key);
            if (clip == null)
            {
                Debug.LogWarning($"[SimpleAudioManager] Music key not found: \"{key}\"");
                return;
            }

            PlayMusic(clip, fadeInDuration);
        }

        #endregion

        #region SFX Playback

        /// <summary>
        /// Plays a one-shot sound effect at the given volume.
        /// </summary>
        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
                return;

            sfxSource.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// Looks up an SFX clip by key from the AudioLibrary and plays it.
        /// Logs a warning if the key is not found.
        /// </summary>
        public void PlaySFXByKey(string key, float volume = 1f)
        {
            if (audioLibrary == null)
            {
                Debug.LogWarning($"[SimpleAudioManager] No AudioLibrary assigned.");
                return;
            }

            AudioClip clip = audioLibrary.GetClip(key);
            if (clip == null)
            {
                Debug.LogWarning($"[SimpleAudioManager] SFX key not found: \"{key}\"");
                return;
            }

            PlaySFX(clip, volume);
        }

        #endregion

        #region Fade Coroutines

        private IEnumerator FadeCoroutine(AudioSource source, float from, float to, float duration)
        {
            float elapsed = 0f;
            source.volume = from;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                source.volume = Mathf.Lerp(from, to, t);
                yield return null;
            }

            source.volume = to;
            musicFadeCoroutine = null;
        }

        private IEnumerator FadeOutAndStopCoroutine(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                source.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            source.volume = 0f;
            source.Stop();
            musicFadeCoroutine = null;
        }

        private IEnumerator CrossfadeCoroutine(AudioClip newClip, float duration)
        {
            // Create a temporary AudioSource for the outgoing music
            AudioSource outgoingSource = gameObject.AddComponent<AudioSource>();
            outgoingSource.clip = musicSource.clip;
            outgoingSource.volume = musicSource.volume;
            outgoingSource.loop = musicSource.loop;
            outgoingSource.spatialBlend = 0f;
            outgoingSource.time = musicSource.time;
            outgoingSource.Play();

            float outgoingStartVolume = outgoingSource.volume;

            // Start the new clip on the main music source
            musicSource.Stop();
            musicSource.clip = newClip;
            musicSource.volume = 0f;
            musicSource.Play();

            if (duration <= 0f)
            {
                musicSource.volume = 1f;
                Destroy(outgoingSource);
                musicFadeCoroutine = null;
                yield break;
            }

            // Crossfade: fade out old, fade in new
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                outgoingSource.volume = Mathf.Lerp(outgoingStartVolume, 0f, t);
                musicSource.volume = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            musicSource.volume = 1f;
            outgoingSource.Stop();
            Destroy(outgoingSource);
            musicFadeCoroutine = null;
        }

        #endregion
    }
}
