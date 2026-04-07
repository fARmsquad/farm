using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    [CreateAssetMenu(fileName = "NewCinematicSequence", menuName = "FarmSimVR/Cinematic Sequence")]
    public class CinematicSequence : ScriptableObject
    {
        public CinematicStep[] steps = new CinematicStep[0];
    }
}
