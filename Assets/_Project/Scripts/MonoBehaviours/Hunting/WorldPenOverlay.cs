using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    public sealed class WorldPenOverlay : MonoBehaviour
    {
        [SerializeField] private bool showOverlay = true;
        [SerializeField] private WorldPenGameController gameController;
        [SerializeField] private WorldPenProgressionController progressionController;
        [SerializeField] private WorldPenDevShortcuts shortcuts;

        private GUIStyle _title;
        private GUIStyle _body;
        private GUIStyle _accent;

        public void Configure(
            WorldPenGameController controller,
            WorldPenProgressionController progression,
            WorldPenDevShortcuts devShortcuts)
        {
            gameController = controller;
            progressionController = progression;
            shortcuts = devShortcuts;
        }

        private void Update()
        {
            if (gameController == null)
                gameController = GetComponent<WorldPenGameController>() ?? FindAnyObjectByType<WorldPenGameController>();

            if (progressionController == null)
                progressionController = GetComponent<WorldPenProgressionController>() ?? FindAnyObjectByType<WorldPenProgressionController>();

            if (shortcuts == null)
                shortcuts = GetComponent<WorldPenDevShortcuts>() ?? FindAnyObjectByType<WorldPenDevShortcuts>();
        }

        private void OnGUI()
        {
            if (!showOverlay)
                return;

            BuildStyles();
            DrawPanel();
            DrawPrompt();
            DrawStatus();
        }

        private void DrawPanel()
        {
            var rect = new Rect(Screen.width - 360f, 20f, 340f, 190f);
            GUI.color = new Color(0.08f, 0.06f, 0.03f, 0.88f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, rect.height - 20f));
            GUILayout.Label("World Pen Game", _title);
            GUILayout.Label(BuildProgressionLine(), _body);
            GUILayout.Label(BuildSessionLine(), _body);
            GUILayout.Space(6f);
            GUILayout.Label("Controls", _accent);
            GUILayout.Label("G start / stop pen game  |  E catch  |  walk animals to the gate", _body);
            GUILayout.Label($"{WorldPenDevShortcuts.ExperienceShortcutLabel} +100 pen XP  |  {WorldPenDevShortcuts.SkillShortcutLabel} spend Animal Handling point", _body);
            GUILayout.EndArea();
        }

        private void DrawPrompt()
        {
            if (gameController == null || !gameController.IsPromptVisible)
                return;

            var width = 440f;
            var x = (Screen.width - width) * 0.5f;
            var y = Screen.height - 120f;
            GUI.color = new Color(0f, 0f, 0f, 0.74f);
            GUI.DrawTexture(new Rect(x, y, width, 54f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 16f, y + 10f, width - 32f, 30f), "Animal Pen ready. Press G if you want to play the pen game.", _accent);
        }

        private void DrawStatus()
        {
            var status = shortcuts != null && !string.IsNullOrEmpty(shortcuts.StatusMessage)
                ? shortcuts.StatusMessage
                : progressionController != null && !string.IsNullOrEmpty(progressionController.StatusMessage)
                    ? progressionController.StatusMessage
                    : gameController?.StatusMessage;

            if (string.IsNullOrEmpty(status))
                return;

            var width = 420f;
            var x = (Screen.width - width) * 0.5f;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(x, 76f, width, 32f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 12f, 80f, width - 24f, 24f), status, _body);
        }

        private string BuildProgressionLine()
        {
            if (progressionController?.Service == null)
                return "Pen progression loading...";

            var state = progressionController.Service.State;
            return $"Level: {state.Level}  |  XP: {state.Experience}  |  Skill Points: {state.SkillPoints}  |  Handling Rank: {state.AnimalHandlingRank}";
        }

        private string BuildSessionLine()
        {
            if (gameController == null)
                return "Pen session unavailable.";

            var state = gameController.IsGameActive ? "Active" : "Idle";
            return $"State: {state}  |  Wild: {gameController.WildCount}  |  Carrying: {gameController.CarriedCount}  |  Deposited: {gameController.DepositedCount}";
        }

        private void BuildStyles()
        {
            if (_title != null)
                return;

            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            _title.normal.textColor = Color.white;

            _body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true
            };
            _body.normal.textColor = new Color(0.93f, 0.9f, 0.84f);

            _accent = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            _accent.normal.textColor = new Color(0.98f, 0.85f, 0.55f);
        }
    }
}
