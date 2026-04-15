using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using FarmSimVR.MonoBehaviours.Mailbox;

namespace FarmSimVR.Editor
{
    public static class CreateMailboxUI
    {
        private const string CORE_SCENE_PATH = "Assets/_Project/Scenes/CoreScene.unity";

        [MenuItem("FarmSimVR/Mailbox/Wire Up Mailbox in CoreScene")]
        public static void WireUp()
        {
            var scene = EditorSceneManager.OpenScene(CORE_SCENE_PATH, OpenSceneMode.Single);

            // ── Remove any existing mailbox objects so re-running is safe ─────
            foreach (var existing in Object.FindObjectsByType<MailGeneratorDriver>(FindObjectsSortMode.None))
                Object.DestroyImmediate(existing.gameObject);
            foreach (var existing in Object.FindObjectsByType<MailboxNotificationBadge>(FindObjectsSortMode.None))
                Object.DestroyImmediate(existing.gameObject);
            foreach (var existing in Object.FindObjectsByType<MailboxPanelController>(FindObjectsSortMode.None))
                Object.DestroyImmediate(existing.gameObject);

            // ── 0. Ensure EventSystem exists (required for button clicks) ─────
            EnsureEventSystem();

            // ── 1. MailboxManager GameObject ─────────────────────────────────
            var managerGO = new GameObject("MailboxManager");
            var driver    = managerGO.AddComponent<MailGeneratorDriver>();
            var llmClient = managerGO.AddComponent<MailLLMClient>();

            // Wire llmClient into driver via SerializedObject
            var so = new SerializedObject(driver);
            so.FindProperty("llmClient").objectReferenceValue = llmClient;
            so.ApplyModifiedProperties();

            // ── 2. Find or create HUD Canvas ──────────────────────────────────
            var canvas = FindOrCreateHUDCanvas();

            // ── 3. Notification Badge (top-right corner) ──────────────────────
            var badgeGO  = new GameObject("MailboxBadge");
            badgeGO.transform.SetParent(canvas.transform, false);

            var badgeRect = badgeGO.AddComponent<RectTransform>();
            badgeRect.anchorMin        = new Vector2(1f, 1f);
            badgeRect.anchorMax        = new Vector2(1f, 1f);
            badgeRect.pivot            = new Vector2(1f, 1f);
            badgeRect.anchoredPosition = new Vector2(-20f, -20f);
            badgeRect.sizeDelta        = new Vector2(80f, 80f);

            var badgeBg  = badgeGO.AddComponent<Image>();
            badgeBg.color = new Color(0.18f, 0.32f, 0.18f, 0.92f);

            var badgeBtn = badgeGO.AddComponent<Button>();
            var badgeNav = badgeBtn.navigation;
            badgeNav.mode = Navigation.Mode.None;
            badgeBtn.navigation = badgeNav;

            // Envelope body rect
            var envGO   = new GameObject("EnvelopeBody");
            envGO.transform.SetParent(badgeGO.transform, false);
            var envRect = envGO.AddComponent<RectTransform>();
            envRect.anchorMin        = new Vector2(0.5f, 0.5f);
            envRect.anchorMax        = new Vector2(0.5f, 0.5f);
            envRect.pivot            = new Vector2(0.5f, 0.5f);
            envRect.anchoredPosition = new Vector2(0f, 8f);
            envRect.sizeDelta        = new Vector2(46f, 30f);
            envGO.AddComponent<Image>().color = new Color(0.88f, 0.82f, 0.68f);

            // Flap V — left diagonal
            var flapL     = new GameObject("FlapL");
            flapL.transform.SetParent(envGO.transform, false);
            var flapLRect = flapL.AddComponent<RectTransform>();
            flapLRect.anchorMin        = new Vector2(0f, 0.5f);
            flapLRect.anchorMax        = new Vector2(0f, 0.5f);
            flapLRect.pivot            = new Vector2(0f, 0.5f);
            flapLRect.anchoredPosition = new Vector2(0f, 7f);
            flapLRect.sizeDelta        = new Vector2(26f, 2f);
            flapL.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
            flapL.AddComponent<Image>().color = new Color(0.45f, 0.40f, 0.32f);

            // Flap V — right diagonal
            var flapR     = new GameObject("FlapR");
            flapR.transform.SetParent(envGO.transform, false);
            var flapRRect = flapR.AddComponent<RectTransform>();
            flapRRect.anchorMin        = new Vector2(1f, 0.5f);
            flapRRect.anchorMax        = new Vector2(1f, 0.5f);
            flapRRect.pivot            = new Vector2(1f, 0.5f);
            flapRRect.anchoredPosition = new Vector2(0f, 7f);
            flapRRect.sizeDelta        = new Vector2(26f, 2f);
            flapR.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
            flapR.AddComponent<Image>().color = new Color(0.45f, 0.40f, 0.32f);

            // Count label — bottom of badge
            var countGO   = new GameObject("CountLabel");
            countGO.transform.SetParent(badgeGO.transform, false);
            var countText = countGO.AddComponent<TextMeshProUGUI>();
            countText.text      = string.Empty;
            countText.fontSize  = 16f;
            countText.color     = Color.white;
            countText.fontStyle = FontStyles.Bold;
            countText.alignment = TextAlignmentOptions.Center;
            var countRect = countText.GetComponent<RectTransform>();
            countRect.anchorMin        = new Vector2(0f, 0f);
            countRect.anchorMax        = new Vector2(1f, 0.28f);
            countRect.offsetMin        = Vector2.zero;
            countRect.offsetMax        = Vector2.zero;

            // ── 4. Mailbox Panel ──────────────────────────────────────────────
            var panelGO   = new GameObject("MailboxPanel");
            panelGO.transform.SetParent(canvas.transform, false);
            panelGO.SetActive(false);

            // Stretch to fill ~90% of screen so it reads well at any resolution
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.15f, 0.10f);
            panelRect.anchorMax = new Vector2(0.85f, 0.90f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelBg = panelGO.AddComponent<Image>();
            panelBg.color = new Color(0.12f, 0.10f, 0.08f, 0.97f);

            // Panel title
            var titleGO   = new GameObject("Title");
            titleGO.transform.SetParent(panelGO.transform, false);
            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text      = "Mailbox";
            titleText.fontSize  = 28f;
            titleText.color     = new Color(0.95f, 0.88f, 0.70f);
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin        = new Vector2(0f, 1f);
            titleRect.anchorMax        = new Vector2(1f, 1f);
            titleRect.pivot            = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -10f);
            titleRect.sizeDelta        = new Vector2(0f, 40f);

