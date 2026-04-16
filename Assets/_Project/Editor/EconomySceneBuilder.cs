using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;
using FarmSimVR.MonoBehaviours.Economy;

namespace FarmSimVR.Editor
{
    /// <summary>
    /// Wires the economy systems (EconomyManager + wallet HUD) into the CoreScene.
    /// Eggs are sold in town by talking to Mira the Baker — no farm visit NPC needed.
    /// Menu: FarmSimVR / Economy / Wire Economy in CoreScene
    /// </summary>
    public static class EconomySceneBuilder
    {
        private const string CoreScenePath     = "Assets/_Project/Scenes/CoreScene.unity";
        private const string EconomyRootName   = "EconomyManager";
        private const string EconomyCanvasName = "EconomyHUDCanvas";

        [MenuItem("FarmSimVR/Economy/Wire Economy in CoreScene")]
        public static void WireEconomy()
        {
            var scene = EditorSceneManager.OpenScene(CoreScenePath, OpenSceneMode.Single);

            RemoveExistingEconomyObjects();
            EnsureEventSystem();

            // ── 1. Economy Canvas ──────────────────────────────────────────────
            var canvas = CreateEconomyCanvas();

            // ── 2. Wallet HUD (top-left) ───────────────────────────────────────
            var walletHUDGO = CreateWalletHUD(canvas.transform);
            var walletHUD   = walletHUDGO.GetComponent<WalletHUDController>();

            // ── 3. Economy Manager ─────────────────────────────────────────────
            var managerGO = new GameObject(EconomyRootName);
            var manager   = managerGO.AddComponent<EconomyManager>();

            var mso = new SerializedObject(manager);
            mso.FindProperty("walletHUD").objectReferenceValue = walletHUD;
            mso.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[EconomySceneBuilder] Economy wired. Press F1 in Play mode to skip to next day.");
        }

        // ── Wallet HUD ────────────────────────────────────────────────────────

        private static GameObject CreateWalletHUD(Transform canvasTransform)
        {
            var go  = new GameObject("WalletHUD");
            go.transform.SetParent(canvasTransform, false);
            var hud = go.AddComponent<WalletHUDController>();

            var bg    = go.AddComponent<Image>();
            bg.color  = new Color(0.10f, 0.08f, 0.06f, 0.85f);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin        = new Vector2(0f, 1f);
            rect.anchorMax        = new Vector2(0f, 1f);
            rect.pivot            = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(16f, -16f);
            rect.sizeDelta        = new Vector2(160f, 44f);

            var labelGO  = new GameObject("CoinLabel");
            labelGO.transform.SetParent(go.transform, false);
            var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text      = "Coins: 0";
            labelTMP.fontSize  = 20f;
            labelTMP.fontStyle = FontStyles.Bold;
            labelTMP.color     = new Color(1f, 0.85f, 0.20f);
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
            var labelRect = labelTMP.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 0f);
            labelRect.offsetMax = new Vector2(-8f, 0f);

            var so = new SerializedObject(hud);
            so.FindProperty("coinLabel").objectReferenceValue = labelTMP;
            so.ApplyModifiedProperties();

            return go;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Canvas CreateEconomyCanvas()
        {
            var go     = new GameObject(EconomyCanvasName);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static void RemoveExistingEconomyObjects()
        {
            foreach (var go in Object.FindObjectsByType<EconomyManager>(FindObjectsSortMode.None))
                Object.DestroyImmediate(go.gameObject);

            var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in allCanvases)
                if (c.gameObject.name == EconomyCanvasName)
                    Object.DestroyImmediate(c.gameObject);

            // Remove stale Baker_NPC and ContractPanel objects left over from deleted economy scripts
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allObjects)
                if (go != null && (go.name == "Baker_NPC" || go.name == "ContractPanel"))
                    Object.DestroyImmediate(go);
        }
    }
}
