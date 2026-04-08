using System;
using UnityEngine;
using UnityEngine.Playables;

namespace FarmSimVR.Timeline
{
    [Serializable]
    public class SubtitleBehaviour : PlayableBehaviour
    {
        public string text;
    }

    [Serializable]
    public class SubtitleClip : PlayableAsset
    {
        [TextArea(2, 4)]
        [Tooltip("Subtitle text to display during this clip.")]
        public string text = "";

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<SubtitleBehaviour>.Create(graph);
            playable.GetBehaviour().text = text;
            return playable;
        }
    }
}
