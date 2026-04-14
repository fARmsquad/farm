using FarmSimVR.Core.Story;
using FarmSimVR.Core.Tutorial;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Farming;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    public sealed class TutorialFindToolsSceneController : MonoBehaviour
    {
        private const float GoalHalfExtent = 1.5f;
        private const float PickupCollectionRadius = 1.25f;
        private static readonly Vector3 GoalSquarePosition = new(0f, 0.05f, 6f);
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly PackageFindToolsMissionService _packageMission = new();

        private Transform _player;
        private string _feedbackMessage;
        private float _feedbackUntil;
        private float _completionAt = -1f;
        private bool _goalReached;
        private bool _usePackageMode;
        private bool _failureAcknowledged;
        private bool _sceneHasEnvironment;
        private string _currentObjective = string.Empty;
        private TutorialToolPickup[] _packagePickups = System.Array.Empty<TutorialToolPickup>();

        private GUIStyle _bodyStyle;
        private GUIStyle _feedbackStyle;
        private bool _stylesReady;

        public string CurrentObjectiveText => _currentObjective;

        private void Start()
        {
            _usePackageMode = TryConfigurePackageMode();

            // Only build procedural environment when the scene lacks its own.
            _sceneHasEnvironment = GameObject.Find("Environment") != null;
            if (!_sceneHasEnvironment)
                BuildEnvironment(includeGoalSquare: !_usePackageMode);

            if (_usePackageMode)
            {
                BuildPackagePickups();
                _currentObjective = _packageMission.CurrentObjective;
                SetFeedback("Recover the scattered tools to continue.");
            }
            else
            {
                _currentObjective = "Walk to the glowing square to continue.";
                SetFeedback(_currentObjective);
            }

            var rig = FarmFirstPersonRigUtility.EnsureRig();
            _player = rig != null ? rig.transform : null;
        }

        private void Update()
        {
            if (_player == null)
            {
                var rig = FindAnyObjectByType<FirstPersonExplorer>();
                if (rig != null)
                    _player = rig.transform;
            }

            if (_usePackageMode)
                HandlePackageMode();
            else
                HandleGoalReached();

            HandleCompletion();
        }

        private void OnGUI()
        {
            BuildStyles();
            DrawObjectivePanel();
            DrawFeedback();
        }

        private bool TryConfigurePackageMode()
        {
            if (!StoryPackageRuntimeCatalog.TryGetMinigameConfig(SceneManager.GetActiveScene().name, out _, out var minigame))
                return false;

            if (minigame == null || !string.Equals(minigame.AdapterId, "tutorial.find_tools", System.StringComparison.Ordinal))
                return false;

            var targetToolSet = "starter";
            var searchZone = "yard";
            var hintStrength = "strong";
            minigame.TryGetStringParameter("targetToolSet", out targetToolSet);
            minigame.TryGetStringParameter("searchZone", out searchZone);
            minigame.TryGetStringParameter("hintStrength", out hintStrength);

            _packageMission.Configure(
                minigame.ObjectiveText,
                targetToolSet,
                minigame.RequiredCount,
                searchZone,
                hintStrength,
                minigame.TimeLimitSeconds);
            return true;
        }

        private void BuildEnvironment(bool includeGoalSquare)
        {
            var root = ResolveOrCreateRoot();
            EnsureGround(root.transform);
            EnsureSpawnPoint(root.transform);
            EnsureMarkerPost(root.transform, new Vector3(0f, 1.3f, 6f), "GoalMarker", new Color(1f, 0.65f, 0.2f));
            EnsureFence(root.transform, new Vector3(0f, 0.5f, 10f), new Vector3(18f, 1f, 0.5f));
            EnsureFence(root.transform, new Vector3(0f, 0.5f, -10f), new Vector3(18f, 1f, 0.5f));
            EnsureFence(root.transform, new Vector3(10f, 0.5f, 0f), new Vector3(0.5f, 1f, 18f));
            EnsureFence(root.transform, new Vector3(-10f, 0.5f, 0f), new Vector3(0.5f, 1f, 18f));

            if (includeGoalSquare)
                EnsureGoalSquare(root.transform);
        }

        private void BuildPackagePickups()
        {
            var root = ResolveOrCreateRoot().transform;
            var pickupRoot = root.Find("PackageFindTools_Pickups");
            if (pickupRoot != null)
                DestroyUnityObject(pickupRoot.gameObject);

            pickupRoot = new GameObject("PackageFindTools_Pickups").transform;
            pickupRoot.SetParent(root, false);

            var positions = PackageFindToolsSceneLayout.GetPickupPositions(_packageMission.SearchZone);
            _packagePickups = new TutorialToolPickup[_packageMission.ToolDisplayNames.Length];

            for (var i = 0; i < _packageMission.ToolDisplayNames.Length; i++)
            {
                var pickup = CreatePickup(
                    pickupRoot,
                    _packageMission.ToolDisplayNames[i],
                    positions[i % positions.Length],
                    i);
                _packagePickups[i] = pickup;
            }
        }

        private TutorialToolPickup CreatePickup(Transform parent, string toolName, Vector3 position, int index)
        {
            var root = new GameObject($"ToolPickup_{index + 1:00}_{Sanitize(toolName)}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            var pickup = root.AddComponent<TutorialToolPickup>();
            pickup.Configure(toolName);

            if (!_sceneHasEnvironment)
            {
                var visual = GameObject.CreatePrimitive(ResolvePrimitive(toolName));
                visual.name = "PickupVisual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale = ResolveScale(toolName);
                DestroyUnityObject(visual.GetComponent<Collider>());
                SetRendererColor(visual.GetComponent<Renderer>(), ResolveToolColor(toolName));

                if (_packageMission.HintStrength != "light")
                {
                    var markerColor = _packageMission.HintStrength == "strong"
                        ? new Color(1f, 0.9f, 0.32f)
                        : new Color(0.72f, 0.92f, 1f);
                    var height = _packageMission.HintStrength == "strong" ? 1.6f : 1.2f;
                    EnsureMarkerPost(root.transform, new Vector3(0f, height, 0f), "PickupMarker", markerColor);
                }
            }

            return pickup;
        }

        private void HandlePackageMode()
        {
            var collectedCount = 0;
            for (var i = 0; i < _packagePickups.Length; i++)
            {
                var pickup = _packagePickups[i];
                if (pickup == null)
                    continue;

                if (!pickup.IsCollected && _player != null)
                {
                    var distance = Vector3.Distance(_player.position, pickup.transform.position);
                    if (distance <= PickupCollectionRadius && pickup.Collect())
                    {
                        MarkRecoveredTool(pickup.ToolName);
                        SetFeedback($"{pickup.ToolName} recovered.");
                    }
                }

                if (pickup.IsCollected)
                    collectedCount++;
            }

            _packageMission.Observe(collectedCount, Time.deltaTime);
            _currentObjective = _packageMission.CurrentObjective;

            if (_packageMission.IsComplete && _completionAt < 0f)
            {
                _completionAt = Time.time + 1.1f;
                SetFeedback("Tools recovered. Continuing...");
            }

            if (_packageMission.IsFailed && !_failureAcknowledged)
            {
                _failureAcknowledged = true;
                SetFeedback("Time ran out.");
            }
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
            var text = _usePackageMode
                ? $"{_currentObjective}\n\nMove close to each tool to recover it."
                : "Placeholder Gameplay Beat\n" +
                  $"{Mark(_goalReached)} Walk to the marked square\n\n" +
                  "Use WASD to move and the mouse to look.\n" +
                  "Cross the glowing square to advance.";

            GUI.color = new Color(0.04f, 0.05f, 0.04f, 0.82f);
            GUI.DrawTexture(new Rect(18f, 120f, 380f, _usePackageMode ? 106f : 128f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(32f, 132f, 348f, 108f), text, _bodyStyle);
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

        private static GameObject ResolveOrCreateRoot()
        {
            var root = GameObject.Find("TutorialFindTools_Root");
            return root != null ? root : new GameObject("TutorialFindTools_Root");
        }

        private static void EnsureGround(Transform parent)
        {
            var ground = FindChild(parent, "Ground");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.SetParent(parent, false);
                ground.transform.localScale = new Vector3(2f, 1f, 2f);
            }

            SetRendererColor(ground.GetComponent<Renderer>(), new Color(0.35f, 0.44f, 0.28f));
        }

        private static void EnsureSpawnPoint(Transform parent)
        {
            var spawnPoint = FindChild(parent, "SpawnPoint");
            if (spawnPoint == null)
            {
                spawnPoint = new GameObject("SpawnPoint");
                spawnPoint.transform.SetParent(parent, false);
            }

            spawnPoint.transform.position = new Vector3(0f, 0.1f, -8f);
        }

        private static void EnsureGoalSquare(Transform parent)
        {
            var goalSquare = FindChild(parent, "GoalSquare");
            if (goalSquare == null)
            {
                goalSquare = GameObject.CreatePrimitive(PrimitiveType.Cube);
                goalSquare.name = "GoalSquare";
                goalSquare.transform.SetParent(parent, false);
                goalSquare.transform.position = GoalSquarePosition;
                goalSquare.transform.localScale = new Vector3(GoalHalfExtent * 2f, 0.1f, GoalHalfExtent * 2f);
            }

            SetRendererColor(goalSquare.GetComponent<Renderer>(), new Color(0.3f, 0.85f, 0.45f));
        }

        private static void EnsureFence(Transform parent, Vector3 position, Vector3 scale)
        {
            var fence = FindChildAt(parent, "Boundary", position);
            if (fence == null)
            {
                fence = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fence.name = "Boundary";
                fence.transform.SetParent(parent, false);
            }

            fence.transform.position = position;
            fence.transform.localScale = scale;
            SetRendererColor(fence.GetComponent<Renderer>(), new Color(0.48f, 0.33f, 0.19f));
        }

        private static void EnsureMarkerPost(Transform parent, Vector3 position, string name, Color color)
        {
            var post = FindChild(parent, name);
            if (post == null)
            {
                post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = name;
                post.transform.SetParent(parent, false);
            }

            post.transform.localPosition = position;
            post.transform.localScale = new Vector3(0.35f, 1.2f, 0.35f);
            SetRendererColor(post.GetComponent<Renderer>(), color);

            var topper = FindChild(post.transform, $"{name}Top");
            if (topper == null)
            {
                topper = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                topper.name = $"{name}Top";
                topper.transform.SetParent(post.transform, false);
            }

            topper.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            topper.transform.localScale = new Vector3(0.9f, 0.45f, 0.9f);
            SetRendererColor(topper.GetComponent<Renderer>(), color);
        }

        private static PrimitiveType ResolvePrimitive(string toolName)
        {
            if (toolName.Contains("Watering") || toolName.Contains("Bucket"))
                return PrimitiveType.Cylinder;
            if (toolName.Contains("Pouch") || toolName.Contains("Reel"))
                return PrimitiveType.Sphere;
            return PrimitiveType.Cube;
        }

        private static Vector3 ResolveScale(string toolName)
        {
            if (toolName.Contains("Watering") || toolName.Contains("Bucket"))
                return new Vector3(0.55f, 0.45f, 0.55f);
            if (toolName.Contains("Pouch") || toolName.Contains("Reel"))
                return new Vector3(0.5f, 0.5f, 0.5f);
            return new Vector3(0.55f, 0.35f, 0.55f);
        }

        private static Color ResolveToolColor(string toolName)
        {
            if (toolName.Contains("Watering") || toolName.Contains("Bucket"))
                return new Color(0.32f, 0.75f, 1f);
            if (toolName.Contains("Pouch"))
                return new Color(0.82f, 0.62f, 0.24f);
            if (toolName.Contains("Basket"))
                return new Color(0.72f, 0.48f, 0.20f);
            return new Color(0.58f, 0.86f, 0.34f);
        }

        private void MarkRecoveredTool(string toolName)
        {
            if (TutorialFlowController.Instance == null)
                return;

            switch (toolName)
            {
                case "Watering Can":
                    TutorialFlowController.Instance.MarkToolRecovered(TutorialToolId.WateringCan);
                    break;
                case "Seed Pouch":
                    TutorialFlowController.Instance.MarkToolRecovered(TutorialToolId.SeedPouch);
                    break;
                case "Harvest Basket":
                    TutorialFlowController.Instance.MarkToolRecovered(TutorialToolId.HarvestBasket);
                    break;
            }
        }

        private static GameObject FindChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            return child != null ? child.gameObject : null;
        }

        private static GameObject FindChildAt(Transform parent, string childName, Vector3 localPosition)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name != childName)
                    continue;

                if (Vector3.Distance(child.position, localPosition) < 0.01f)
                    return child.gameObject;
            }

            return null;
        }

        private static void SetRendererColor(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(ColorId, color);
            block.SetColor(BaseColorId, color);
            renderer.SetPropertyBlock(block);
        }

        private static string Sanitize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "Tool"
                : value.Replace(" ", string.Empty);
        }

        private static void DestroyUnityObject(Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }
    }
}
