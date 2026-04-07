using System.Collections;
using UnityEngine;
using FarmSimVR.MonoBehaviours.Audio;

namespace FarmSimVR.MonoBehaviours.Autoplay
{
    public class AutoplayAudio : AutoplayBase
    {
        private void Awake()
        {
            specId = "INT-002";
            specTitle = "Audio Manager";
            totalSteps = 5;
        }

        protected override IEnumerator RunDemo()
        {
            var am = SimpleAudioManager.Instance;
            if (am == null) { currentLabel = "SimpleAudioManager not found!"; yield break; }

            Step("Play music (A3 tone, fade in)");
            var music = CreateTone("demo_music", 220f, 8f);
            am.PlayMusic(music, 0.8f);
            yield return Wait(3f);

            Step("Play SFX blip");
            var sfx = CreateTone("demo_sfx", 880f, 0.3f);
            am.PlaySFX(sfx);
            yield return Wait(1.5f);

            Step("Play another SFX");
            var sfx2 = CreateTone("demo_sfx2", 660f, 0.4f);
            am.PlaySFX(sfx2);
            yield return Wait(1.5f);

            Step("Crossfade to new music (E4 tone)");
            var music2 = CreateTone("demo_music2", 330f, 8f);
            am.CrossfadeMusic(music2, 2f);
            yield return Wait(4f);

            Step("Stop music (fade out)");
            am.StopMusic(1.5f);
            yield return Wait(2f);
        }

        private static AudioClip CreateTone(string clipName, float freq, float dur)
        {
            int sr = 44100;
            int count = Mathf.CeilToInt(sr * dur);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sr;
                float env = Mathf.Clamp01(Mathf.Min(t * 20f, (dur - t) * 20f));
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.3f * env;
            }
            var clip = AudioClip.Create(clipName, count, 1, sr, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