            // Close button
            var closeBtnGO  = CreateButton("CloseButton", panelGO.transform, "X",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-8f, -8f), new Vector2(36f, 36f));

            // List pane (left half)
            var listPaneGO   = new GameObject("ListPane");
            listPaneGO.transform.SetParent(panelGO.transform, false);
            var listPaneRect = listPaneGO.AddComponent<RectTransform>();
            listPaneRect.anchorMin = new Vector2(0f,    0f);
            listPaneRect.anchorMax = new Vector2(0.30f, 1f);
            listPaneRect.offsetMin = new Vector2(10f,  10f);
            listPaneRect.offsetMax = new Vector2(-5f, -55f);
            var listPaneBg = listPaneGO.AddComponent<Image>();
            listPaneBg.color = new Color(0.08f, 0.07f, 0.06f, 0.8f);

            // Scroll view inside list pane
            var scrollGO   = new GameObject("ScrollView");
            scrollGO.transform.SetParent(listPaneGO.transform, false);
            var scrollRect2 = scrollGO.AddComponent<RectTransform>();
            scrollRect2.anchorMin = Vector2.zero;
            scrollRect2.anchorMax = Vector2.one;
            scrollRect2.offsetMin = new Vector2(4f, 4f);
            scrollRect2.offsetMax = new Vector2(-4f, -4f);
            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var contentGO   = new GameObject("Content");
            contentGO.transform.SetParent(scrollGO.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot     = new Vector2(0f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 0f);
            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing           = 8f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRect;

            // ── 5. Message Row Prefab ─────────────────────────────────────────
            var rowPrefabGO = new GameObject("MessageRowPrefab");
            rowPrefabGO.transform.SetParent(panelGO.transform, false);
            rowPrefabGO.SetActive(false);

            var rowRect = rowPrefabGO.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, 84f);

            var rowBg = rowPrefabGO.AddComponent<Image>();
            rowBg.color = new Color(0.15f, 0.13f, 0.11f, 1f);
            rowPrefabGO.AddComponent<Button>();

            var rowHlg = rowPrefabGO.AddComponent<HorizontalLayoutGroup>();
            rowHlg.padding               = new RectOffset(14, 14, 12, 12);
            rowHlg.spacing               = 10f;
            rowHlg.childForceExpandWidth  = false;
            rowHlg.childForceExpandHeight = true;
            rowHlg.childAlignment         = TextAnchor.UpperLeft;

