using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Self-contained IMGUI skip prompt. Shows "Hold [Space] to skip" in the
    /// bottom-right corner after a configurable delay. Fires OnSkipRequested
    /// when the player holds Space long enough.
    /// </summary>
    public class SkipPrompt : MonoBehaviour
    {
        [SerializeField] float showDelay = 3f;
        [SerializeField] float holdDuration = 1.5f;

        public UnityEvent OnSkipRequested;

        public bool IsActive { get; private set; }

        private float activatedTime;
        private float holdProgress;
        private bool skipFired;

        public void Activate()
        {
            IsActive = true;
            activatedTime = Time.time;
            holdProgress = 0f;
            skipFired = false;
        }

        public void Deactivate()
        {
            IsActive = false;
            holdProgress = 0f;
            skipFired = false;
        }

        private void Update()
        {
            if (!IsActive || skipFired) return;

            // Wait for show delay before accepting input
            if (Time.time - activatedTime < showDelay) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.spaceKey.isPressed)
            {
                holdProgress += Time.deltaTime;

                if (holdProgress >= holdDuration)
                {
                    skipFired = true;
                    Debug.Log("[SkipPrompt] Skip requested!");
                    OnSkipRequested?.Invoke();
                }
            }
            else
            {
                holdProgress = 0f;
            }
        }

        private void OnGUI()
        {
            if (!IsActive || skipFired) return;
            if (Time.time - activatedTime < showDelay) return;

            // ── Layout ───────────────────────────────────────
            float margin = 20f;
            float boxW = 280f;
            float boxH = 60f;
            float x = Screen.width - boxW - margin;
            float y = Screen.height - boxH - margin;

            // ── Alpha: 40% idle -> 100% while held ──────────
            float normalizedProgress = Mathf.Clamp01(holdProgress / holdDuration);
            float alpha = Mathf.Lerp(0.4f, 1f, normalizedProgress);

            Color prevColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);

            GUI.Box(new Rect(x, y, boxW, boxH), "");

            // ── Prompt text ──────────────────────────────────
            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = new Color(1f, 1f, 1f, alpha);

            GUI.Label(new Rect(x, y + 4f, boxW, 24f), "Hold [Space] to skip", labelStyle);

            // ── Progress bar as [###---] ─────────────────────
            int totalSegments = 10;
            int filledSegments = Mathf.RoundToInt(normalizedProgress * totalSegments);
            string filled = new string('#', filledSegments);
            string empty = new string('-', totalSegments - filledSegments);
            string progressBar = $"[{filled}{empty}]";

            var barStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            barStyle.normal.textColor = new Color(1f, 1f, 0.6f, alpha);

            GUI.Label(new Rect(x, y + 28f, boxW, 26f), progressBar, barStyle);

            GUI.color = prevColor;
        }
    }
}
