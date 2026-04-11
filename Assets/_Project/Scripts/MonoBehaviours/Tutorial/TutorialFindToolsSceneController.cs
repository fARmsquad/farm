using FarmSimVR.MonoBehaviours.Farming;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    public sealed class TutorialFindToolsSceneController : MonoBehaviour
    {
        private const float GoalHalfExtent = 1.5f;
        private static readonly Vector3 GoalSquarePosition = new(0f, 0.05f, 6f);

        private Transform _player;
        private string _feedbackMessage;
        private float _feedbackUntil;
        private float _completionAt = -1f;
        private bool _goalReached;

        private GUIStyle _bodyStyle;
        private GUIStyle _feedbackStyle;
        private bool _stylesReady;

        private void Start()
        {
            BuildEnvironment();
            var rig = FarmFirstPersonRigUtility.EnsureRig();
            _player = rig != null ? rig.transform : null;
            SetFeedback("Walk to the glowing square to continue.");
        }

        private void Update()
        {
            if (_player == null)
            {
                var rig = FindAnyObjectByType<FirstPersonExplorer>();
                if (rig != null)
                    _player = rig.transform;
            }

            HandleGoalReached();
            HandleCompletion();
        }

        private void OnGUI()
        {
            BuildStyles();
            DrawObjectivePanel();
            DrawFeedback();
        }

        private void BuildEnvironment()
        {
            if (GameObject.Find("TutorialFindTools_Root") != null)
                return;

            var root = new GameObject("TutorialFindTools_Root");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(root.transform, false);
            ground.transform.localScale = new Vector3(2f, 1f, 2f);
            ground.GetComponent<Renderer>().material.color = new Color(0.35f, 0.44f, 0.28f);

            var spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(root.transform, false);
            spawnPoint.transform.position = new Vector3(0f, 0.1f, -8f);

            CreateGoalSquare(root.transform);
            CreateMarkerPost(root.transform, new Vector3(0f, 1.3f, 6f));

            CreateFence(root.transform, new Vector3(0f, 0.5f, 10f), new Vector3(18f, 1f, 0.5f));
            CreateFence(root.transform, new Vector3(0f, 0.5f, -10f), new Vector3(18f, 1f, 0.5f));
            CreateFence(root.transform, new Vector3(10f, 0.5f, 0f), new Vector3(0.5f, 1f, 18f));
            CreateFence(root.transform, new Vector3(-10f, 0.5f, 0f), new Vector3(0.5f, 1f, 18f));
        }

        private static void CreateGoalSquare(Transform parent)
        {
            var goalSquare = GameObject.CreatePrimitive(PrimitiveType.Cube);
            goalSquare.name = "GoalSquare";
            goalSquare.transform.SetParent(parent, false);
            goalSquare.transform.position = GoalSquarePosition;
            goalSquare.transform.localScale = new Vector3(GoalHalfExtent * 2f, 0.1f, GoalHalfExtent * 2f);
            goalSquare.GetComponent<Renderer>().material.color = new Color(0.3f, 0.85f, 0.45f);
        }

        private static void CreateMarkerPost(Transform parent, Vector3 position)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "GoalMarker";
            post.transform.SetParent(parent, false);
            post.transform.position = position;
            post.transform.localScale = new Vector3(0.35f, 1.2f, 0.35f);
            post.GetComponent<Renderer>().material.color = new Color(1f, 0.93f, 0.32f);

            var topper = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            topper.name = "GoalMarkerTop";
            topper.transform.SetParent(post.transform, false);
            topper.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            topper.transform.localScale = new Vector3(0.9f, 0.45f, 0.9f);
            topper.GetComponent<Renderer>().material.color = new Color(1f, 0.65f, 0.2f);
        }

        private static void CreateFence(Transform parent, Vector3 position, Vector3 scale)
        {
            var fence = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fence.name = "Boundary";
            fence.transform.SetParent(parent, false);
            fence.transform.position = position;
            fence.transform.localScale = scale;
            fence.GetComponent<Renderer>().material.color = new Color(0.48f, 0.33f, 0.19f);
        }

        private void HandleGoalReached()
        {
            if (_goalReached || _player == null)
                return;

            var position = _player.position;
            if (Mathf.Abs(position.x - GoalSquarePosition.x) > GoalHalfExtent)
                return;

            if (Mathf.Abs(position.z - GoalSquarePosition.z) > GoalHalfExtent)
                return;

            _goalReached = true;
            _completionAt = Time.time + 1.1f;
            SetFeedback("Reached the marker. Continuing...");
        }

        private void HandleCompletion()
        {
            if (_completionAt < 0f || Time.time < _completionAt)
                return;

            _completionAt = -1f;
            TutorialFlowController.Instance?.CompleteCurrentSceneAndLoadNext();
        }

        private void DrawObjectivePanel()
        {
            var text =
                "Placeholder Gameplay Beat\n" +
                $"{Mark(_goalReached)} Walk to the marked square\n\n" +
                "Use WASD to move and the mouse to look.\n" +
                "Cross the glowing square to advance.";

            GUI.color = new Color(0.04f, 0.05f, 0.04f, 0.82f);
            GUI.DrawTexture(new Rect(18f, 120f, 340f, 128f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(32f, 132f, 308f, 108f), text, _bodyStyle);
        }

        private void DrawFeedback()
        {
            if (string.IsNullOrWhiteSpace(_feedbackMessage) || Time.time > _feedbackUntil)
                return;

            var width = 420f;
            var x = (Screen.width - width) * 0.5f;
            GUI.color = new Color(0f, 0f, 0f, 0.66f);
            GUI.DrawTexture(new Rect(x, 32f, width, 34f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 10f, 38f, width - 20f, 22f), _feedbackMessage, _feedbackStyle);
        }

        private static string Mark(bool recovered)
        {
            return recovered ? "[x]" : "[ ]";
        }

        private void SetFeedback(string message)
        {
            _feedbackMessage = message;
            _feedbackUntil = Time.time + 2f;
        }

        private void BuildStyles()
        {
            if (_stylesReady)
                return;

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true
            };
            _bodyStyle.normal.textColor = new Color(0.93f, 0.96f, 0.92f);

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
