using System;
using UnityEngine;
using UnityEngine.Playables;

namespace FarmSimVR.Timeline
{
    public enum AudioEventType
    {
        PlaySFX,
        PlayMusic,
        StopMusic
    }

    [Serializable]
    public class AudioEventBehaviour : PlayableBehaviour
    {
        public AudioEventType eventType;
        public string audioKey;
        public float volume;
        public float fadeDuration;

        private bool _triggered;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            // Prevent re-triggering when scrubbing or looping
            if (_triggered) return;
            if (!Application.isPlaying) return;

            _triggered = true;
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            // Reset trigger so it can fire again on next play
            _triggered = false;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            // Audio dispatch is handled by the mixer
        }
    }

    [Serializable]
    public class AudioEventClip : PlayableAsset
    {
        [Tooltip("Type of audio event to fire.")]
        public AudioEventType eventType = AudioEventType.PlaySFX;

        [Tooltip("Audio key registered in SimpleAudioManager.")]
        public string audioKey = "";

        [Range(0f, 2f)]
        [Tooltip("Volume for SFX, or fade duration for Music.")]
        public float volumeOrFade = 1f;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<AudioEventBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.eventType = eventType;
            behaviour.audioKey = audioKey;
            behaviour.volume = volumeOrFade;
            behaviour.fadeDuration = volumeOrFade;
            return playable;
        }
    }
}
