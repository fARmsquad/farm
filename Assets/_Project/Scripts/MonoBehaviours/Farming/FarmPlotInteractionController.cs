using FarmSimVR.Core.Farming;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours.Farming
{
    public sealed class FarmPlotInteractionController : MonoBehaviour
    {
        [SerializeField] private FarmSimDriver _driver;
        [SerializeField] private float _interactionDistance = 4f;

        private Camera _camera;
        private string _focusedPlotName;
        private FarmPlotActionPrompt _currentPrompt;
        private string _feedbackMessage;
        private float _feedbackUntil;

        private GUIStyle _titleStyle;
        private GUIStyle _detailStyle;
        private GUIStyle _actionStyle;
        private GUIStyle _feedbackStyle;
        private bool _stylesReady;

        private void Start()
        {
            if (_driver == null)
                _driver = FindAnyObjectByType<FarmSimDriver>();

            FarmFirstPersonRigUtility.EnsureRig();
            _camera = Camera.main;
        }

        private void Update()
        {
            if (_driver == null)
                return;

            if (_camera == null)
                _camera = Camera.main;

            UpdateFocus();
            HandleInput();
        }

        private void UpdateFocus()
        {
            _focusedPlotName = null;
            _currentPrompt = null;

            if (_camera == null)
                return;

            var ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(ray, out var hit, _interactionDistance))
                return;

            var plot = hit.collider.GetComponent<CropPlotController>();
            if (plot == null)
                plot = hit.collider.GetComponentInParent<CropPlotController>();
            if (plot == null)
                return;

            _focusedPlotName = plot.gameObject.name;
            _currentPrompt = _driver.BuildPrompt(_focusedPlotName);
        }

        private void HandleInput()
        {
            if (_currentPrompt == null)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (IsShiftPressed(keyboard))
                return;

            foreach (var option in _currentPrompt.Actions)
            {
                if (!WasPressed(option.Action, keyboard))
                    continue;

                if (_driver.TryExecuteAction(_focusedPlotName, option.Action, out var message))
                {
                    SetFeedback(message);
                    _currentPrompt = _driver.BuildPrompt(_focusedPlotName);
                }
                else
                {
                    SetFeedback(message);
                }

                break;
            }
        }

        private static bool WasPressed(FarmPlotAction action, Keyboard keyboard)
        {
            return action switch
            {
                FarmPlotAction.PlantTomato => keyboard.tKey.wasPressedThisFrame,
                FarmPlotAction.PlantCarrot => keyboard.cKey.wasPressedThisFrame,
                FarmPlotAction.PlantLettuce => keyboard.lKey.wasPressedThisFrame,
                FarmPlotAction.Water => keyboard.pKey.wasPressedThisFrame,
                FarmPlotAction.Harvest => keyboard.hKey.wasPressedThisFrame,
                FarmPlotAction.Compost => keyboard.mKey.wasPressedThisFrame,
                _ => false
            };
        }

        private static bool IsShiftPressed(Keyboard keyboard)
        {
            return keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        }

        private void SetFeedback(string message)
        {
            _feedbackMessage = message;
            _feedbackUntil = Time.time + 2.5f;
        }

        private void OnGUI()
        {
            BuildStyles();
            DrawCrosshair();
            DrawPrompt();
            DrawFeedback();
        }

        private void DrawCrosshair()
        {
            const float size = 6f;
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.DrawTexture(new Rect(cx - size * 0.5f, cy - 1f, size, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1f, cy - size * 0.5f, 2f, size), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawPrompt()
        {
            if (_currentPrompt == null)
                return;

            float width = 540f;
            float height = 140f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height - height - 36f;

            GUI.color = new Color(0.06f, 0.08f, 0.05f, 0.9f);
            GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(x + 16f, y + 12f, width - 32f, height - 24f));
            GUILayout.Label(_currentPrompt.Title, _titleStyle);
            GUILayout.Label(_currentPrompt.Detail, _detailStyle);
            GUILayout.Space(6f);

            GUILayout.BeginHorizontal();
            foreach (var action in _currentPrompt.Actions)
            {
                GUILayout.Label($"[{action.KeyLabel}] {action.Label}", _actionStyle);
                GUILayout.Space(14f);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawFeedback()
        {
            if (string.IsNullOrEmpty(_feedbackMessage) || Time.time > _feedbackUntil)
                return;

            float width = 420f;
            float x = (Screen.width - width) * 0.5f;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(x, 28f, width, 34f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 12f, 34f, width - 24f, 24f), _feedbackMessage, _feedbackStyle);
        }

        private void BuildStyles()
        {
            if (_stylesReady)
                return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                richText = true
            };
            _titleStyle.normal.textColor = Color.white;

            _detailStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                richText = true
            };
            _detailStyle.normal.textColor = new Color(0.82f, 0.9f, 0.8f);

            _actionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                richText = true
            };
            _actionStyle.normal.textColor = new Color(1f, 0.92f, 0.65f);

            _feedbackStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            _feedbackStyle.normal.textColor = Color.white;

            _stylesReady = true;
        }
    }
}
