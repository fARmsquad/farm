using FarmSimVR.Core.Tutorial;
using FarmSimVR.MonoBehaviours.Farming;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    public sealed class FarmVegetableStatesSceneController : MonoBehaviour
    {
        private static readonly string[] OptionLabels =
        {
            "Tomato: plant seed, stop-bar pat soil, mash weeds, arrow-sequence tie vine, arrow-sequence pinch suckers, left-right blossom brushing, mash leaf stripping, stop-bar ripeness check, left-right twist harvest. The plank stays up from planting onward.",
            "Carrot: plant seed, close furrow, thin seedlings, weed row, brush soil off shoulders once orange shows at Carrot_04, loosen crown soil, check shoulder color, then pull harvest.",
            "Corn: plant kernel, cover hill, thin to strongest stalk, weed base, hill soil, strip weak sucker, shake tassels onto silks once fruit appears at Corn_06, check silk set, peel husk tip, milk-check kernel, then snap harvest.",
            "Wheat: sow both seed variants, rake cover, pull broadleaf weeds in the early shoots, clear row competition at tillering, rub grain to check fill when heads are out, then cut and bundle the ripe paired density variants.",
        };

        private GUIStyle _headerStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _hintStyle;
        private bool _stylesReady;

        private void Start()
        {
            BuildEnvironment();
            FarmFirstPersonRigUtility.EnsureRig();

            var camera = Camera.main;
            if (camera != null)
                camera.backgroundColor = new Color(0.74f, 0.84f, 0.92f);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
                SceneManager.LoadScene(TutorialSceneCatalog.TitleScreenSceneName);
        }

        private void OnGUI()
        {
            BuildStyles();

            GUI.color = new Color(0.04f, 0.05f, 0.04f, 0.84f);
            GUI.DrawTexture(new Rect(20f, 20f, 392f, 340f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(36f, 34f, 360f, 30f), "FARM VEGETABLE STATES", _headerStyle);
            GUI.Label(
                new Rect(36f, 72f, 360f, 220f),
                "Walk each crop row and pick the stage visuals.\n\n" + string.Join("\n\n", OptionLabels),
                _bodyStyle);
            GUI.Label(new Rect(36f, 312f, 360f, 20f), "WASD + mouse to inspect. Esc returns to title.", _hintStyle);
        }

        private static void BuildEnvironment()
        {
            if (GameObject.Find("FarmVegetableStates_Root") != null)
                return;

            var root = new GameObject("FarmVegetableStates_Root");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(root.transform, false);
            ground.transform.position = new Vector3(0f, 0f, 12f);
            ground.transform.localScale = new Vector3(5.5f, 1f, 6f);
            ground.GetComponent<Renderer>().material.color = new Color(0.36f, 0.45f, 0.3f);

            var spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(root.transform, false);
            spawnPoint.transform.position = new Vector3(0f, 0.1f, -8f);
        }

        private void BuildStyles()
        {
            if (_stylesReady)
                return;

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            _headerStyle.normal.textColor = new Color(0.95f, 0.97f, 0.93f);

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true
            };
            _bodyStyle.normal.textColor = new Color(0.92f, 0.96f, 0.91f);

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Italic
            };
            _hintStyle.normal.textColor = new Color(0.83f, 0.9f, 0.82f);

            _stylesReady = true;
        }
    }
}
