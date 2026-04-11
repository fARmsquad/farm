using FarmSimVR.Core.Tutorial;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    public sealed class SceneWorkLabelOverlay : MonoBehaviour
    {
        [SerializeField] private bool showOverlay = true;

        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _tagStyle;

        public bool TryGetCurrentScene(out SceneWorkDefinition definition)
        {
            return SceneWorkCatalog.TryGetBySceneName(SceneManager.GetActiveScene().name, out definition);
        }

        private void OnGUI()
        {
            if (!showOverlay || !TryGetCurrentScene(out var scene))
                return;

            BuildStyles();

            var width = 360f;
            var height = scene.HasNextScene ? 116f : 94f;
            var x = Screen.width - width - 24f;
            var y = Screen.height - height - 24f;

            GUI.color = new Color(0.04f, 0.05f, 0.06f, 0.9f);
            GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(x + 14f, y + 12f, width - 28f, 22f), $"{scene.NumberLabel}  {scene.DisplayName}", _titleStyle);
            GUI.Label(new Rect(x + 14f, y + 38f, width - 28f, 18f), KindLabel(scene.Kind), _tagStyle);
            GUI.Label(new Rect(x + 14f, y + 58f, width - 28f, 38f), scene.FocusDescription, _bodyStyle);

            if (!scene.HasNextScene)
                return;

            if (!SceneWorkCatalog.TryGetBySceneName(scene.NextSceneName, out var next))
                return;

            GUI.Label(new Rect(x + 14f, y + height - 22f, width - 28f, 18f), $"Next: {next.NumberLabel}  {next.DisplayName}", _tagStyle);
        }

        private void BuildStyles()
        {
            if (_titleStyle != null)
                return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = false
            };
            _titleStyle.normal.textColor = Color.white;

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true
            };
            _bodyStyle.normal.textColor = new Color(0.88f, 0.92f, 0.9f);

            _tagStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                wordWrap = false
            };
            _tagStyle.normal.textColor = new Color(0.98f, 0.82f, 0.4f);
        }

        private static string KindLabel(SceneWorkKind kind)
        {
            return kind switch
            {
                SceneWorkKind.Cutscene => "CUTSCENE",
                SceneWorkKind.Gameplay => "GAMEPLAY",
                SceneWorkKind.Sandbox => "SANDBOX",
                _ => "SCENE",
            };
        }
    }
}
