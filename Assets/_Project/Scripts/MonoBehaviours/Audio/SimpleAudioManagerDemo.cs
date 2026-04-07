using UnityEngine;
using UnityEngine.InputSystem;
using FarmSimVR.MonoBehaviours.Debugging;

namespace FarmSimVR.MonoBehaviours.Audio
{
    public class SimpleAudioManagerDemo : MonoBehaviour
    {
        private SimpleAudioManager audioManager;
        private AudioClip dummyMusic;
        private AudioClip dummyMusicAlt;
        private AudioClip dummySfx;
        private static readonly Key Panel = DebugPanelShortcuts.AudioManager;

        private void Start()
        {
            audioManager = SimpleAudioManager.Instance ?? FindAnyObjectByType<SimpleAudioManager>();
            dummyMusic = CreateToneClip("DemoMusic", 220f, 10f);
            dummyMusicAlt = CreateToneClip("DemoMusicAlt", 330f, 10f);
            dummySfx = CreateToneClip("DemoSFX", 880f, 0.3f);
            Debug.Log($"[AudioDemo] Start — audioManager={(audioManager != null ? "found" : "NULL")}, clips created");
        }

        private void Update()
        {
            if (!DebugPanelShortcuts.UpdateToggle(Panel)) return;
            if (audioManager == null) audioManager = SimpleAudioManager.Instance ?? FindAnyObjectByType<SimpleAudioManager>();

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit1)) { Debug.Log("[Audio] Play Music"); audioManager?.PlayMusic(dummyMusic, 0.5f); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit2)) { Debug.Log("[Audio] Stop Music"); audioManager?.StopMusic(1f); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit3)) { Debug.Log("[Audio] Play SFX"); audioManager?.PlaySFX(dummySfx, 1f); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit4)) { Debug.Log("[Audio] Crossfade"); audioManager?.CrossfadeMusic(dummyMusicAlt, 1.5f); }
        }

        private void OnGUI()
        {
            if (!DebugPanelShortcuts.IsPanelActive(Panel)) return;

            float w = 280f; float h = 200f;
            float x = Screen.width - w - 10f; float y = 10f;
            float btnH = 28f; float pad = 3f;

            GUI.Box(new Rect(x, y, w, h), "Audio Manager (Shift+2)");
            float cy = y + 22f;

            string clipName = audioManager?.CurrentMusicClip != null ? audioManager.CurrentMusicClip.name : "None";
            GUI.Label(new Rect(x+4, cy, w-8, 20f), $"Playing: {(audioManager != null && audioManager.IsMusicPlaying ? "Yes" : "No")} | {clipName}");
            cy += 24f;

            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[1] Play Music (A3 tone)")) { audioManager?.PlayMusic(dummyMusic, 0.5f); } cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[2] Stop Music (1s fade)")) { audioManager?.StopMusic(1f); }                cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[3] Play SFX (blip)")) { audioManager?.PlaySFX(dummySfx, 1f); }             cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[4] Crossfade to E4")) { audioManager?.CrossfadeMusic(dummyMusicAlt, 1.5f); }
        }

        private static AudioClip CreateToneClip(string name, float frequency, float durationSec)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * durationSec);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Clamp01(Mathf.Min(t * 20f, (durationSec - t) * 20f));
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.3f * envelope;
            }
            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
