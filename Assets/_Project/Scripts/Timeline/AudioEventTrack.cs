using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using FarmSimVR.MonoBehaviours.Audio;

namespace FarmSimVR.Timeline
{
    [TrackClipType(typeof(AudioEventClip))]
    [TrackBindingType(typeof(SimpleAudioManager))]
    [TrackColor(0.9f, 0.6f, 0.1f)]
    public class AudioEventTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<AudioEventMixerBehaviour>.Create(graph, inputCount);
        }
    }

    public class AudioEventMixerBehaviour : PlayableBehaviour
    {
        // Track which clips have already fired to avoid repeat triggers
        private bool[] _fired;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var audioManager = playerData as SimpleAudioManager;
            if (audioManager == null) return;
            if (!Application.isPlaying) return;

            int inputCount = playable.GetInputCount();

            if (_fired == null || _fired.Length != inputCount)
                _fired = new bool[inputCount];

            for (int i = 0; i < inputCount; i++)
            {
                float weight = playable.GetInputWeight(i);

                if (weight > 0f && !_fired[i])
                {
                    _fired[i] = true;
                    var inputPlayable = (ScriptPlayable<AudioEventBehaviour>)playable.GetInput(i);
                    var behaviour = inputPlayable.GetBehaviour();
                    DispatchAudioEvent(audioManager, behaviour);
                }
                else if (weight <= 0f)
                {
                    _fired[i] = false;
                }
            }
        }

        private static void DispatchAudioEvent(SimpleAudioManager manager, AudioEventBehaviour behaviour)
        {
            switch (behaviour.eventType)
            {
                case AudioEventType.PlaySFX:
                    manager.PlaySFXByKey(behaviour.audioKey, behaviour.volume);
                    break;
                case AudioEventType.PlayMusic:
                    manager.PlayMusicByKey(behaviour.audioKey, behaviour.fadeDuration);
                    break;
                case AudioEventType.StopMusic:
                    manager.StopMusic(behaviour.fadeDuration);
                    break;
            }
        }
    }
}
