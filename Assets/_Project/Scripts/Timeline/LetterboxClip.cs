using System;
using UnityEngine;
using UnityEngine.Playables;

namespace FarmSimVR.Timeline
{
    [Serializable]
    public class LetterboxBehaviour : PlayableBehaviour
    {
        public float targetHeight;
    }

    [Serializable]
    public class LetterboxClip : PlayableAsset
    {
        [Range(0f, 1f)]
        [Tooltip("Target letterbox height (0 = hidden, 1 = fully visible).")]
        public float targetHeight = 1f;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<LetterboxBehaviour>.Create(graph);
            playable.GetBehaviour().targetHeight = targetHeight;
            return playable;
        }
    }
}
