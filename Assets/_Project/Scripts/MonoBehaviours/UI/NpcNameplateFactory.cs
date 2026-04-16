using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmSimVR.MonoBehaviours.UI
{
    /// <summary>
    /// Builds a world-space name card (canvas + panel + TMP) for an NPC root.
    /// </summary>
    public static class NpcNameplateFactory
    {
        public static GameObject CreateNameplate(Transform npcRoot, Vector3 localOffset)
        {
            var root = new GameObject("NpcNameplate");
            root.transform.SetParent(npcRoot, false);
            root.transform.localPosition = localOffset;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            root.AddComponent<BillboardFaceMainCamera>();

            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var canvasRt = canvasGo.GetComponent<RectTransform>();
            canvasRt.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            canvasRt.sizeDelta = new Vector2(360f, 100f);
            canvasRt.localPosition = Vector3.zero;

            var card = new GameObject("Card");
            card.transform.SetParent(canvasGo.transform, false);
            var image = card.AddComponent<Image>();
            image.color = new Color(0.08f, 0.09f, 0.11f, 0.92f);
            image.raycastTarget = false;
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = Vector2.zero;
            cardRt.anchorMax = Vector2.one;
            cardRt.offsetMin = Vector2.zero;
            cardRt.offsetMax = Vector2.zero;

            var nameGo = new GameObject("NameText");
            nameGo.transform.SetParent(card.transform, false);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = "NPC";
            nameTmp.fontSize = 30f;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = Color.white;
            nameTmp.raycastTarget = false;
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.38f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(14f, 4f);
            nameRt.offsetMax = new Vector2(-14f, -6f);

            var roleGo = new GameObject("RoleText");
            roleGo.transform.SetParent(card.transform, false);
            var roleTmp = roleGo.AddComponent<TextMeshProUGUI>();
            roleTmp.text = string.Empty;
            roleTmp.fontSize = 22f;
            roleTmp.alignment = TextAlignmentOptions.Center;
            roleTmp.color = new Color(0.75f, 0.78f, 0.82f, 1f);
            roleTmp.raycastTarget = false;
            var roleRt = roleGo.GetComponent<RectTransform>();
            roleRt.anchorMin = new Vector2(0f, 0f);
            roleRt.anchorMax = new Vector2(1f, 0.42f);
            roleRt.offsetMin = new Vector2(14f, 8f);
            roleRt.offsetMax = new Vector2(-14f, -2f);
            roleGo.SetActive(false);

            var binder = root.AddComponent<NpcNameplateBinder>();
            binder.Configure(nameTmp, roleTmp);
            return root;
        }
    }
}
