using System;
using UnityEngine;
using UnityEngine.Playables;

namespace FarmSimVR.Timeline
{
    [Serializable]
    public class ScreenShakeBehaviour : PlayableBehaviour
    {
        public float intensity;
    }

    [Serializable]
    public class ScreenShakeClip : PlayableAsset
    {
        [Min(0f)]
        [Tooltip("Shake intensity (camera offset magnitude).")]
        public float intensity = 0.3f;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<ScreenShakeBehaviour>.Create(graph);
            playable.GetBehaviour().intensity = intensity;
            return playable;
        }
    }
}
