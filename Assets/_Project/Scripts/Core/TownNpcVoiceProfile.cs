namespace FarmSimVR.Core
{
    /// <summary>
    /// Immutable voice configuration for a Town NPC ElevenLabs session.
    /// </summary>
    public sealed class TownNpcVoiceProfile
    {
        public string VoiceId { get; }
        public string ModelId { get; }
        public string OutputFormat { get; }
        public float Stability { get; }
        public float SimilarityBoost { get; }
        public float Style { get; }
        public bool UseSpeakerBoost { get; }
        public float Speed { get; }
        public int SampleRate { get; }

        public TownNpcVoiceProfile(
            string voiceId,
            string modelId,
            string outputFormat,
            float stability,
            float similarityBoost,
            float style,
            bool useSpeakerBoost,
            float speed,
            int sampleRate)
        {
            VoiceId = voiceId;
            ModelId = modelId;
            OutputFormat = outputFormat;
            Stability = stability;
            SimilarityBoost = similarityBoost;
            Style = style;
            UseSpeakerBoost = useSpeakerBoost;
            Speed = speed;
            SampleRate = sampleRate;
        }
    }
}
