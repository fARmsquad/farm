using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.Timeline
{
    [TrackClipType(typeof(LetterboxClip))]
    [TrackBindingType(typeof(ScreenEffects))]
    [TrackColor(0.3f, 0.3f, 0.3f)]
    public class LetterboxTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<LetterboxMixerBehaviour>.Create(graph, inputCount);
        }
    }

    public class LetterboxMixerBehaviour : PlayableBehaviour
    {
        private ScreenEffects _screenEffects;
        private bool _bound;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            _screenEffects = playerData as ScreenEffects;
            if (_screenEffects == null) return;
            _bound = true;

            int inputCount = playable.GetInputCount();
            float blendedHeight = 0f;

            for (int i = 0; i < inputCount; i++)
            {
                float weight = playable.GetInputWeight(i);
                if (weight <= 0f) continue;

                var inputPlayable = (ScriptPlayable<LetterboxBehaviour>)playable.GetInput(i);
                var behaviour = inputPlayable.GetBehaviour();
                blendedHeight += behaviour.targetHeight * weight;
            }

            _screenEffects.SetLetterboxHeightDirect(blendedHeight);
        }

        public override void OnGraphStop(Playable playable)
        {
            if (_bound && _screenEffects != null)
                _screenEffects.SetLetterboxHeightDirect(0f);
        }
    }
}
