using FarmSimVR.Core.Tutorial;
using FarmSimVR.MonoBehaviours.Farming;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    public sealed class TutorialHorseTrainingSceneController : MonoBehaviour
    {
        private const float TreatPickupRadius = 1.35f;
        private const float JumpWindowZ = 1.15f;
        private const float JumpWindowX = 1.75f;
        private const float SlalomGateHalfWidth = 1.4f;
        private const float SlalomPassDepth = 0.9f;
        private const float HorseFollowSpeed = 2.8f;

        private HorseTrainingService _training;
        private Transform _player;
        private Transform _horse;
        private GameObject[] _treatMarkers;
        private GameObject[] _jumpRails;
        private GameObject[] _slalomGates;
        private int _nextSlalomGateIndex;
        private string _feedbackMessage;
        private float _feedbackUntil;

        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _hintStyle;
        private bool _stylesReady;

        private void Start()
        {
            _training = new HorseTrainingService();
            var sceneObjects = HorseTrainingSceneLayout.Ensure();

            var rig = FarmFirstPersonRigUtility.EnsureRig();
            _player = rig != null ? rig.transform : null;
            _horse = sceneObjects.Horse;
            _treatMarkers = sceneObjects.TreatMarkers;
            _jumpRails = sceneObjects.JumpRails;
            _slalomGates = sceneObjects.SlalomGates;

            var camera = Camera.main;
            if (camera != null)
                camera.backgroundColor = new Color(0.76f, 0.86f, 0.9f);

            RefreshCourseObjects();
            SetFeedback("Horse training ready. Press Enter to begin.");
        }

        private void Update()
        {
            CachePlayer();
            HandleGlobalInput();
            UpdateHorseFollow();

            switch (_training.Snapshot.Step)
            {
                case HorseTrainingStep.Setup:
                    HandleBeginInput();
                    break;
                case HorseTrainingStep.GuidedWalk:
                    HandleTreatMarkers();
                    break;
                case HorseTrainingStep.Jumping:
                    HandleJumpRails();
                    break;
                case HorseTrainingStep.Slalom:
                    HandleSlalom();
                    break;
            }
        }

        private void OnGUI()
        {
            BuildStyles();
            DrawStatusPanel();
            DrawFeedbackBanner();
        }

        private void CachePlayer()
        {
            if (_player != null)
                return;

            var rig = FindAnyObjectByType<FirstPersonExplorer>();
            if (rig != null)
                _player = rig.transform;
        }

        private void HandleGlobalInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (_training.Snapshot.Step == HorseTrainingStep.Success || _training.Snapshot.Step == HorseTrainingStep.Failure)
            {
                if (keyboard.rKey.wasPressedThisFrame)
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    return;
                }

                if (keyboard.escapeKey.wasPressedThisFrame)
                    SceneManager.LoadScene(TutorialSceneCatalog.TitleScreenSceneName);
            }
        }

        private void HandleBeginInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (!keyboard.enterKey.wasPressedThisFrame && !keyboard.spaceKey.wasPressedThisFrame)
                return;

            _training.Begin();
            RefreshCourseObjects();
            SetFeedback("Guide the horse through the treat markers.");
        }

        private void HandleTreatMarkers()
        {
            if (_player == null)
                return;

            var nextIndex = _training.Snapshot.TreatMarkersCleared;
            if (nextIndex < 0 || nextIndex >= HorseTrainingSceneLayout.TreatMarkerPositions.Length)
                return;

            if (Vector3.Distance(_player.position, HorseTrainingSceneLayout.TreatMarkerPositions[nextIndex]) > TreatPickupRadius)
                return;

            if (_treatMarkers != null && nextIndex < _treatMarkers.Length && _treatMarkers[nextIndex] != null)
                _treatMarkers[nextIndex].SetActive(false);

            var snapshot = _training.RecordTreatMarkerReached();
            RefreshCourseObjects();
            SetFeedback(snapshot.Step == HorseTrainingStep.Jumping
                ? "Jumping test unlocked. Press E at each rail."
                : $"Treat marker {snapshot.TreatMarkersCleared}/{snapshot.RequiredTreatMarkers}.");
        }

        private void HandleJumpRails()
        {
            if (_player == null)
                return;

            var nextIndex = _training.Snapshot.JumpRailsCleared;
            if (nextIndex < 0 || nextIndex >= HorseTrainingSceneLayout.JumpRailPositions.Length)
                return;

            var target = HorseTrainingSceneLayout.JumpRailPositions[nextIndex];
            var keyboard = Keyboard.current;
            if (keyboard != null &&
                keyboard.eKey.wasPressedThisFrame &&
                Mathf.Abs(_player.position.z - target.z) <= JumpWindowZ &&
                Mathf.Abs(_player.position.x - target.x) <= JumpWindowX)
            {
                if (_jumpRails != null && nextIndex < _jumpRails.Length && _jumpRails[nextIndex] != null)
                    _jumpRails[nextIndex].SetActive(false);

                var snapshot = _training.RecordJumpRailCleared();
                _nextSlalomGateIndex = 0;
                RefreshCourseObjects();
                SetFeedback(snapshot.Step == HorseTrainingStep.Slalom
                    ? "Balance test live. Thread the slalom gates."
                    : $"Jump rail {snapshot.JumpRailsCleared}/{snapshot.RequiredJumpRails} cleared.");
                return;
            }

            if (_player.position.z <= target.z + JumpWindowZ)
                return;

            _training.RecordJumpMissed();
            RefreshCourseObjects();
            SetFeedback("Training failure. You missed the jump cue.");
        }

        private void HandleSlalom()
        {
            if (_player == null || _nextSlalomGateIndex >= HorseTrainingSceneLayout.SlalomGatePositions.Length)
                return;

            var target = HorseTrainingSceneLayout.SlalomGatePositions[_nextSlalomGateIndex];
            if (_player.position.z < target.z - SlalomPassDepth)
                return;

            bool passedGate = Mathf.Abs(_player.position.x - target.x) <= SlalomGateHalfWidth;
            if (_slalomGates != null && _nextSlalomGateIndex < _slalomGates.Length && _slalomGates[_nextSlalomGateIndex] != null)
                _slalomGates[_nextSlalomGateIndex].SetActive(false);

            var snapshot = passedGate
                ? _training.RecordSlalomGateCleared()
                : _training.RecordSlalomMiss();

            _nextSlalomGateIndex++;
            RefreshCourseObjects();

            if (snapshot.Step == HorseTrainingStep.Success)
            {
                SetFeedback("Training complete. The horse is calm and ready.");
                return;
            }

            if (snapshot.Step == HorseTrainingStep.Failure)
            {
                SetFeedback("Training failure. The slalom balance broke down.");
                return;
            }

            SetFeedback(passedGate
                ? $"Slalom gate {snapshot.SlalomGatesCleared}/{snapshot.RequiredSlalomGates} cleared."
                : $"Balance slipping. {snapshot.BalanceNormalized:P0} remaining.");
        }

        private void UpdateHorseFollow()
        {
            if (_horse == null || _player == null)
                return;

            var snapshot = _training.Snapshot;
            Vector3 target;
            if (snapshot.Step == HorseTrainingStep.Setup || snapshot.Step == HorseTrainingStep.Failure)
                target = new Vector3(-2f, 1f, -7f);
            else
                target = _player.position + new Vector3(-1.3f, 0.9f, -2.1f);

            _horse.position = Vector3.Lerp(_horse.position, target, Time.deltaTime * HorseFollowSpeed);
            var lookTarget = new Vector3(_player.position.x, _horse.position.y, _player.position.z + 2f);
            var forward = lookTarget - _horse.position;
            if (forward.sqrMagnitude > 0.01f)
                _horse.rotation = Quaternion.Lerp(_horse.rotation, Quaternion.LookRotation(forward.normalized), Time.deltaTime * 4f);
        }

        private void RefreshCourseObjects()
        {
            var snapshot = _training.Snapshot;
            UpdateVisibility(_treatMarkers, snapshot.Step == HorseTrainingStep.GuidedWalk, snapshot.TreatMarkersCleared);
            UpdateVisibility(_jumpRails, snapshot.Step == HorseTrainingStep.Jumping, snapshot.JumpRailsCleared);
            UpdateVisibility(_slalomGates, snapshot.Step == HorseTrainingStep.Slalom, _nextSlalomGateIndex);
        }

        private static void UpdateVisibility(GameObject[] objects, bool activePhase, int clearedCount)
        {
            if (objects == null)
                return;

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null)
                    continue;

                objects[i].SetActive(activePhase && i >= clearedCount);
            }
        }

        private void DrawStatusPanel()
        {
            var snapshot = _training.Snapshot;
            switch (snapshot.Step)
            {
                case HorseTrainingStep.Setup:
                    DrawSetupPanel();
                    return;
                case HorseTrainingStep.Success:
                case HorseTrainingStep.Failure:
                    DrawResolutionPanel(snapshot);
                    return;
            }

            GUI.color = new Color(0.05f, 0.05f, 0.06f, 0.84f);
            GUI.DrawTexture(new Rect(18f, 18f, 400f, 186f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(34f, 32f, 360f, 30f), "HORSE TRAINING COURSE", _titleStyle);
            GUI.Label(new Rect(34f, 66f, 360f, 44f), ObjectiveText(snapshot), _bodyStyle);
            DrawProgressBar(new Rect(34f, 122f, 332f, 18f), "Jumping", snapshot.JumpRailsCleared, snapshot.RequiredJumpRails, new Color(0.92f, 0.74f, 0.28f));
            DrawProgressBar(new Rect(34f, 150f, 332f, 18f), "Slalom", snapshot.SlalomGatesCleared, snapshot.RequiredSlalomGates, new Color(0.96f, 0.84f, 0.32f));
            DrawFloatBar(new Rect(34f, 178f, 332f, 18f), "Balance", snapshot.BalanceNormalized, new Color(0.37f, 0.75f, 0.91f));
        }

        private void DrawSetupPanel()
        {
            var width = Mathf.Min(780f, Screen.width - 80f);
            var x = (Screen.width - width) * 0.5f;
            GUI.color = new Color(0.04f, 0.04f, 0.05f, 0.9f);
            GUI.DrawTexture(new Rect(x, 38f, width, 246f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(x + 24f, 58f, width - 48f, 34f), "HORSE TRAINING GROUNDS", _titleStyle);
            GUI.Label(
                new Rect(x + 24f, 104f, width - 48f, 112f),
                "BEGIN\n\n1. Guide the horse with treats.\n2. Clear the jumping rails with E.\n3. Weave the slalom and keep your balance.",
                _bodyStyle);
            GUI.Label(new Rect(x + 24f, 226f, width - 48f, 26f), "Press Enter or Space to begin the slice.", _hintStyle);
        }

        private void DrawResolutionPanel(HorseTrainingSnapshot snapshot)
        {
            var width = 520f;
            var x = (Screen.width - width) * 0.5f;
            var title = snapshot.Step == HorseTrainingStep.Success ? "TRAINING COMPLETE!" : "TRAINING FAILURE!";
            var body = snapshot.Step == HorseTrainingStep.Success
                ? "Jumping and slalom are both clear. The horse settled into the course."
                : snapshot.FailureReason == HorseTrainingFailureReason.FailedJump
                    ? "The jump timing slipped. Retry and press E when you meet the rail."
                    : "The slalom line broke down. Retry and stay inside each gate.";

            GUI.color = new Color(0.06f, 0.05f, 0.05f, 0.9f);
            GUI.DrawTexture(new Rect(x, 54f, width, 220f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(x + 24f, 74f, width - 48f, 30f), title, _titleStyle);
            GUI.Label(new Rect(x + 24f, 116f, width - 48f, 74f), body, _bodyStyle);
            GUI.Label(new Rect(x + 24f, 204f, width - 48f, 26f), "Press R to retry or Escape to return to TitleScreen.", _hintStyle);
        }

        private void DrawFeedbackBanner()
        {
            if (string.IsNullOrWhiteSpace(_feedbackMessage) || Time.time > _feedbackUntil)
                return;

            var width = 460f;
            var x = (Screen.width - width) * 0.5f;
            GUI.color = new Color(0f, 0f, 0f, 0.68f);
            GUI.DrawTexture(new Rect(x, Screen.height - 64f, width, 34f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 12f, Screen.height - 60f, width - 24f, 24f), _feedbackMessage, _hintStyle);
        }

        private void DrawProgressBar(Rect rect, string label, int value, int max, Color fillColor)
        {
            GUI.Label(new Rect(rect.x, rect.y - 16f, rect.width, 14f), label, _hintStyle);
            var progress = max <= 0 ? 0f : Mathf.Clamp01((float)value / max);
            DrawBar(rect, progress, fillColor);
        }

        private void DrawFloatBar(Rect rect, string label, float value, Color fillColor)
        {
            GUI.Label(new Rect(rect.x, rect.y - 16f, rect.width, 14f), label, _hintStyle);
            DrawBar(rect, Mathf.Clamp01(value), fillColor);
        }

        private static void DrawBar(Rect rect, float normalized, Color fillColor)
        {
            GUI.color = new Color(0.14f, 0.15f, 0.17f, 1f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = fillColor;
            GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * normalized, rect.height - 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static string ObjectiveText(HorseTrainingSnapshot snapshot)
        {
            return snapshot.Step switch
            {
                HorseTrainingStep.GuidedWalk => $"Guide the horse with treats {snapshot.TreatMarkersCleared}/{snapshot.RequiredTreatMarkers}.",
                HorseTrainingStep.Jumping => "Press E when you reach each jump rail.",
                HorseTrainingStep.Slalom => $"Thread the slalom gates. Balance {snapshot.BalanceNormalized:P0}.",
                _ => string.Empty,
            };
        }

        private void SetFeedback(string message)
        {
            _feedbackMessage = message;
            _feedbackUntil = Time.time + 2.5f;
        }

        private void BuildStyles()
        {
            if (_stylesReady)
                return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };
            _titleStyle.normal.textColor = Color.white;

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                wordWrap = true
            };
            _bodyStyle.normal.textColor = new Color(0.94f, 0.95f, 0.92f);

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };
            _hintStyle.normal.textColor = new Color(0.99f, 0.88f, 0.54f);

            _stylesReady = true;
        }
    }
}
