using System;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    [Serializable]
    public struct CinematicStep
    {
        public CinematicStepType type;
        public string stringParam;
        public float floatParam;
        public int intParam;
        public float duration;
        public bool waitForCompletion;
    }
}
