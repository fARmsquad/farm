using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.Editor
{
    public static class SequencerDemoBuilder
    {
        [MenuItem("FarmSim/Build Sequencer Demo Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // ── Ground ──
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5, 1, 5);
            ground.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ground.GetComponent<Renderer>().sharedMaterial.color = new Color(0.3f, 0.5f, 0.2f);

            // ── Some props to look at ──
            var barn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barn.name = "Barn";
            barn.transform.position = new Vector3(0, 2, 8);
            barn.transform.localScale = new Vector3(6, 4, 4);
            barn.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            barn.GetComponent<Renderer>().sharedMaterial.color = new Color(0.6f, 0.2f, 0.1f);

            var silo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            silo.name = "Silo";
            silo.transform.position = new Vector3(5, 3, 8);
            silo.transform.localScale = new Vector3(2, 3, 2);
            silo.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            silo.GetComponent<Renderer>().sharedMaterial.color = Color.grey;

            // ── Directional Light (already in default scene, just adjust) ──
            var light = Object.FindAnyObjectByType<Light>();
            if (light != null)
            {
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
                light.intensity = 1.2f;
            }

            // ── Camera (use the default Main Camera) ──
            var cam = Camera.main;
            cam.transform.position = new Vector3(0, 3, -10);
            cam.transform.LookAt(barn.transform);
            cam.fieldOfView = 60;

            // ── ScreenEffects (full UI setup) ──
            var screenEffectsGo = BuildScreenEffects(cam);

            // ── Player stub (just needs PlayerMovement for enable/disable test) ──
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0, 1, 0);
            player.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            player.GetComponent<Renderer>().sharedMaterial.color = Color.yellow;
            player.tag = "Player";
            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            var pm = player.AddComponent<FarmSimVR.MonoBehaviours.Hunting.PlayerMovement>();

            // ── CinematicSequencer ──
            var sequencerGo = new GameObject("CinematicSequencer");
            var sequencer = sequencerGo.AddComponent<CinematicSequencer>();

            // Wire subsystem references via SerializedObject
            var so = new SerializedObject(sequencer);
            so.FindProperty("_screenEffects").objectReferenceValue = screenEffectsGo.GetComponent<ScreenEffects>();
            so.FindProperty("_playerMovement").objectReferenceValue = pm;
            so.ApplyModifiedProperties();

            // ── AutoPlay script ──
            var autoPlay = sequencerGo.AddComponent<SequencerDemoAutoPlay>();

            // ── Save Scene ──
            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/SequencerDemo.unity");
            EditorUtility.SetDirty(sequencerGo);

            Debug.Log("[SequencerDemoBuilder] Demo scene built. Hit Play to watch!");
        }

        private static GameObject BuildScreenEffects(Camera cam)
        {
            var go = new GameObject("ScreenEffects");
            var screenEffects = go.AddComponent<ScreenEffects>();

            // Canvas
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            // Fade overlay
            var fadeGo = new GameObject("FadeOverlay");
            fadeGo.transform.SetParent(go.transform, false);
            var fadeImg = fadeGo.AddComponent<Image>();
            fadeImg.color = new Color(0, 0, 0, 0);
            fadeImg.raycastTarget = false;
            var fadeRect = fadeGo.GetComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.sizeDelta = Vector2.zero;
            var fadeCg = fadeGo.AddComponent<CanvasGroup>();
            fadeCg.alpha = 0;

            // Letterbox top bar
            var topBarGo = new GameObject("TopBar");
            topBarGo.transform.SetParent(go.transform, false);
            var topImg = topBarGo.AddComponent<Image>();
            topImg.color = Color.black;
            topImg.raycastTarget = false;
            var topRect = topBarGo.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 0);

            // Letterbox bottom bar
            var botBarGo = new GameObject("BottomBar");
            botBarGo.transform.SetParent(go.transform, false);
            var botImg = botBarGo.AddComponent<Image>();
            botImg.color = Color.black;
            botImg.raycastTarget = false;
            var botRect = botBarGo.GetComponent<RectTransform>();
            botRect.anchorMin = Vector2.zero;
            botRect.anchorMax = new Vector2(1, 0);
            botRect.pivot = new Vector2(0.5f, 0);
            botRect.sizeDelta = new Vector2(0, 0);

            // Objective popup container (AddComponent<RectTransform> via explicit add)
            var objContainer = new GameObject("ObjectiveContainer", typeof(RectTransform));
            objContainer.transform.SetParent(go.transform, false);
            var objRect = objContainer.GetComponent<RectTransform>();
            objRect.anchorMin = new Vector2(0, 0);
            objRect.anchorMax = new Vector2(1, 0);
            objRect.pivot = new Vector2(0.5f, 0);
            objRect.anchoredPosition = new Vector2(0, 80);
            objRect.sizeDelta = new Vector2(-100, 50);

            var objText = new GameObject("ObjectiveText");
            objText.transform.SetParent(objContainer.transform, false);
            var objTmp = objText.AddComponent<TextMeshProUGUI>();
            objTmp.text = "";
            objTmp.fontSize = 24;
            objTmp.alignment = TextAlignmentOptions.Center;
            objTmp.color = Color.white;
            var objTmpRect = objText.GetComponent<RectTransform>();
            objTmpRect.anchorMin = Vector2.zero;
            objTmpRect.anchorMax = Vector2.one;
            objTmpRect.sizeDelta = Vector2.zero;

            // Mission passed (Image first to force RectTransform)
            var mpGo = new GameObject("MissionPassed");
            mpGo.transform.SetParent(go.transform, false);
            var mpBg = mpGo.AddComponent<Image>();
            mpBg.color = new Color(0, 0, 0, 0.7f);
            var mpCg = mpGo.AddComponent<CanvasGroup>();
            mpCg.alpha = 0;
            var mpRect = mpGo.GetComponent<RectTransform>();
            mpRect.anchorMin = new Vector2(0.2f, 0.35f);
            mpRect.anchorMax = new Vector2(0.8f, 0.65f);
            mpRect.sizeDelta = Vector2.zero;

            var mpTextGo = new GameObject("MissionPassedText");
            mpTextGo.transform.SetParent(mpGo.transform, false);
            var mpTmp = mpTextGo.AddComponent<TextMeshProUGUI>();
            mpTmp.text = "";
            mpTmp.fontSize = 36;
            mpTmp.alignment = TextAlignmentOptions.Center;
            mpTmp.color = Color.white;
            var mpTmpRect = mpTextGo.GetComponent<RectTransform>();
            mpTmpRect.anchorMin = Vector2.zero;
            mpTmpRect.anchorMax = Vector2.one;
            mpTmpRect.sizeDelta = Vector2.zero;

            // Wire serialized fields via SerializedObject
            var seSo = new SerializedObject(screenEffects);
            seSo.FindProperty("fadeOverlay").objectReferenceValue = fadeImg;
            seSo.FindProperty("fadeCanvasGroup").objectReferenceValue = fadeCg;
            seSo.FindProperty("topBar").objectReferenceValue = topRect;
            seSo.FindProperty("bottomBar").objectReferenceValue = botRect;
            seSo.FindProperty("objectiveContainer").objectReferenceValue = objRect;
            seSo.FindProperty("objectiveText").objectReferenceValue = objTmp;
            seSo.FindProperty("missionPassedGroup").objectReferenceValue = mpCg;
            seSo.FindProperty("missionPassedText").objectReferenceValue = mpTmp;
            seSo.FindProperty("targetCamera").objectReferenceValue = cam;
            seSo.ApplyModifiedProperties();

            return go;
        }
    }
}
