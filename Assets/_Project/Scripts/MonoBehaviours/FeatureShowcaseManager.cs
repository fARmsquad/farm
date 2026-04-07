using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Diagnostics;
using FarmSimVR.MonoBehaviours.Audio;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Drives a step-by-step feature showcase. Each step shows instructions
    /// on screen and triggers the relevant system. Press "Next" to advance.
    /// </summary>
    public class FeatureShowcaseManager : MonoBehaviour
    {
        [Header("References (wired by builder)")]
        [SerializeField] private FirstPersonExplorer explorer;
        [SerializeField] private Transform npcSpawnPoint;

        private int _currentStep = -1;
        private bool _waitingForInput;
        private string _title = "";
        private string _instructions = "";
        private string _status = "";
        private bool _showNext = true;
        private GameObject _spawnedNpc;
        private Coroutine _activeRoutine;

        private readonly struct Step
        {
            public readonly string Title;
            public readonly string Instructions;
            public readonly Action<FeatureShowcaseManager> Execute;

            public Step(string title, string instructions, Action<FeatureShowcaseManager> execute)
            {
                Title = title;
                Instructions = instructions;
                Execute = execute;
            }
        }

        private Step[] _steps;

        private void Awake()
        {
            _steps = new Step[]
            {
                new("Welcome", "Welcome to the FarmSim VR Feature Showcase!\n\nThis will walk you through every feature built so far.\nUse WASD to move, mouse to look around.\n\nPress Next to begin.", _ => {}),

                // ── Screen Effects ──
                new("Fade To Black", "Watch the screen fade to black, then back.", m =>
                {
                    m._showNext = false;
                    m._status = "Fading to black...";
                    ScreenEffects.Instance?.FadeToBlack(1f, () =>
                    {
                        m._status = "Fading back in...";
                        ScreenEffects.Instance?.FadeFromBlack(1f, () =>
                        {
                            m._status = "Done!";
                            m._showNext = true;
                        });
                    });
                }),

                new("Screen Shake", "The camera will shake for 1 second.", m =>
                {
                    m._showNext = false;
                    m._status = "Shaking...";
                    ScreenEffects.Instance?.ScreenShake(0.4f, 1f, () =>
                    {
                        m._status = "Done!";
                        m._showNext = true;
                    });
                }),

                new("Letterbox", "Cinematic letterbox bars will appear, then disappear.", m =>
                {
                    m._showNext = false;
                    m._status = "Showing letterbox...";
                    ScreenEffects.Instance?.ShowLetterbox(1f, 0.5f, () =>
                    {
                        m._activeRoutine = m.StartCoroutine(m.DelayThen(1.5f, () =>
                        {
                            m._status = "Hiding letterbox...";
                            ScreenEffects.Instance?.HideLetterbox(0.5f, () =>
                            {
                                m._status = "Done!";
                                m._showNext = true;
                            });
                        }));
                    });
                }),

                new("Objective Popup", "An objective will slide in from the left.", m =>
                {
                    m._showNext = false;
                    m._status = "Showing objective...";
                    ScreenEffects.Instance?.ShowObjective("Find the farmhouse", () =>
                    {
                        m._status = "Done!";
                        m._showNext = true;
                    });
                }),

                new("Mission Passed", "A 'Mission Passed' banner will appear.", m =>
                {
                    m._showNext = false;
                    m._status = "Showing banner...";
                    ScreenEffects.Instance?.ShowMissionPassed("SHOWCASE PASSED", () =>
                    {
                        m._status = "Done!";
                        m._showNext = true;
                    });
                }),

                // ── Cinematic Camera ──
                new("Cinematic Camera: Fly Over", "The camera will fly to an overhead view, then return to you.", m =>
                {
                    m._showNext = false;
                    m._status = "Flying to overview...";
                    var cam = FindAnyObjectByType<CinematicCamera>();
                    if (cam == null) { m._status = "CinematicCamera not found"; m._showNext = true; return; }

                    cam.EnableCinematicCamera();
                    var wp = new CameraWaypoint
                    {
                        position = m.explorer.transform.position + new Vector3(0f, 25f, -15f),
                        rotation = Quaternion.Euler(60f, 0f, 0f),
                        fov = 60f, duration = 2f,
                        easing = AnimationCurve.EaseInOut(0, 0, 1, 1)
                    };
                    cam.MoveToWaypoint(wp);
                    m._activeRoutine = m.StartCoroutine(m.DelayThen(3f, () =>
                    {
                        m._status = "Returning to gameplay camera...";
                        cam.EnableGameplayCamera();
                        m._status = "Done!";
                        m._showNext = true;
                    }));
                }),

                new("Cinematic Camera: Follow", "The camera will follow you from above. Walk around!\nPress Next when done.", m =>
                {
                    var cam = FindAnyObjectByType<CinematicCamera>();
                    if (cam == null) { m._status = "CinematicCamera not found"; return; }
                    cam.EnableCinematicCamera();
                    cam.FollowTarget(m.explorer.transform, new Vector3(0f, 10f, -7f));
                    m._status = "Following... walk around with WASD!";
                }),

                new("Cinematic Camera: Return", "Returning to first-person view.", m =>
                {
                    var cam = FindAnyObjectByType<CinematicCamera>();
                    cam?.EnableGameplayCamera();
                    m._status = "Back to first person.";
                }),

                // ── Audio ──
                new("Audio: Music", "A test tone will play as background music.", m =>
                {
                    m._showNext = false;
                    m._status = "Playing music...";
                    var clip = CreateToneClip("showcase_music", 220f, 6f);
                    SimpleAudioManager.Instance?.PlayMusic(clip, 0.5f);
                    m._activeRoutine = m.StartCoroutine(m.DelayThen(4f, () =>
                    {
                        m._status = "Stopping music...";
                        SimpleAudioManager.Instance?.StopMusic(1f);
                        m._activeRoutine = m.StartCoroutine(m.DelayThen(1.5f, () =>
                        {
                            m._status = "Done!";
                            m._showNext = true;
                        }));
                    }));
                }),

                new("Audio: SFX", "A short blip sound effect will play.", m =>
                {
                    var clip = CreateToneClip("showcase_sfx", 880f, 0.3f);
                    SimpleAudioManager.Instance?.PlaySFX(clip);
                    m._status = "Blip!";
                }),

                // ── Dialogue ──
                new("Dialogue System", "A dialogue sequence will play with typewriter text.\nPress Space or E to advance lines.", m =>
                {
                    m._showNext = false;
                    m._waitingForInput = true;
                    m._status = "Dialogue playing... press Space/E to advance.";

                    var data = ScriptableObject.CreateInstance<DialogueData>();
                    data.lines = new DialogueLine[]
                    {
                        new() { speakerName = "Mayor", text = "Welcome to Willowbrook! This is our feature showcase.", duration = 2f, autoAdvance = false, speakerColor = new Color(0.2f, 0.6f, 1f) },
                        new() { speakerName = "Mayor", text = "We've built screen effects, cameras, audio, NPCs, and more.", duration = 2f, autoAdvance = false, speakerColor = new Color(0.2f, 0.6f, 1f) },
                        new() { speakerName = "Mayor", text = "Let's keep going!", duration = 2f, autoAdvance = false, speakerColor = new Color(0.2f, 0.6f, 1f) },
                    };

                    var dm = DialogueManager.Instance;
                    if (dm == null) { m._status = "DialogueManager not found"; m._showNext = true; return; }
                    dm.OnDialogueComplete.AddListener(OnDialogueFinished);
                    dm.StartDialogue(data);

                    void OnDialogueFinished()
                    {
                        dm.OnDialogueComplete.RemoveListener(OnDialogueFinished);
                        m._waitingForInput = false;
                        m._status = "Dialogue complete!";
                        m._showNext = true;
                    }
                }),

                // ── NPC ──
                new("NPC Interaction", "An NPC has been spawned nearby. Walk up to them\nand press E when you see 'Press E' above their head.\nPress Next when done.", m =>
                {
                    m._status = "NPC spawned. Walk to them and press E!";
                    if (m._spawnedNpc != null) Destroy(m._spawnedNpc);
                    m.SpawnShowcaseNpc();
                }),

                // ── Mission Manager ──
                new("Mission System", "A mission will start, update its objective, then complete.", m =>
                {
                    m._showNext = false;
                    m._status = "Starting mission...";
                    var mm = MissionManager.Instance;
                    if (mm == null) { m._status = "MissionManager not found"; m._showNext = true; return; }

                    mm.StartMission("Feature Showcase", "Explore the showcase area");
                    m._activeRoutine = m.StartCoroutine(m.DelayThen(2.5f, () =>
                    {
                        m._status = "Updating objective...";
                        mm.UpdateObjective("Complete all feature demos");
                        m._activeRoutine = m.StartCoroutine(m.DelayThen(2.5f, () =>
                        {
                            m._status = "Completing mission...";
                            mm.CompleteMission();
                            m._activeRoutine = m.StartCoroutine(m.DelayThen(4f, () =>
                            {
                                m._status = "Done!";
                                m._showNext = true;
                            }));
                        }));
                    }));
                }),

                // ── Zone Detection ──
                new("Zone Detection", "Walk around and cross into different zones.\nThe Game State Logger overlay shows your current zone.\nPress Next when done.", m =>
                {
                    m._status = "Walk between zones — watch the overlay!";
                    GameStateLogger.Instance?.LogEvent("Showcase: Zone detection demo started");
                }),

                // ── Game State Logger ──
                new("Game State Logger", "The diagnostic overlay is now visible on the right.\nIt shows player position, zone, FPS, and recent events.\nPress Next to continue.", m =>
                {
                    m._status = "Overlay visible on the right side of the screen.";
                    GameStateLogger.Instance?.LogEvent("Showcase: Logger overlay demo");
                }),

                // ── Finale ──
                new("Showcase Complete!", "You've seen every feature built so far!\n\nSystems demonstrated:\n  - Screen Effects (fade, shake, letterbox, objective, mission passed)\n  - Cinematic Camera (fly-over, follow, return)\n  - Audio (music, SFX)\n  - Dialogue (typewriter, speaker names)\n  - NPC Interaction (spawn, face player, talk)\n  - Mission Manager (start, update, complete)\n  - Zone Detection + Game State Logger\n\nPress Esc to quit or keep exploring!", m =>
                {
                    m._showNext = false;
                    m._status = "Thanks for checking out the showcase!";
                    if (m._spawnedNpc != null) Destroy(m._spawnedNpc);
                    ScreenEffects.Instance?.ResetAll();
                }),
            };
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            AdvanceStep();
        }

        private void Update()
        {
            // Allow ESC to unlock cursor for GUI interaction
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                    ? CursorLockMode.None
                    : CursorLockMode.Locked;
                Cursor.visible = Cursor.lockState == CursorLockMode.None;
            }
        }

        private void AdvanceStep()
        {
            if (_activeRoutine != null) { StopCoroutine(_activeRoutine); _activeRoutine = null; }

            _currentStep++;
            if (_currentStep >= _steps.Length) return;

            var step = _steps[_currentStep];
            _title = step.Title;
            _instructions = step.Instructions;
            _status = "";
            _showNext = true;

            step.Execute(this);
            GameStateLogger.Instance?.LogEvent($"Showcase step: {step.Title}");
        }

        private IEnumerator DelayThen(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            action?.Invoke();
        }

        private void SpawnShowcaseNpc()
        {
            Vector3 pos = npcSpawnPoint != null
                ? npcSpawnPoint.position
                : explorer.transform.position + explorer.transform.forward * 5f;

            var root = new GameObject("Showcase_NPC_Mayor");
            root.transform.position = pos;

            // Capsule body
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(root.transform, false);
            capsule.GetComponent<Renderer>().material.color = new Color(0.2f, 0.6f, 1f);

            // Name tag
            var nameGo = new GameObject("NameTag");
            nameGo.transform.SetParent(root.transform, false);
            nameGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            var tm = nameGo.AddComponent<TextMesh>();
            tm.text = "Mayor";
            tm.fontSize = 32;
            tm.characterSize = 0.15f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.2f, 0.6f, 1f);

            // Prompt canvas
            var promptGo = new GameObject("PromptCanvas");
            promptGo.transform.SetParent(root.transform, false);
            promptGo.transform.localPosition = new Vector3(0f, 2.8f, 0f);
            var canvas = promptGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            promptGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            var pRect = promptGo.GetComponent<RectTransform>();
            pRect.sizeDelta = new Vector2(200, 50);
            pRect.localScale = Vector3.one * 0.01f;
            var textGo = new GameObject("PromptText");
            textGo.transform.SetParent(promptGo.transform, false);
            var tRect = textGo.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
            var text = textGo.AddComponent<UnityEngine.UI.Text>();
            text.text = "Press E";
            text.fontSize = 28;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptGo.SetActive(false);

            // Wire NPC controller
            var npc = root.AddComponent<NPCController>();
            var npcType = npc.GetType();
            SetField(npcType, npc, "npcName", "Mayor");
            SetField(npcType, npc, "capsuleColor", new Color(0.2f, 0.6f, 1f));
            SetField(npcType, npc, "interactionRange", 5f);

            var dialogue = ScriptableObject.CreateInstance<DialogueData>();
            dialogue.lines = new DialogueLine[]
            {
                new() { speakerName = "Mayor", text = "Hello there! I'm the Mayor of Willowbrook.", autoAdvance = false, speakerColor = new Color(0.2f, 0.6f, 1f) },
                new() { speakerName = "Mayor", text = "This NPC system lets characters face you, show prompts, and trigger dialogue.", autoAdvance = false, speakerColor = new Color(0.2f, 0.6f, 1f) },
                new() { speakerName = "Mayor", text = "Pretty neat, right? Press Next on the panel to continue the showcase.", autoAdvance = false, speakerColor = new Color(0.2f, 0.6f, 1f) },
            };
            SetField(npcType, npc, "dialogueData", dialogue);

            _spawnedNpc = root;
        }

        private static void SetField(Type type, object obj, string fieldName, object value)
        {
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        private static AudioClip CreateToneClip(string clipName, float frequency, float durationSec)
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
            var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // ── GUI ──────────────────────────────────────────────────

        private void OnGUI()
        {
            if (_currentStep < 0 || _currentStep >= _steps.Length) return;

            float panelW = 400f;
            float panelH = 300f;
            float x = 15f;
            float y = (Screen.height - panelH) / 2f;

            // Semi-transparent panel
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(x, y, panelW, panelH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Gold accent bar
            GUI.color = new Color(0.72f, 0.53f, 0.04f);
            GUI.DrawTexture(new Rect(x, y, 4f, panelH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float pad = 16f;
            float cx = x + pad;
            float cy = y + pad;
            float contentW = panelW - pad * 2;

            // Step counter
            var counterStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11, normal = { textColor = new Color(0.72f, 0.53f, 0.04f) },
                fontStyle = FontStyle.Bold
            };
            GUI.Label(new Rect(cx, cy, contentW, 18f), $"STEP {_currentStep + 1} OF {_steps.Length}", counterStyle);
            cy += 22f;

            // Title
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20, fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }, wordWrap = true
            };
            GUI.Label(new Rect(cx, cy, contentW, 30f), _title, titleStyle);
            cy += 34f;

            // Divider
            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            GUI.DrawTexture(new Rect(cx, cy, contentW, 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            cy += 10f;

            // Instructions
            var instrStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13, normal = { textColor = new Color(0.85f, 0.82f, 0.75f) },
                wordWrap = true, richText = true
            };
            float instrH = instrStyle.CalcHeight(new GUIContent(_instructions), contentW);
            GUI.Label(new Rect(cx, cy, contentW, instrH), _instructions, instrStyle);
            cy += instrH + 10f;

            // Status
            if (!string.IsNullOrEmpty(_status))
            {
                var statusStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12, fontStyle = FontStyle.Italic,
                    normal = { textColor = new Color(0.6f, 0.85f, 0.4f) }
                };
                GUI.Label(new Rect(cx, cy, contentW, 20f), _status, statusStyle);
                cy += 24f;
            }

            // Next button
            if (_showNext && !_waitingForInput)
            {
                float btnW = 120f;
                float btnH = 36f;
                float btnY = y + panelH - btnH - pad;

                // Button background
                GUI.color = new Color(0.72f, 0.53f, 0.04f);
                if (GUI.Button(new Rect(cx, btnY, btnW, btnH), ""))
                {
                    AdvanceStep();
                }
                GUI.color = Color.white;

                var btnStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
                GUI.Label(new Rect(cx, btnY, btnW, btnH),
                    _currentStep < _steps.Length - 1 ? "Next  >" : "Finish", btnStyle);
            }

            // Hint at bottom
            var hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10, normal = { textColor = new Color(1f, 1f, 1f, 0.35f) },
                alignment = TextAnchor.LowerRight
            };
            GUI.Label(new Rect(x + panelW - 200, y + panelH - 25, 185, 18),
                "Esc = toggle mouse cursor", hintStyle);
        }
    }
}
