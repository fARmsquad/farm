namespace FarmSimVR.Core.Story
{
    [System.Serializable]
    public sealed class StorySequenceStepSnapshot
    {
        public string StepType;
        public string StringParam;
        public float FloatParam;
        public int IntParam;
        public float Duration;
        public bool WaitForCompletion;
    }
}
