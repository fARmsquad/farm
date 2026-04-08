using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.Timeline
{
    [TrackClipType(typeof(FadeClip))]
    [TrackBindingType(typeof(ScreenEffects))]
    [TrackColor(0.1f, 0.1f, 0.1f)]
    public class FadeTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<FadeMixerBehaviour>.Create(graph, inputCount);
        }
    }

    public class FadeMixerBehaviour : PlayableBehaviour
    {
        private ScreenEffects _screenEffects;
        private bool _bound;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            _screenEffects = playerData as ScreenEffects;
            if (_screenEffects == null) return;
            _bound = true;

            int inputCount = playable.GetInputCount();
            float blendedAlpha = 0f;

            for (int i = 0; i < inputCount; i++)
            {
                float weight = playable.GetInputWeight(i);
                if (weight <= 0f) continue;

                var inputPlayable = (ScriptPlayable<FadeBehaviour>)playable.GetInput(i);
                var behaviour = inputPlayable.GetBehaviour();
                blendedAlpha += behaviour.targetAlpha * weight;
            }

            _screenEffects.SetFadeAlphaDirect(blendedAlpha);
        }

        public override void OnGraphStop(Playable playable)
        {
            if (_bound && _screenEffects != null)
                _screenEffects.SetFadeAlphaDirect(0f);
        }
    }
}
