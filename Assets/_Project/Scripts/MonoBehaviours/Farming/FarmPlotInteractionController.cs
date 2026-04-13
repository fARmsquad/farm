using System.Collections.Generic;
using FarmSimVR.Core.Farming;
using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Tutorial;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours.Farming
{
    public sealed class FarmPlotInteractionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FarmSimDriver _driver;

        [Header("Detection")]
        [SerializeField] private float _lookDistance = 5f;
        [SerializeField] private float _proximityRadius = 2.2f;

        [Header("Highlight")]
        [SerializeField] private Color _highlightColor = new(1f, 0.85f, 0.2f, 0.55f);
        [SerializeField] private float _highlightPulseSpeed = 1.8f;

        private readonly FarmStageMinigameOverlayPresenter _minigameOverlay = new();

        private Camera _camera;
        private TutorialFarmSceneController _mission;
        private Transform _playerTransform;
        private string _focusedPlotName;
        private FarmPlotActionPrompt _currentPrompt;
        private CropPlotController _focusedController;
        private string _feedbackMessage;
        private float _feedbackUntil;
        private bool _feedbackGood;
        private GameObject _highlightRing;
        private Renderer _highlightRenderer;
        private Material _highlightMat;
        private FarmStageMinigameSession _activeMinigame;
        private float _minigameClosedTime = -1f;
        private const float MinigameInputCooldown = 0.15f;

        /// <summary>True when a stage minigame overlay is open and consuming input.</summary>
        public bool IsMinigameActive => _activeMinigame != null
            || (_minigameClosedTime >= 0f && Time.unscaledTime - _minigameClosedTime < MinigameInputCooldown);

        private string _minigamePlotName;
        private CropPlotController _minigameController;
        private GUIStyle _missionStyle;
        private GUIStyle _primaryKeyStyle;
        private GUIStyle _primaryLabelStyle;
        private GUIStyle _secondaryStyle;
        private GUIStyle _feedbackStyle;
        private bool _stylesReady;

        private void Start()
        {
            if (_driver == null)
                _driver = FindAnyObjectByType<FarmSimDriver>();

            _mission = FindAnyObjectByType<TutorialFarmSceneController>();
            if (_driver != null && _driver.UseThirdPersonRig)
                FarmFirstPersonRigUtility.EnsureThirdPersonRig();
            else
                FarmFirstPersonRigUtility.EnsureRig();
            _camera = Camera.main;
            BuildHighlightRing();
        }

        private void Update()
        {
            if (_driver == null)
                return;

            if (_camera == null)
                _camera = Camera.main;

            if (_activeMinigame != null)
            {
                TickActiveMinigame();
                return;
            }

            UpdateFocus();
            TickHighlight();
            HandlePromptInput();
        }

        private void UpdateFocus()
        {
            _focusedPlotName = null;
            _currentPrompt = null;
            _focusedController = null;

            if (_camera == null)
                return;

            var candidates = new List<FarmPlotFocusCandidate<CropPlotController>>(8);
            var promptsByController = new Dictionary<CropPlotController, FarmPlotActionPrompt>();
            var seen = new HashSet<CropPlotController>();

            var ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out var hit, _lookDistance))
            {
                var hitController = hit.collider.GetComponent<CropPlotController>()
                    ?? hit.collider.GetComponentInParent<CropPlotController>();
                AddFocusCandidate(hitController, -1f, candidates, promptsByController, seen);
            }

            // Use player position for proximity in third-person; camera position otherwise.
            var proximityCenter = ResolveProximityCenter();
            var nearby = Physics.OverlapSphere(proximityCenter, _proximityRadius);
            foreach (var col in nearby)
            {
                var controller = col.GetComponent<CropPlotController>() ?? col.GetComponentInParent<CropPlotController>();
                if (controller == null)
                    continue;

                var distance = Vector3.Distance(proximityCenter, controller.transform.position);
                AddFocusCandidate(controller, distance, candidates, promptsByController, seen);
            }

            var bestController = FarmPlotFocusSelector.ChooseBest(candidates);
            if (bestController != null)
            {
                promptsByController.TryGetValue(bestController, out var prompt);
                AcceptFocus(bestController, prompt);
            }
        }

        private Vector3 ResolveProximityCenter()
        {
            if (_driver != null && _driver.UseThirdPersonRig)
            {
                if (_playerTransform == null)
                {
                    var player = GameObject.FindWithTag("Player");
                    if (player != null)
                        _playerTransform = player.transform;
                }

                if (_playerTransform != null)
                    return _playerTransform.position;
            }

            return _camera.transform.position;
        }

        private void AcceptFocus(CropPlotController controller, FarmPlotActionPrompt prompt)
        {
            _focusedController = controller;
            _focusedPlotName = controller.gameObject.name;
            _currentPrompt = prompt;

            if (_highlightRing != null)
                _highlightRing.transform.position = controller.transform.position + Vector3.up * 0.03f;
        }

        private void AddFocusCandidate(
            CropPlotController controller,
            float distance,
            ICollection<FarmPlotFocusCandidate<CropPlotController>> candidates,
            IDictionary<CropPlotController, FarmPlotActionPrompt> promptsByController,
            ISet<CropPlotController> seen)
        {
            if (controller == null || !seen.Add(controller))
                return;

            var prompt = BuildVisiblePrompt(controller);
            promptsByController[controller] = prompt;
            candidates.Add(new FarmPlotFocusCandidate<CropPlotController>(
                controller,
                distance,
                prompt != null));
        }

        private void BuildHighlightRing()
        {
            _highlightRing = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _highlightRing.name = "PlotHighlightRing";
            Object.Destroy(_highlightRing.GetComponent<MeshCollider>());

            _highlightRing.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            _highlightRing.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

            _highlightMat = new Material(Shader.Find("Sprites/Default"))
            {
                color = _highlightColor,
                renderQueue = 3000,
            };

            _highlightRenderer = _highlightRing.GetComponent<Renderer>();
            _highlightRenderer.material = _highlightMat;
            _highlightRenderer.enabled = false;
        }

        private void TickHighlight()
        {
            if (_focusedController == null)
            {
                if (_highlightRenderer != null)
                    _highlightRenderer.enabled = false;
                return;
            }

            var pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * _highlightPulseSpeed * Mathf.PI);
            var color = _highlightColor;
            color.a = pulse * _highlightColor.a;
            _highlightMat.color = color;
            _highlightRenderer.enabled = true;
        }

        private void HandlePromptInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            // Seed selection via number keys (always active, not gated by prompt)
            if (keyboard.digit1Key.wasPressedThisFrame)
                _driver.SetSelectedSeed(0);
            else if (keyboard.digit2Key.wasPressedThisFrame)
                _driver.SetSelectedSeed(1);
            else if (keyboard.digit3Key.wasPressedThisFrame)
                _driver.SetSelectedSeed(2);

            if (_currentPrompt == null)
                return;

            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                return;

            foreach (var option in _currentPrompt.Actions)
            {
                if (!WasPressed(option.Action, keyboard))
                    continue;

                if (option.Action == FarmPlotAction.PrimaryInteract && TryOpenStageMinigame(_focusedController))
                    return;

                var ok = _driver.TryExecuteAction(_focusedPlotName, option.Action, out var message);
                SetFeedback(ok, ok ? SuccessText(message) : FailureText(message));
                if (ok)
                    _currentPrompt = BuildVisiblePrompt(_focusedController);
                return;
            }
        }

        private void TickActiveMinigame()
        {
            var keyboard = Keyboard.current;
            if (_activeMinigame == null || keyboard == null)
                return;

            _activeMinigame.Tick(Time.deltaTime);

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                SetFeedback(false, "Task cancelled.");
                CloseMinigame();
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                SubmitMinigameInput(FarmStageMinigameInput.Confirm);
            if (keyboard.leftArrowKey.wasPressedThisFrame)
                SubmitMinigameInput(FarmStageMinigameInput.Left);
            if (keyboard.rightArrowKey.wasPressedThisFrame)
                SubmitMinigameInput(FarmStageMinigameInput.Right);
            if (keyboard.upArrowKey.wasPressedThisFrame)
                SubmitMinigameInput(FarmStageMinigameInput.Up);
            if (keyboard.downArrowKey.wasPressedThisFrame)
                SubmitMinigameInput(FarmStageMinigameInput.Down);
        }

        private void SubmitMinigameInput(FarmStageMinigameInput input)
        {
            if (_activeMinigame == null)
                return;

            _activeMinigame.HandleInput(input);
            if (!_activeMinigame.IsComplete)
                return;

            var controller = _minigameController;
            var plotName = _minigamePlotName;
            var ok = _driver.TryCompleteCurrentCropTask(plotName, out var message);
            SetFeedback(ok, ok ? SuccessText(message) : FailureText(message));
            CloseMinigame();
            _currentPrompt = BuildVisiblePrompt(controller ?? _focusedController);
        }

        private bool TryOpenStageMinigame(CropPlotController controller)
        {
            if (controller?.State == null || !controller.State.IsTutorialTaskMode)
                return false;

            var soilStatus = controller.SoilState?.Status ?? PlotStatus.Empty;
            if ((soilStatus == PlotStatus.Empty || soilStatus == PlotStatus.Untilled) && controller.State.Phase == PlotPhase.Empty)
                return false;

            var minigame = controller.State.CurrentMinigame;
            if (controller.State.CurrentTaskId == CropTaskId.None || minigame.Type == FarmStageMinigameType.None)
                return false;

            _activeMinigame = new FarmStageMinigameSession(minigame);
            _minigamePlotName = controller.gameObject.name;
            _minigameController = controller;
            return true;
        }

        private void CloseMinigame()
        {
            _activeMinigame = null;
            _minigamePlotName = null;
            _minigameController = null;
            _minigameClosedTime = Time.unscaledTime;
        }

        private static bool WasPressed(FarmPlotAction action, Keyboard keyboard)
        {
            var mouse = Mouse.current;
            return action switch
            {
                FarmPlotAction.PrimaryInteract => keyboard.eKey.wasPressedThisFrame,
                FarmPlotAction.Till => mouse != null && mouse.leftButton.wasPressedThisFrame,
                FarmPlotAction.PlantSelected => mouse != null && mouse.leftButton.wasPressedThisFrame,
                FarmPlotAction.PlantTomato => keyboard.tKey.wasPressedThisFrame,
                FarmPlotAction.PlantCarrot => keyboard.cKey.wasPressedThisFrame,
                FarmPlotAction.PlantLettuce => keyboard.lKey.wasPressedThisFrame,
                FarmPlotAction.Water => mouse != null && mouse.leftButton.wasPressedThisFrame,
                FarmPlotAction.Harvest => keyboard.eKey.wasPressedThisFrame,
                FarmPlotAction.Compost => keyboard.mKey.wasPressedThisFrame,
                FarmPlotAction.ClearDead => keyboard.xKey.wasPressedThisFrame,
                _ => false,
            };
        }

        private void OnGUI()
        {
            BuildStyles();

            if (_activeMinigame != null)
            {
                _minigameOverlay.Draw(_activeMinigame);
                DrawFeedback();
                return;
            }

            DrawCrosshair();
            if (_currentPrompt != null)
                DrawPanel();
            DrawFeedback();
        }

        private void DrawCrosshair()
        {
            const float size = 7f;
            var centerX = Screen.width * 0.5f;
            var centerY = Screen.height * 0.5f;
            var color = _focusedController != null
                ? new Color(1f, 0.9f, 0.3f, 0.95f)
                : new Color(1f, 1f, 1f, 0.7f);
            GUI.color = color;
            GUI.DrawTexture(new Rect(centerX - size * 0.5f, centerY - 1f, size, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(centerX - 1f, centerY - size * 0.5f, 2f, size), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawPanel()
        {
            var missionAction = _mission?.GetPrimaryAction(_focusedController);
            const float panelWidth = 540f;
            const float panelHeight = 140f;
            var panelX = (Screen.width - panelWidth) * 0.5f;
            var panelY = Screen.height - panelHeight - 44f;

            GUI.color = new Color(0.03f, 0.05f, 0.02f, 0.94f);
            GUI.DrawTexture(new Rect(panelX, panelY, panelWidth, panelHeight), Texture2D.whiteTexture);
            GUI.color = new Color(0.38f, 0.72f, 0.28f, 0.9f);
            GUI.DrawTexture(new Rect(panelX, panelY, 3f, panelHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var rowY = panelY + 13f;
            var missionText = _mission != null ? _mission.CurrentObjectiveText : string.Empty;
            if (!string.IsNullOrEmpty(missionText))
            {
                GUI.Label(new Rect(panelX + 16f, rowY, panelWidth - 32f, 22f), missionText, _missionStyle);
                rowY += 26f;
            }

            GUI.color = new Color(1f, 1f, 1f, 0.10f);
            GUI.DrawTexture(new Rect(panelX + 12f, rowY, panelWidth - 24f, 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            rowY += 9f;

            var chipX = panelX + 16f;
            foreach (var option in _currentPrompt.Actions)
            {
                var isPrimary = missionAction.HasValue && option.Action == missionAction.Value;
                chipX = DrawActionChip(chipX, rowY, option.KeyLabel, option.Label, isPrimary) + 12f;
            }

            // Seed selection indicator
            rowY += 34f;
            var seedNames = new[] { "Tomato", "Carrot", "Lettuce" };
            var selectedIdx = _driver != null ? _driver.SelectedSeedIndex : 0;
            var seedChipX = panelX + 16f;
            for (var i = 0; i < seedNames.Length; i++)
            {
                var isSelected = i == selectedIdx;
                seedChipX = DrawActionChip(seedChipX, rowY, (i + 1).ToString(), seedNames[i], isSelected) + 8f;
            }
        }

        private float DrawActionChip(float x, float y, string key, string label, bool primary)
        {
            const float keyWidth = 28f;
            const float keyHeight = 26f;
            const float labelPadding = 10f;

            var labelStyle = primary ? _primaryLabelStyle : _secondaryStyle;
            var labelWidth = labelStyle.CalcSize(new GUIContent(label)).x + labelPadding;
            var chipWidth = keyWidth + 6f + labelWidth;

            if (primary)
            {
                GUI.color = new Color(0.95f, 0.82f, 0.15f, 1f);
                GUI.DrawTexture(new Rect(x, y, keyWidth, keyHeight), Texture2D.whiteTexture);
                GUI.color = new Color(0.08f, 0.06f, 0f, 1f);
                GUI.Label(new Rect(x, y + 2f, keyWidth, keyHeight - 4f), key, _primaryKeyStyle);
                GUI.color = Color.white;
                GUI.Label(new Rect(x + keyWidth + 5f, y + 2f, labelWidth, keyHeight - 4f), label, _primaryLabelStyle);
                return x + chipWidth;
            }

            GUI.color = new Color(0.25f, 0.25f, 0.25f, 0.85f);
            GUI.DrawTexture(new Rect(x, y, keyWidth, keyHeight), Texture2D.whiteTexture);
            GUI.color = new Color(0.72f, 0.72f, 0.72f, 1f);
            GUI.Label(new Rect(x, y + 2f, keyWidth, keyHeight - 4f), key, _primaryKeyStyle);
            GUI.color = new Color(0.58f, 0.58f, 0.58f, 1f);
            GUI.Label(new Rect(x + keyWidth + 5f, y + 2f, labelWidth, keyHeight - 4f), label, _secondaryStyle);
            GUI.color = Color.white;
            return x + chipWidth;
        }

        private void DrawFeedback()
        {
            if (string.IsNullOrEmpty(_feedbackMessage) || Time.time > _feedbackUntil)
                return;

            var alpha = Mathf.Clamp01((_feedbackUntil - Time.time) / 0.5f);
            const float width = 340f;
            var x = (Screen.width - width) * 0.5f;
            var y = Screen.height - 160f;

            GUI.color = _feedbackGood
                ? new Color(0.05f, 0.3f, 0.05f, 0.88f * alpha)
                : new Color(0.3f, 0.08f, 0.05f, 0.88f * alpha);
            GUI.DrawTexture(new Rect(x, y, width, 34f), Texture2D.whiteTexture);

            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(x + 12f, y + 7f, width - 24f, 22f), _feedbackMessage, _feedbackStyle);
            GUI.color = Color.white;
        }

        private FarmPlotActionPrompt BuildVisiblePrompt(CropPlotController controller)
        {
            if (controller == null || _driver == null)
                return null;
            _mission?.EnsureHeroPlotConfigured();
            var prompt = _driver.BuildPrompt(controller.gameObject.name);
            if (prompt == null || _mission == null || _mission.IsComplete)
                return prompt;

            var filtered = new List<FarmPlotActionOption>(prompt.Actions.Count);
            foreach (var option in prompt.Actions)
            {
                if (_mission.IsActionAllowed(option.Action, controller))
                    filtered.Add(option);
            }

            return filtered.Count == 0
                ? null
                : new FarmPlotActionPrompt(prompt.Title, prompt.Detail, filtered);
        }

        private void BuildStyles()
        {
            if (_stylesReady)
                return;

            _missionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _missionStyle.normal.textColor = new Color(0.98f, 0.93f, 0.72f);

            _primaryKeyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _primaryLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _primaryLabelStyle.normal.textColor = Color.white;

            _secondaryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };

            _feedbackStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _feedbackStyle.normal.textColor = Color.white;

            _stylesReady = true;
        }

        private void SetFeedback(bool success, string message)
        {
            _feedbackGood = success;
            _feedbackMessage = message;
            _feedbackUntil = Time.time + 2f;
        }

        private static string SuccessText(string raw)
        {
            return string.IsNullOrWhiteSpace(raw) ? "Done." : raw;
        }

        private static string FailureText(string raw)
        {
            return string.IsNullOrWhiteSpace(raw) ? "Not ready." : raw;
        }

        private void OnDestroy()
        {
            if (_highlightMat != null)
                Object.Destroy(_highlightMat);
            if (_highlightRing != null)
                Object.Destroy(_highlightRing);
        }
    }
}
