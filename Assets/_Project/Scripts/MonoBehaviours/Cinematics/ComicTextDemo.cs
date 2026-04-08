using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using FarmSimVR.MonoBehaviours.Debugging;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Debug overlay for testing ComicTextManager features.
    /// Toggle with Shift+C. Number keys 1-5 trigger actions.
    /// </summary>
    public class ComicTextDemo : MonoBehaviour
    {
        private ComicTextManager comicTextManager;
        private static readonly Key Panel = Key.C;

        private void Start()
        {
            comicTextManager = FindAnyObjectByType<ComicTextManager>();
            Debug.Log($"[ComicTextDemo] Start — comicTextManager={(comicTextManager != null ? "found" : "NULL")}");
        }

        private void Update()
        {
            if (!DebugPanelShortcuts.UpdateToggle(Panel)) return;

            if (comicTextManager == null)
                comicTextManager = FindAnyObjectByType<ComicTextManager>();

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit1))
            {
                Debug.Log("[ComicText] Show Panel Text");
                comicTextManager?.ShowPanelText(
                    "The sun had barely kissed the horizon...",
                    holdDuration: 3f);
            }

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit2))
            {
                Debug.Log("[ComicText] Show Comic Burst");
                comicTextManager?.ShowComicBurst(
                    "COCK-A-DOODLE-DOO!",
                    holdDuration: 2f,
                    fontSize: 72f,
                    color: Color.red,
                    outlineColor: Color.black);
            }

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit3))
            {
                Debug.Log("[ComicText] Show Speech Bubble on Player");
                var player = FindAnyObjectByType<FirstPersonExplorer>();
                if (player != null)
                {
                    comicTextManager?.ShowSpeechBubble(
                        player.transform,
                        "I should explore the farm...",
                        holdDuration: 3f);
                }
                else
                {
                    Debug.LogWarning("[ComicTextDemo] FirstPersonExplorer not found in scene.");
                }
            }

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit4))
            {
                Debug.Log("[ComicText] Show Speech Bubble with Translation");
                var player = FindAnyObjectByType<FirstPersonExplorer>();
                if (player != null)
                {
                    comicTextManager?.ShowSpeechBubble(
                        player.transform,
                        "Bawk bawk BAWK!",
                        translationText: "(Translation: Good morning, humans)",
                        holdDuration: 4f);
                }
                else
                {
                    Debug.LogWarning("[ComicTextDemo] FirstPersonExplorer not found in scene.");
                }
            }

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit5))
            {
                Debug.Log("[ComicText] Hide All");
                comicTextManager?.HideAll();
            }
        }

        private void OnGUI()
        {
            if (!DebugPanelShortcuts.IsPanelActive(Panel)) return;

            float w = 320f;
            float h = 220f;
            float x = 10f;
            float y = (Screen.height - h) / 2f;
            float btnH = 28f;
            float pad = 3f;

            GUI.Box(new Rect(x, y, w, h), "Comic Text (Shift+C)");
            float cy = y + 22f;

            string status = comicTextManager != null ? "Ready" : "NULL";
            GUI.Label(new Rect(x + 4, cy, w - 8, 20f), $"Status: {status}");
            cy += 24f;

            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[1] Show Panel Text"))
            {
                comicTextManager?.ShowPanelText("The sun had barely kissed the horizon...", holdDuration: 3f);
            }
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[2] Show Comic Burst"))
            {
                comicTextManager?.ShowComicBurst("COCK-A-DOODLE-DOO!", holdDuration: 2f,
                    fontSize: 72f, color: Color.red, outlineColor: Color.black);
            }
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[3] Speech Bubble on Player"))
            {
                var player = FindAnyObjectByType<FirstPersonExplorer>();
                if (player != null)
                    comicTextManager?.ShowSpeechBubble(player.transform, "I should explore the farm...", holdDuration: 3f);
            }
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[4] Speech Bubble + Translation"))
            {
                var player = FindAnyObjectByType<FirstPersonExplorer>();
                if (player != null)
                    comicTextManager?.ShowSpeechBubble(player.transform, "Bawk bawk BAWK!",
                        translationText: "(Translation: Good morning, humans)", holdDuration: 4f);
            }
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[5] Hide All"))
            {
                comicTextManager?.HideAll();
            }
        }
    }
}
