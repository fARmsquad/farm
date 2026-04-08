using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmSimVR.MonoBehaviours.Autoplay
{
    /// <summary>
    /// Chains all autoplay scenes together. Loads each scene, waits for its
    /// AutoplayBase to finish, then loads the next. Survives scene loads
    /// via DontDestroyOnLoad. Shows overall progress HUD.
    /// </summary>
    public class AutoplayMasterChain : MonoBehaviour
    {
        [SerializeField] private string[] sceneNames;

        private int currentIndex = -1;
        private int totalScenes;
        private string currentSceneName = "";
        private bool waitingForDemo;
        private bool allDone;
        private float sceneStartTime;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (sceneNames == null || sceneNames.Length == 0)
            {
                sceneNames = new[]
                {
                    "Autoplay_INT001_ScreenEffects",
                    "Autoplay_INT002_AudioManager",
                    "Autoplay_INT003_Dialogue",
                    "Autoplay_INT004_CinematicCamera",
                    "Autoplay_INT006_NPC",
                    "Autoplay_INT007_Mission",
                    "Autoplay_INT009_Lighting",
                    "Autoplay_INT010_Particles",
                    "Autoplay_INT011_ComicText",
                    "Autoplay_INT012_SkipSave",
                    "Autoplay_INT013_IntroProps",
                };
            }

            totalScenes = sceneNames.Length;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Start()
        {
            StartCoroutine(ChainCoroutine());
        }

        private IEnumerator ChainCoroutine()
        {
            yield return new WaitForSeconds(1f);

            for (int i = 0; i < sceneNames.Length; i++)
            {
                currentIndex = i;
                currentSceneName = sceneNames[i];
                sceneStartTime = Time.realtimeSinceStartup;

                // Load the scene
                var op = SceneManager.LoadSceneAsync(currentSceneName, LoadSceneMode.Single);
                if (op == null)
                {
                    Debug.LogWarning($"[MasterChain] Scene not found: {currentSceneName}, skipping.");
                    continue;
                }

                yield return op;

                // Wait one frame for scene to initialize
                yield return null;
                yield return null;

                // Find the AutoplayBase in the loaded scene
                waitingForDemo = true;
                AutoplayBase demo = null;

                // Give it a few frames to find
                for (int f = 0; f < 10; f++)
                {
                    demo = FindAnyObjectByType<AutoplayBase>();
                    if (demo != null) break;
                    yield return null;
                }

                if (demo == null)
                {
                    Debug.LogWarning($"[MasterChain] No AutoplayBase found in {currentSceneName}, skipping after 3s.");
                    yield return new WaitForSeconds(3f);
                    waitingForDemo = false;
                    continue;
                }

                // Wait for demo to finish
                while (!demo.IsFinished)
                {
                    yield return null;
                }

                waitingForDemo = false;

                // Brief pause between scenes
                yield return new WaitForSeconds(2f);
            }

            allDone = true;
            currentSceneName = "ALL DEMOS COMPLETE";
        }

        private void OnGUI()
        {
            // Bottom bar showing chain progress
            float barH = 40f;
            float barY = Screen.height - barH;

            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(0, barY, Screen.width, barH), Texture2D.whiteTexture);

            // Gold accent
            GUI.color = new Color(0.72f, 0.53f, 0.04f);
            GUI.DrawTexture(new Rect(0, barY, Screen.width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Scene counter
            var counterStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.72f, 0.53f, 0.04f) },
                alignment = TextAnchor.MiddleLeft
            };
            string counter = allDone ? "COMPLETE" : $"Scene {currentIndex + 1} / {totalScenes}";
            GUI.Label(new Rect(16, barY + 4, 200, 32), counter, counterStyle);

            // Current scene name
            var nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(new Rect(0, barY + 4, Screen.width, 32), currentSceneName, nameStyle);

            // MASTER CHAIN badge
            var badgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.72f, 0.53f, 0.04f, 0.6f) },
                alignment = TextAnchor.MiddleRight
            };
            GUI.Label(new Rect(Screen.width - 150, barY + 4, 140, 32), "MASTER CHAIN", badgeStyle);

            // Progress bar
            float progress = totalScenes > 0 ? (float)(currentIndex + 1) / totalScenes : 0f;
            if (allDone) progress = 1f;
            GUI.color = new Color(1f, 1f, 1f, 0.08f);
            GUI.DrawTexture(new Rect(0, barY + barH - 3f, Screen.width, 3f), Texture2D.whiteTexture);
            GUI.color = new Color(0.72f, 0.53f, 0.04f);
            GUI.DrawTexture(new Rect(0, barY + barH - 3f, Screen.width * progress, 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
