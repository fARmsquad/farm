using UnityEngine;

namespace FarmSimVR.MonoBehaviours.ChickenGame
{
    /// <summary>
    /// Intro line, looping BGM, and speech one-shots for the chicken minigame.
    /// </summary>
    public class ChickenGameSceneAudio : MonoBehaviour
    {
        [Header("Clips")]
        [SerializeField] private AudioClip _introClip;
        [SerializeField] private AudioClip _musicClip;
        [SerializeField] private AudioClip _grabClip;
        [SerializeField] private AudioClip _dropClip;

        private AudioSource _musicSource;
        private AudioSource _voiceSource;

        private void Awake()
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;

            _voiceSource = gameObject.AddComponent<AudioSource>();
            _voiceSource.playOnAwake = false;
            _voiceSource.loop = false;
            _voiceSource.spatialBlend = 0f;
        }

        private void Start()
        {
            if (_introClip != null)
                _voiceSource.PlayOneShot(_introClip);

            if (_musicClip != null)
            {
                _musicSource.clip = _musicClip;
                _musicSource.Play();
            }
        }

        public void PlayGrabLine()
        {
            if (_grabClip != null)
                _voiceSource.PlayOneShot(_grabClip);
        }

        public void PlayDropLine()
        {
            if (_dropClip != null)
                _voiceSource.PlayOneShot(_dropClip);
        }
    }
}
