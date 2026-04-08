using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.Timeline
{
    [TrackClipType(typeof(ScreenShakeClip))]
    [TrackBindingType(typeof(ScreenEffects))]
    [TrackColor(0.9f, 0.2f, 0.2f)]
    public class ScreenShakeTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<ScreenShakeMixerBehaviour>.Create(graph, inputCount);
        }
    }

    public class ScreenShakeMixerBehaviour : PlayableBehaviour
    {
        private ScreenEffects _screenEffects;
        private bool _bound;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            _screenEffects = playerData as ScreenEffects;
            if (_screenEffects == null) return;
            _bound = true;

            int inputCount = playable.GetInputCount();
            float blendedIntensity = 0f;

            for (int i = 0; i < inputCount; i++)
            {
                float weight = playable.GetInputWeight(i);
                if (weight <= 0f) continue;

                var inputPlayable = (ScriptPlayable<ScreenShakeBehaviour>)playable.GetInput(i);
                var behaviour = inputPlayable.GetBehaviour();
                blendedIntensity += behaviour.intensity * weight;
            }

            _screenEffects.SetTimelineShake(blendedIntensity);
        }

        public override void OnGraphStop(Playable playable)
        {
            if (_bound && _screenEffects != null)
                _screenEffects.SetTimelineShake(0f);
        }
    }
}