            // UnreadDot — centred vertically on the left
            var dotGO     = new GameObject("UnreadDot");
            dotGO.transform.SetParent(rowPrefabGO.transform, false);
            var dotImg    = dotGO.AddComponent<Image>();
            dotImg.color  = new Color(0.4f, 0.9f, 0.4f);
            var dotLayout = dotGO.AddComponent<LayoutElement>();
            dotLayout.minWidth        = 8f;
            dotLayout.preferredWidth  = 8f;
            dotLayout.flexibleWidth   = 0f;
            dotLayout.minHeight       = 8f;
            dotLayout.preferredHeight = 8f;
            dotLayout.flexibleHeight  = 0f;

            // Text group: Subject (bold) on top, Sender (dim) below
            var textsGO     = new GameObject("Texts");
            textsGO.transform.SetParent(rowPrefabGO.transform, false);
            var textsVlg    = textsGO.AddComponent<VerticalLayoutGroup>();
            textsVlg.spacing               = 3f;
            textsVlg.childForceExpandWidth  = true;
            textsVlg.childForceExpandHeight = false;
            textsVlg.childAlignment         = TextAnchor.UpperLeft;
            var textsLayout = textsGO.AddComponent<LayoutElement>();
            textsLayout.flexibleWidth = 1f;

            // Subject label — primary, bold, full row width, ellipsis overflow
            var subjectGO  = new GameObject("SubjectLabel");
            subjectGO.transform.SetParent(textsGO.transform, false);
            var subjectTxt = subjectGO.AddComponent<TextMeshProUGUI>();
            subjectTxt.text               = "Subject";
            subjectTxt.fontSize           = 16f;
            subjectTxt.fontStyle          = FontStyles.Bold;
            subjectTxt.color              = new Color(0.95f, 0.90f, 0.80f);
            subjectTxt.alignment          = TextAlignmentOptions.Left;
            subjectTxt.overflowMode       = TextOverflowModes.Truncate;
            subjectTxt.enableWordWrapping = true;
            subjectTxt.maxVisibleLines    = 2;
            subjectGO.AddComponent<LayoutElement>().preferredHeight = 36f;

            // Sender label — secondary, dimmer, smaller
            var senderGO  = new GameObject("SenderLabel");
            senderGO.transform.SetParent(textsGO.transform, false);
            var senderTxt = senderGO.AddComponent<TextMeshProUGUI>();
            senderTxt.text               = "Sender";
            senderTxt.fontSize           = 13f;
            senderTxt.color              = new Color(0.60f, 0.56f, 0.48f);
            senderTxt.alignment          = TextAlignmentOptions.Left;
            senderTxt.overflowMode       = TextOverflowModes.Ellipsis;
            senderTxt.enableWordWrapping = false;

            // ── 6. Detail pane (right half) ───────────────────────────────────
            var detailPaneGO   = new GameObject("DetailPane");
            detailPaneGO.transform.SetParent(panelGO.transform, false);
            detailPaneGO.SetActive(false);

            var detailPaneRect = detailPaneGO.AddComponent<RectTransform>();
            detailPaneRect.anchorMin = new Vector2(0.30f, 0f);
            detailPaneRect.anchorMax = new Vector2(1f,    1f);
            detailPaneRect.offsetMin = new Vector2(5f,   10f);
            detailPaneRect.offsetMax = new Vector2(-10f, -55f);
            var detailPaneBg = detailPaneGO.AddComponent<Image>();
            detailPaneBg.color = new Color(0.08f, 0.07f, 0.06f, 0.8f);

            var detailLayout = detailPaneGO.AddComponent<VerticalLayoutGroup>();
            detailLayout.padding               = new RectOffset(24, 24, 20, 20);
            detailLayout.spacing               = 12f;
            detailLayout.childForceExpandWidth  = true;
            detailLayout.childForceExpandHeight = false;
            detailLayout.childControlWidth      = true;
            detailLayout.childControlHeight     = true;

            // Subject — single prominent line, no wrapping
            var subjectDetailTxt = CreateDetailText("SubjectLabel", detailPaneGO.transform, "", 22f, FontStyles.Bold);
            subjectDetailTxt.enableWordWrapping = false;
            subjectDetailTxt.overflowMode       = TextOverflowModes.Ellipsis;
            subjectDetailTxt.GetComponent<LayoutElement>().minHeight = 30f;

            // Sender — smaller subtitle below subject
            var senderDetailTxt = CreateDetailText("SenderLabel", detailPaneGO.transform, "", 15f, FontStyles.Normal);
            senderDetailTxt.color = new Color(0.60f, 0.56f, 0.48f);
            senderDetailTxt.enableWordWrapping = false;

