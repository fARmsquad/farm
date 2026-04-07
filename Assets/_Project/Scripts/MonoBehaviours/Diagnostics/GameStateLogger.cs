using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FarmSimVR.MonoBehaviours.Hunting;

namespace FarmSimVR.MonoBehaviours.Diagnostics
{
    /// <summary>
    /// [DEV ONLY] Logs a structured text snapshot of the entire game state
    /// to a file that can be read by Claude via MCP.
    ///
    /// Press Shift+5 to toggle the on-screen overlay.
    /// Press Shift+6 to force-write a snapshot immediately.
    /// Snapshots auto-write every snapshotInterval seconds.
    /// Overlay is hidden by default — dev feature only.
    ///
    /// Output file: Assets/_Project/Logs/gamestate.log
    /// </summary>
    public class GameStateLogger : MonoBehaviour
    {
        [Header("Logging")]
        [SerializeField] private float snapshotInterval = 2f;
        [SerializeField] private string logFileName = "gamestate.log";
        [SerializeField] private int maxEventHistory = 50;

        [Header("Overlay")]
        [SerializeField] private bool showOverlay = false;
        [SerializeField] private int overlayFontSize = 14;

        private float _snapshotTimer;
        private string _logPath;
        private string _latestSnapshot = "";
        private readonly List<string> _eventLog = new();
        private Vector2 _scrollPos;

        // Singleton for easy access from other scripts
        public static GameStateLogger Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            string logDir = Path.Combine(Application.dataPath, "_Project/Logs");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            _logPath = Path.Combine(logDir, logFileName);

            LogEvent("GameStateLogger initialized");
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.leftShiftKey.isPressed)
            {
                if (kb.digit5Key.wasPressedThisFrame) showOverlay = !showOverlay;
                if (kb.digit6Key.wasPressedThisFrame) WriteSnapshot();
            }

            _snapshotTimer -= Time.deltaTime;
            if (_snapshotTimer <= 0f)
            {
                WriteSnapshot();
                _snapshotTimer = snapshotInterval;
            }
        }

        /// <summary>
        /// Call this from any script to log a game event.
        /// Example: GameStateLogger.Instance?.LogEvent("Player caught a Chicken");
        /// </summary>
        public void LogEvent(string message)
        {
            string entry = $"[{Time.time:F1}s] {message}";
            _eventLog.Add(entry);
            if (_eventLog.Count > maxEventHistory)
                _eventLog.RemoveAt(0);
            UnityEngine.Debug.Log($"[GameState] {message}");
        }

        private void WriteSnapshot()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== GAME STATE SNAPSHOT === Time: {Time.time:F1}s Frame: {Time.frameCount}");
            sb.AppendLine($"FPS: {1f / Time.deltaTime:F0} | Delta: {Time.deltaTime * 1000:F1}ms");
            sb.AppendLine();

            // Player
            WritePlayerState(sb);
            sb.AppendLine();

            // Hunting system
            WriteHuntingState(sb);
            sb.AppendLine();

            // Animal Pen
            WritePenState(sb);
            sb.AppendLine();

            // Wild Animals
            WriteWildAnimals(sb);
            sb.AppendLine();

            // Recent Events
            sb.AppendLine("--- RECENT EVENTS (newest last) ---");
            int start = Mathf.Max(0, _eventLog.Count - 15);
            for (int i = start; i < _eventLog.Count; i++)
                sb.AppendLine(_eventLog[i]);

            _latestSnapshot = sb.ToString();

            try
            {
                File.WriteAllText(_logPath, _latestSnapshot);
            }
            catch (IOException) { /* file locked, skip this write */ }
        }

        private void WritePlayerState(StringBuilder sb)
        {
            sb.AppendLine("--- PLAYER ---");
            var player = FindAnyObjectByType<PlayerMovement>();
            if (player == null) { sb.AppendLine("  (not found)"); return; }

            var t = player.transform;
            sb.AppendLine($"  Position: ({t.position.x:F1}, {t.position.y:F1}, {t.position.z:F1})");
            sb.AppendLine($"  Forward:  ({t.forward.x:F1}, {t.forward.y:F1}, {t.forward.z:F1})");

            var cc = player.GetComponent<CharacterController>();
            if (cc != null)
                sb.AppendLine($"  Grounded: {cc.isGrounded} | Velocity: {cc.velocity.magnitude:F1}");

            var input = player.GetComponent<KeyboardPlayerInput>();
            if (input != null)
                sb.AppendLine($"  CatchPressed: {input.CatchPressed}");
        }

        private void WriteHuntingState(StringBuilder sb)
        {
            sb.AppendLine("--- HUNTING ---");
            var mgr = FindAnyObjectByType<HuntingManager>();
            if (mgr == null) { sb.AppendLine("  (HuntingManager not found)"); return; }

            var spawner = FindAnyObjectByType<WildAnimalSpawner>();
            if (spawner != null)
                sb.AppendLine($"  Wild Animals Active: {spawner.ActiveCount}");

            // Read tracker state via HUD (it has the references)
            var hud = FindAnyObjectByType<HuntingHUD>();
            // We can't directly access the tracker, but the HUD displays the counts
            // So let's find BarnDropOff and check if we can read
            sb.AppendLine($"  (Carrying/Barn counts visible in HUD overlay)");
        }

        private void WritePenState(StringBuilder sb)
        {
            sb.AppendLine("--- ANIMAL PEN ---");
            var pen = FindAnyObjectByType<AnimalPen>();
            if (pen == null) { sb.AppendLine("  (not found)"); return; }

            sb.AppendLine($"  Center: ({pen.PenCenter.x:F1}, {pen.PenCenter.y:F1}, {pen.PenCenter.z:F1}) Radius: {pen.PenRadius:F1}");
            sb.AppendLine($"  Animals in pen: {pen.PenAnimals.Count}");

            foreach (var animal in pen.PenAnimals)
            {
                if (animal == null) continue;
                var pos = animal.transform.position;
                sb.AppendLine($"    [{animal.AnimalType}] at ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})");
            }
        }

        private void WriteWildAnimals(StringBuilder sb)
        {
            sb.AppendLine("--- WILD ANIMALS ---");
            var fleers = FindObjectsByType<AnimalFleeBehavior>(FindObjectsSortMode.None);
            if (fleers.Length == 0) { sb.AppendLine("  (none)"); return; }

            foreach (var flee in fleers)
            {
                if (flee == null) continue;
                var pos = flee.transform.position;
                var catchZone = flee.GetComponent<CatchZone>();
                string type = catchZone != null ? catchZone.AnimalType.ToString() : "Unknown";
                sb.AppendLine($"  [{type}] at ({pos.x:F1}, {pos.y:F1}, {pos.z:F1}) fleeing={flee.IsFleeing}");
            }
        }

        private void OnGUI()
        {
            if (!showOverlay || string.IsNullOrEmpty(_latestSnapshot)) return;

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = overlayFontSize,
                alignment = TextAnchor.UpperLeft,
                wordWrap = false
            };

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = overlayFontSize,
                richText = false
            };

            float width = 380;
            float height = Screen.height - 20;
            Rect area = new Rect(Screen.width - width - 10, 10, width, height);

            GUI.Box(area, "");
            GUILayout.BeginArea(new Rect(area.x + 5, area.y + 5, area.width - 10, area.height - 10));
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            GUILayout.Label(_latestSnapshot, labelStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // Toggle hint
            GUI.Label(new Rect(Screen.width - 250, Screen.height - 25, 250, 20),
                "[DEV] Shift+5=Toggle | Shift+6=Snapshot", new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.LowerRight });
        }
    }
}
