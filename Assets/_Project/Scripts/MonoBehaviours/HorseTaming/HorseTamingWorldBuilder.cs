using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FarmSimVR.MonoBehaviours.HorseTaming
{
    /// <summary>
    /// Builds the HorseTaming playfield at runtime (and is also invoked from the Editor menu).
    /// </summary>
    public static class HorseTamingWorldBuilder
    {
        private const string ActorsResourceRoot = "HorseTaming/Actors";

        private static GameObject InstantiateHorse(Vector3 position)
        {
            var prefab = Resources.Load<GameObject>($"{ActorsResourceRoot}/horse");
            GameObject horseGo;
            if (prefab != null)
            {
                horseGo = Object.Instantiate(prefab);
                horseGo.name = "Horse";
                horseGo.transform.position = position;
                horseGo.transform.rotation = Quaternion.identity;
            }
            else
            {
                horseGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                horseGo.name = "Horse";
                horseGo.transform.position = position + Vector3.up * 0.9f;
                DestroyColliderGeneric(horseGo.GetComponent<Collider>());
                var hrend = horseGo.GetComponent<Renderer>();
                if (hrend != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = new Color(0.45f, 0.32f, 0.2f);
                    hrend.sharedMaterial = mat;
                }
            }

            StripColliders(horseGo);

            var horseCol = horseGo.AddComponent<SphereCollider>();
            horseCol.radius = 0.72f;
            horseCol.center = new Vector3(0f, 0.85f, 0f);

            return horseGo;
        }

        private static GameObject InstantiatePlayerFarmer(Vector3 position, Vector3 lookToward)
        {
            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.transform.position = position;
            OrientYawToward(playerGo.transform, lookToward);

            var cc = playerGo.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var farmerPrefab = Resources.Load<GameObject>($"{ActorsResourceRoot}/SM_Chr_Farmer_Male_01");
            if (farmerPrefab != null)
            {
                var vis = Object.Instantiate(farmerPrefab, playerGo.transform);
                vis.name = "SM_Chr_Farmer_Male_01";
                vis.transform.localPosition = Vector3.zero;
                vis.transform.localRotation = Quaternion.identity;
                vis.transform.localScale = Vector3.one;
                StripColliders(vis);
            }
            else
            {
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.name = "PlaceholderBody";
                capsule.transform.SetParent(playerGo.transform, false);
                capsule.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                DestroyColliderGeneric(capsule.GetComponent<Collider>());
                var prend = capsule.GetComponent<Renderer>();
                if (prend != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = new Color(0.25f, 0.45f, 0.75f);
                    prend.sharedMaterial = mat;
                }
            }

            return playerGo;
        }

        private static void OrientYawToward(Transform t, Vector3 worldPoint)
        {
            var flat = worldPoint - t.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f)
                return;
            t.rotation = Quaternion.LookRotation(flat.normalized);
        }

        private static void StripColliders(GameObject go)
        {
            if (go == null)
                return;
            foreach (var c in go.GetComponentsInChildren<Collider>())
                DestroyColliderGeneric(c);
        }

        private static void DestroyColliderGeneric(Collider col)
        {
            if (col == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(col);
            else
                Object.DestroyImmediate(col);
        }

        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<HorseTamingGameController>() != null)
                return;

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(2f, 1f, 2f);
            var grend = ground.GetComponent<Renderer>();
            if (grend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.42f, 0.52f, 0.35f);
                grend.sharedMaterial = mat;
            }

            var sceneryRoot = new GameObject("HorseTaming_Scenery").transform;
            HorseTamingSyntyEnvironment.Build(sceneryRoot, ground.transform);

            var horsePos = new Vector3(0f, 0f, 0f);
            var playerPos = new Vector3(-5f, 0f, -4f);

            var horseGo = InstantiateHorse(horsePos);
            OrientYawToward(horseGo.transform, playerPos);
            horseGo.AddComponent<HorseTamingHorse>();

            var ringGo = new GameObject("ComfortRing");
            ringGo.transform.SetParent(horseGo.transform, false);
            ringGo.transform.localPosition = Vector3.zero;
            ringGo.AddComponent<HorseTamingComfortRing>();

            var playerGo = InstantiatePlayerFarmer(playerPos, horsePos);

            playerGo.AddComponent<HorseTamingPlayerController>();

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<AudioListener>();
            var topCam = camGo.AddComponent<HorseTamingTopDownCamera>();
            topCam.SetTarget(playerGo.transform);

            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var trustBack = new GameObject("TrustBarBackground");
            trustBack.transform.SetParent(canvasGo.transform, false);
            var trustBackRect = trustBack.AddComponent<RectTransform>();
            trustBackRect.anchorMin = new Vector2(0.2f, 0.88f);
            trustBackRect.anchorMax = new Vector2(0.8f, 0.95f);
            trustBackRect.sizeDelta = Vector2.zero;
            trustBack.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            var trustFillGo = new GameObject("TrustFill");
            trustFillGo.transform.SetParent(trustBack.transform, false);
            var trustFillRect = trustFillGo.AddComponent<RectTransform>();
            trustFillRect.anchorMin = Vector2.zero;
            trustFillRect.anchorMax = Vector2.one;
            trustFillRect.sizeDelta = Vector2.zero;
            trustFillRect.offsetMin = new Vector2(4f, 4f);
            trustFillRect.offsetMax = new Vector2(-4f, -4f);
            var trustFill = trustFillGo.AddComponent<Image>();
            trustFill.color = new Color(0.3f, 0.85f, 0.35f);
            trustFill.type = Image.Type.Filled;
            trustFill.fillMethod = Image.FillMethod.Horizontal;
            trustFill.fillAmount = 0f;

            var trustTextGo = new GameObject("TrustPercent");
            trustTextGo.transform.SetParent(canvasGo.transform, false);
            var trustTextRect = trustTextGo.AddComponent<RectTransform>();
            trustTextRect.anchorMin = new Vector2(0.2f, 0.8f);
            trustTextRect.anchorMax = new Vector2(0.8f, 0.87f);
            trustTextRect.sizeDelta = Vector2.zero;
            var trustText = trustTextGo.AddComponent<Text>();
            trustText.font = font;
            trustText.fontSize = 28;
            trustText.color = Color.white;
            trustText.alignment = TextAnchor.MiddleCenter;

            var hintGo = new GameObject("Hints");
            hintGo.transform.SetParent(canvasGo.transform, false);
            var hintRect = hintGo.AddComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.05f, 0.02f);
            hintRect.anchorMax = new Vector2(0.95f, 0.35f);
            hintRect.sizeDelta = Vector2.zero;
            var hintText = hintGo.AddComponent<Text>();
            hintText.font = font;
            hintText.fontSize = 22;
            hintText.color = Color.white;
            hintText.alignment = TextAnchor.LowerCenter;

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            var gameGo = new GameObject("HorseTamingGame");
            var game = gameGo.AddComponent<HorseTamingGameController>();
            game.WireRuntime(
                playerGo.GetComponent<HorseTamingPlayerController>(),
                horseGo.GetComponent<HorseTamingHorse>(),
                ringGo.GetComponent<HorseTamingComfortRing>(),
                topCam,
                trustFill,
                trustText,
                hintText);
        }
    }
}