            // Divider
            var dividerGO = new GameObject("Divider");
            dividerGO.transform.SetParent(detailPaneGO.transform, false);
            var dividerImg = dividerGO.AddComponent<Image>();
            dividerImg.color = new Color(0.4f, 0.35f, 0.28f, 0.5f);
            var dividerLayout = dividerGO.AddComponent<LayoutElement>();
            dividerLayout.minHeight       = 2f;
            dividerLayout.preferredHeight = 2f;

            // Body — fills remaining space, wraps naturally with paragraph spacing
            var bodyDetailTxt = CreateDetailText("BodyLabel", detailPaneGO.transform, "", 18f, FontStyles.Normal);
            bodyDetailTxt.lineSpacing      = 2f;
            bodyDetailTxt.paragraphSpacing = 8f;
            bodyDetailTxt.GetComponent<LayoutElement>().flexibleHeight = 1f;

            // Claim button — hidden by default, shown when attachment is unclaimed
            var claimBtnGO = CreateButton("ClaimButton", detailPaneGO.transform, "Claim Attachment",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(0f, 44f));
            claimBtnGO.SetActive(false);
            var claimLabelComp = claimBtnGO.GetComponentInChildren<TextMeshProUGUI>();

            // ── 7. Wire MailboxPanelController ────────────────────────────────
            var panelCtrl = panelGO.AddComponent<MailboxPanelController>();
            var pso = new SerializedObject(panelCtrl);
            pso.FindProperty("panelRoot").objectReferenceValue         = panelGO;
            pso.FindProperty("closeButton").objectReferenceValue       = closeBtnGO.GetComponent<Button>();
            pso.FindProperty("listContainer").objectReferenceValue     = contentGO.transform;
            pso.FindProperty("messageRowPrefab").objectReferenceValue  = rowPrefabGO;
            pso.FindProperty("detailRoot").objectReferenceValue        = detailPaneGO;
            pso.FindProperty("detailSubject").objectReferenceValue     = subjectDetailTxt;
            pso.FindProperty("detailSender").objectReferenceValue      = senderDetailTxt;
            pso.FindProperty("detailBody").objectReferenceValue        = bodyDetailTxt;
            pso.FindProperty("claimButton").objectReferenceValue       = claimBtnGO.GetComponent<Button>();
            pso.FindProperty("claimLabel").objectReferenceValue        = claimLabelComp;
            pso.ApplyModifiedProperties();

            // ── 8. Wire MailboxNotificationBadge ─────────────────────────────
            var badge = badgeGO.AddComponent<MailboxNotificationBadge>();
            var bso = new SerializedObject(badge);
            bso.FindProperty("countLabel").objectReferenceValue = countText;
            bso.FindProperty("panel").objectReferenceValue      = panelCtrl;
            bso.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[CreateMailboxUI] Mailbox wired up in CoreScene. Assign your API key on MailLLMClient.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static Canvas FindOrCreateHUDCanvas()
        {
            // Look for the dedicated mailbox canvas by name — never reuse DialogueCanvas
            // or any other canvas that might be World Space / Screen Space - Camera.
            const string canvasName = "MailboxHUDCanvas";
            var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in allCanvases)
            {
                if (c.gameObject.name == canvasName)
                    return c;
            }

            var go     = new GameObject(canvasName);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;          // always on top
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static TextMeshProUGUI CreateDetailText(
            string name, Transform parent, string text, float size, FontStyles style)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = size;
            tmp.fontStyle = style;
            tmp.color     = new Color(0.9f, 0.85f, 0.75f);
            tmp.enableWordWrapping = true;
            go.AddComponent<LayoutElement>();
            return tmp;
        }

        private static GameObject CreateButton(
            string name, Transform parent, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img  = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.42f, 0.25f);
            go.AddComponent<Button>();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin        = anchorMin;
            rect.anchorMax        = anchorMax;
            rect.pivot            = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta        = sizeDelta;

            var lblGO  = new GameObject("Label");
            lblGO.transform.SetParent(go.transform, false);
            var lbl    = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text      = label;
            lbl.fontSize  = 16f;
            lbl.color     = Color.white;
            lbl.alignment = TextAlignmentOptions.Center;
            var lblRect = lbl.GetComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = Vector2.one;
            lblRect.offsetMin = Vector2.zero;
            lblRect.offsetMax = Vector2.zero;

            return go;
        }
    }
}
