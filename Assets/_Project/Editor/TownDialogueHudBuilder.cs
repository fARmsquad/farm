using FarmSimVR.MonoBehaviours;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FarmSimVR.Editor
{
    /// <summary>
    /// Builds the Town dialogue HUD Canvas and wires DialogueChoiceUI into Town.unity.
    /// Menu: fARm > Town > Build Dialogue HUD
    ///
    /// Run from any scene setup — the script opens Town.unity additively if it isn't
    /// already loaded, builds the full Canvas hierarchy in that scene, wires
    /// DialogueChoiceUI to the existing LLMConversationController, then saves.
    /// Safe to run again — it removes any existing DialogueHUD root before rebuilding.
    /// </summary>
    public static class TownDialogueHudBuilder
    {
        private const string CoreScenePath   = "Assets/_Project/Scenes/CoreScene.unity";
        private const string RootName        = "DialogueHUD";
        private const float  PanelWidth      = 560f;
        private const float  PanelHeight     = 180f;
        private const float  PanelBottomPad  = 20f;
        private const float  ChoiceGap       = 10f;

        [MenuItem("fARm/Town/Build Dialogue HUD")]
        public static void Build()
        {
            // Ensure CoreScene is open — open it additively if needed
            var coreScene = SceneManager.GetSceneByPath(CoreScenePath);
            if (!coreScene.isLoaded)
            {
                coreScene = EditorSceneManager.OpenScene(CoreScenePath, OpenSceneMode.Additive);
                Debug.Log("[TownDialogueHudBuilder] Opened CoreScene.unity additively.");
            }

            SceneManager.SetActiveScene(coreScene);

            // Remove stale root so re-runs are idempotent
            var stale = GameObject.Find(RootName);
            if (stale != null)
            {
                Undo.DestroyObjectImmediate(stale);
                Debug.Log("[TownDialogueHudBuilder] Removed existing DialogueHUD root.");
            }

            // ── Canvas ───────────────────────────────────────────────────────
            var rootGo = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(rootGo, "Build Town Dialogue HUD");

            var canvas = rootGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = rootGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight   = 0.5f;

            rootGo.AddComponent<GraphicRaycaster>();

            // ── Dialogue panel (anchored bottom-centre) ───────────────────────
            var panel = MakeRect("DialoguePanel", rootGo.transform);
            SetAnchors(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            panel.anchoredPosition = new Vector2(0f, PanelBottomPad);
            panel.sizeDelta        = new Vector2(PanelWidth, PanelHeight);

            var bg = panel.gameObject.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.14f, 0.92f);

            // ── Speaker name ─────────────────────────────────────────────────
            var speakerNameRect = MakeRect("SpeakerName", panel);
            SetAnchors(speakerNameRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
            speakerNameRect.anchoredPosition = new Vector2(16f, -12f);
            speakerNameRect.sizeDelta        = new Vector2(-32f, 28f);

            var speakerTmp = speakerNameRect.gameObject.AddComponent<TextMeshProUGUI>();
            speakerTmp.text           = string.Empty;
            speakerTmp.fontSize       = 20f;
            speakerTmp.fontStyle      = FontStyles.Bold;
            speakerTmp.color          = new Color(0.95f, 0.85f, 0.55f, 1f);
            speakerTmp.alignment      = TextAlignmentOptions.TopLeft;
            speakerTmp.enableWordWrapping = false;

            // ── Dialogue body ─────────────────────────────────────────────────
            var dialogueRect = MakeRect("DialogueText", panel);
            SetAnchors(dialogueRect, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f));
            dialogueRect.anchoredPosition = new Vector2(16f, -4f);
            dialogueRect.sizeDelta        = new Vector2(-32f, -72f);

            var dialogueTmp = dialogueRect.gameObject.AddComponent<TextMeshProUGUI>();
            dialogueTmp.text              = string.Empty;
            dialogueTmp.fontSize          = 18f;
            dialogueTmp.color             = Color.white;
            dialogueTmp.alignment         = TextAlignmentOptions.TopLeft;
            dialogueTmp.enableWordWrapping = true;
            dialogueTmp.overflowMode      = TextOverflowModes.Overflow;

            // ── Status badge (Thinking…) ──────────────────────────────────────
            var loadingRect = MakeRect("StatusBadge", panel);
            SetAnchors(loadingRect, Vector2.zero, Vector2.zero, Vector2.zero);
            loadingRect.anchoredPosition = new Vector2(16f, 12f);
            loadingRect.sizeDelta        = new Vector2(420f, 28f);

            var loadingTmp = loadingRect.gameObject.AddComponent<TextMeshProUGUI>();
            loadingTmp.text               = string.Empty;
            loadingTmp.fontSize           = 16f;
            loadingTmp.color              = new Color(0.76f, 0.87f, 1f, 0.92f);
            loadingTmp.alignment          = TextAlignmentOptions.MidlineLeft;
            loadingTmp.enableWordWrapping = true;
            loadingTmp.overflowMode       = TextOverflowModes.Overflow;
            loadingRect.gameObject.SetActive(false);

            // ── Hint label ────────────────────────────────────────────────────
            // Named "HintLabel" exactly — DialogueChoiceUI.ResolveHintText() looks for this.
            var hintRect = MakeRect("HintLabel", panel);
            SetAnchors(hintRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));
            hintRect.anchoredPosition = new Vector2(-16f, 12f);
            hintRect.sizeDelta        = new Vector2(-200f, 28f);

            var hintTmp = hintRect.gameObject.AddComponent<TextMeshProUGUI>();
            hintTmp.text               = string.Empty;
            hintTmp.fontSize           = 16f;
            hintTmp.color              = new Color(1f, 1f, 1f, 0.55f);
            hintTmp.alignment          = TextAlignmentOptions.MidlineRight;
            hintTmp.enableWordWrapping = true;
            hintTmp.overflowMode       = TextOverflowModes.Overflow;

            // ── Choice container (sits above the panel, filled at runtime) ────
            var choiceRect = MakeRect("ChoiceContainer", rootGo.transform);
            SetAnchors(choiceRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            choiceRect.anchoredPosition = new Vector2(0f, PanelBottomPad + PanelHeight + ChoiceGap);
            choiceRect.sizeDelta        = new Vector2(PanelWidth - 16f, 0f);

            // ── DialogueChoiceUI ──────────────────────────────────────────────
            var ui = rootGo.AddComponent<DialogueChoiceUI>();

            // LLMConversationController lives in Town.unity (loaded additively at runtime).
            // DialogueChoiceUI resolves it via SceneManager.sceneLoaded — leave null here.
            LLMConversationController llm = null;

            var so = new SerializedObject(ui);
            so.FindProperty("conversation").objectReferenceValue    = llm;
            so.FindProperty("dialogueCanvas").objectReferenceValue  = canvas;
            so.FindProperty("dialoguePanel").objectReferenceValue   = panel.gameObject;
            so.FindProperty("speakerNameText").objectReferenceValue = speakerTmp;
            so.FindProperty("dialogueText").objectReferenceValue    = dialogueTmp;
            so.FindProperty("choiceContainer").objectReferenceValue = choiceRect;
            so.FindProperty("loadingText").objectReferenceValue     = loadingTmp;
            so.FindProperty("hintText").objectReferenceValue        = hintTmp;
            so.ApplyModifiedProperties();

            // Move the new root into CoreScene explicitly
            SceneManager.MoveGameObjectToScene(rootGo, coreScene);

            Selection.activeGameObject = rootGo;
            EditorUtility.SetDirty(rootGo);
            EditorSceneManager.SaveScene(coreScene);

            Debug.Log("[TownDialogueHudBuilder] Dialogue HUD built in CoreScene. " +
                      "LLMConversationController will be wired at runtime when Town.unity loads.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        private static void SetAnchors(RectTransform r, Vector2 min, Vector2 max, Vector2 pivot)
        {
            r.anchorMin = min;
            r.anchorMax = max;
            r.pivot     = pivot;
        }
    }
}
