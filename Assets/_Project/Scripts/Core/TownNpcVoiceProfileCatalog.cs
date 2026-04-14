namespace FarmSimVR.Core
{
    /// <summary>
    /// Stable ElevenLabs voice mapping for Town NPCs.
    /// </summary>
    public static class TownNpcVoiceProfileCatalog
    {
        private static readonly TownNpcVoiceProfile OldGarrett = new(
            "pqHfZKP75CvOlQylNhV4",
            "eleven_flash_v2_5",
            "pcm_16000",
            0.62f,
            0.78f,
            0.35f,
            true,
            0.92f,
            16000);

        private static readonly TownNpcVoiceProfile MiraTheBaker = new(
            "hpp4J3VqNfWAUOO0d1Us",
            "eleven_flash_v2_5",
            "pcm_16000",
            0.55f,
            0.75f,
            0.45f,
            true,
            1f,
            16000);

        private static readonly TownNpcVoiceProfile YoungPip = new(
            "TX3LPaxmHKxFdv7VOQHJ",
            "eleven_flash_v2_5",
            "pcm_16000",
            0.45f,
            0.7f,
            0.65f,
            true,
            1.08f,
            16000);

        private static readonly TownNpcVoiceProfile Fallback = OldGarrett;

        public static TownNpcVoiceProfile GetProfile(string npcName)
        {
            return npcName switch
            {
                "Old Garrett" => OldGarrett,
                "Mira the Baker" => MiraTheBaker,
                "Young Pip" => YoungPip,
                _ => Fallback
            };
        }
    }
}
