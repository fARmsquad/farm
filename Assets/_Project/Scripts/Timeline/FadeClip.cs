using System;
using UnityEngine;
using UnityEngine.Playables;

namespace FarmSimVR.Timeline
{
    [Serializable]
    public class FadeBehaviour : PlayableBehaviour
    {
        public float targetAlpha;
    }

    [Serializable]
    public class FadeClip : PlayableAsset
    {
        [Range(0f, 1f)]
        [Tooltip("Target screen fade alpha (0 = clear, 1 = black).")]
        public float targetAlpha = 1f;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<FadeBehaviour>.Create(graph);
            playable.GetBehaviour().targetAlpha = targetAlpha;
            return playable;
        }
    }
}
