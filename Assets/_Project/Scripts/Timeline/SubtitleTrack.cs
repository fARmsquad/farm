using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.Timeline
{
    [TrackClipType(typeof(SubtitleClip))]
    [TrackBindingType(typeof(ScreenEffects))]
    [TrackColor(0.2f, 0.6f, 0.9f)]
    public class SubtitleTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<SubtitleMixerBehaviour>.Create(graph, inputCount);
        }
    }

    public class SubtitleMixerBehaviour : PlayableBehaviour
    {
        private ScreenEffects _screenEffects;
        private bool _bound;
        private bool _showing;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            _screenEffects = playerData as ScreenEffects;
            if (_screenEffects == null) return;
            _bound = true;

            int inputCount = playable.GetInputCount();
            float bestWeight = 0f;
            string activeText = null;

            for (int i = 0; i < inputCount; i++)
            {
                float weight = playable.GetInputWeight(i);
                if (weight <= bestWeight) continue;

                var inputPlayable = (ScriptPlayable<SubtitleBehaviour>)playable.GetInput(i);
                var behaviour = inputPlayable.GetBehaviour();

                if (!string.IsNullOrEmpty(behaviour.text))
                {
                    bestWeight = weight;
                    activeText = behaviour.text;
                }
            }

            if (activeText != null)
            {
                _screenEffects.ShowSubtitleText(activeText);
                _showing = true;
            }
            else if (_showing)
            {
                _screenEffects.HideSubtitleText();
                _showing = false;
            }
        }

        public override void OnGraphStop(Playable playable)
        {
            if (_bound && _screenEffects != null && _showing)
            {
                _screenEffects.HideSubtitleText();
                _showing = false;
            }
        }
    }
}
