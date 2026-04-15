using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using FarmSimVR.MonoBehaviours.Mailbox;

namespace FarmSimVR.Editor
{
    public static class CreatePaperStackUI
    {
        private const string CORE_SCENE_PATH = "Assets/_Project/Scenes/CoreScene.unity";

        [MenuItem("FarmSimVR/Mailbox/Wire Up Paper Stack in CoreScene")]
        public static void WireUp()
        {
            var scene = EditorSceneManager.OpenScene(CORE_SCENE_PATH, OpenSceneMode.Single);

            // Remove any existing stack so re-running is safe
            foreach (var existing in Object.FindObjectsByType<MailPaperStackController>(FindObjectsSortMode.None))
                Object.DestroyImmediate(existing.gameObject);

            var canvas = FindMailboxCanvas();
            if (canvas == null)
            {
                Debug.LogError("[CreatePaperStackUI] MailboxHUDCanvas not found. Run Wire Up Mailbox in CoreScene first.");
                return;
            }

            // ── Stack panel — full-screen semi-transparent overlay ─────────────
            var stackPanelGO = new GameObject("PaperStackPanel");
            stackPanelGO.transform.SetParent(canvas.transform, false);
            stackPanelGO.SetActive(false);

            var panelRect = stackPanelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            stackPanelGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            // ── Stack root — centered anchor point where papers instantiate ────
            var stackRootGO = new GameObject("StackRoot");
            stackRootGO.transform.SetParent(stackPanelGO.transform, false);
            var stackRootRect = stackRootGO.AddComponent<RectTransform>();
            stackRootRect.anchorMin        = new Vector2(0.5f, 0.5f);
            stackRootRect.anchorMax        = new Vector2(0.5f, 0.5f);
            stackRootRect.pivot            = new Vector2(0.5f, 0.5f);
            stackRootRect.anchoredPosition = Vector2.zero;
            stackRootRect.sizeDelta        = new Vector2(560f, 720f);

            // ── Paper prefab ──────────────────────────────────────────────────
            var paperPrefabGO = BuildPaperPrefab(stackPanelGO.transform);

            // ── Controller on its own always-active GameObject ────────────────
            // IMPORTANT: the controller must NOT live on stackPanelGO because
            // Awake() calls SetActive(false) on panelRoot, which would disable
            // the controller itself and prevent Update() from ever running.
            var managerGO = new GameObject("PaperStackManager");
            var ctrl      = managerGO.AddComponent<MailPaperStackController>();
            var so        = new SerializedObject(ctrl);
            so.FindProperty("panelRoot").objectReferenceValue   = stackPanelGO;
            so.FindProperty("paperPrefab").objectReferenceValue = paperPrefabGO;
            so.FindProperty("stackRoot").objectReferenceValue   = stackRootGO.transform;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[CreatePaperStackUI] Paper stack wired up. Press Enter in Play mode to open it.");
        }

        // ── Paper prefab ──────────────────────────────────────────────────────

        private static GameObject BuildPaperPrefab(Transform parent)
        {
            var go = new GameObject("PaperPrefab");
            go.transform.SetParent(parent, false);
            go.SetActive(false);

            // Paper background — off-white parchment
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(560f, 720f);
            go.AddComponent<Image>().color = new Color(0.96f, 0.93f, 0.86f);

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.padding               = new RectOffset(36, 36, 36, 80); // bottom pad leaves room for seal
            vlg.spacing               = 10f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = true;

            // Sender — small, italic, muted brown
            var senderGO  = new GameObject("SenderLabel");
            senderGO.transform.SetParent(go.transform, false);
            var senderTxt = senderGO.AddComponent<TextMeshProUGUI>();
            senderTxt.fontSize           = 14f;
            senderTxt.fontStyle          = FontStyles.Italic;
            senderTxt.color              = new Color(0.38f, 0.30f, 0.20f);
            senderTxt.alignment          = TextAlignmentOptions.Left;
            senderTxt.enableWordWrapping = false;
            senderTxt.overflowMode       = TextOverflowModes.Ellipsis;
            senderGO.AddComponent<LayoutElement>().preferredHeight = 22f;

            // Subject — larger, bold, dark ink
            var subjectGO  = new GameObject("SubjectLabel");
            subjectGO.transform.SetParent(go.transform, false);
            var subjectTxt = subjectGO.AddComponent<TextMeshProUGUI>();
            subjectTxt.fontSize           = 22f;
            subjectTxt.fontStyle          = FontStyles.Bold;
            subjectTxt.color              = new Color(0.15f, 0.11f, 0.07f);
            subjectTxt.alignment          = TextAlignmentOptions.Left;
            subjectTxt.enableWordWrapping = true;
            subjectTxt.overflowMode       = TextOverflowModes.Overflow;
            subjectGO.AddComponent<LayoutElement>().preferredHeight = 32f;

            // Divider
            var divGO = new GameObject("Divider");
            divGO.transform.SetParent(go.transform, false);
            divGO.AddComponent<Image>().color = new Color(0.55f, 0.45f, 0.32f, 0.5f);
            var divLE = divGO.AddComponent<LayoutElement>();
            divLE.preferredHeight = 1f;
            divLE.minHeight       = 1f;

            // Body — fills remaining space
            var bodyGO  = new GameObject("BodyLabel");
            bodyGO.transform.SetParent(go.transform, false);
            var bodyTxt = bodyGO.AddComponent<TextMeshProUGUI>();
            bodyTxt.fontSize           = 15f;
            bodyTxt.color              = new Color(0.15f, 0.11f, 0.07f);
            bodyTxt.alignment          = TextAlignmentOptions.TopLeft;
            bodyTxt.enableWordWrapping = true;
            bodyTxt.lineSpacing        = 2f;
            bodyTxt.paragraphSpacing   = 6f;
            bodyGO.AddComponent<LayoutElement>().flexibleHeight = 1f;

            // Wax seal — absolute positioned at bottom center, ignores layout flow
            var sealGO   = new GameObject("WaxSeal");
            sealGO.transform.SetParent(go.transform, false);
            sealGO.AddComponent<Image>().color = new Color(0.62f, 0.08f, 0.08f);
            var sealRect = sealGO.GetComponent<RectTransform>();
            sealRect.anchorMin        = new Vector2(0.5f, 0f);
            sealRect.anchorMax        = new Vector2(0.5f, 0f);
            sealRect.pivot            = new Vector2(0.5f, 0f);
            sealRect.anchoredPosition = new Vector2(0f, 24f);
            sealRect.sizeDelta        = new Vector2(52f, 52f);
            sealGO.AddComponent<LayoutElement>().ignoreLayout = true;

            return go;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Canvas FindMailboxCanvas()
        {
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c.gameObject.name == "MailboxHUDCanvas") return c;
            return null;
        }
    }
}
