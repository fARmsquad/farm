using System.Collections;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Autoplay
{
    /// <summary>
    /// Base class for autoplay demos. Shows a HUD with spec name,
    /// current step label, and progress bar. Subclasses implement RunDemo().
    /// </summary>
    public abstract class AutoplayBase : MonoBehaviour
    {
        protected string specId = "";
        protected string specTitle = "";
        protected string currentLabel = "";
        protected int currentStep;
        protected int totalSteps = 1;
        protected bool finished;
        public bool IsFinished => finished;

        protected abstract IEnumerator RunDemo();

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            StartCoroutine(RunWrapper());
        }

        private IEnumerator RunWrapper()
        {
            currentLabel = "Starting...";
            yield return new WaitForSeconds(1.5f);
            yield return RunDemo();
            finished = true;
            currentLabel = "Demo complete!";
            OnDemoComplete();
        }

        /// <summary>
        /// Called once the demo coroutine finishes. Override to hand off control to the player.
        /// </summary>
        protected virtual void OnDemoComplete() { }

        /// <summary>
        /// Immediately ends the demo (e.g. called by a SkipPrompt callback).
        /// Stops all running coroutines and invokes OnDemoComplete.
        /// </summary>
        public void ForceSkip()
        {
            if (finished) return;
            StopAllCoroutines();
            finished     = true;
            currentLabel = "Skipped.";
            OnDemoComplete();
        }

        protected void Step(string label)
        {
            currentStep++;
            currentLabel = label;
        }

        protected IEnumerator Wait(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }

        protected IEnumerator WaitRealtime(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        private void OnGUI()
        {
            if (finished) return;  // hide HUD once player takes control
            // Top bar
            float barH = 56f;
            GUI.color = new Color(0f, 0f, 0f, 0.88f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, barH), Texture2D.whiteTexture);

            // Gold accent line
            GUI.color = new Color(0.72f, 0.53f, 0.04f);
            GUI.DrawTexture(new Rect(0, barH - 3f, Screen.width, 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Spec ID badge
            var badgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.72f, 0.53f, 0.04f) },
                alignment = TextAnchor.MiddleLeft
            };
            GUI.Label(new Rect(16, 6, 120, 20), specId, badgeStyle);

            // Title
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18, fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleLeft
            };
            GUI.Label(new Rect(16, 24, 400, 26), specTitle, titleStyle);

            // Current step label (center)
            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                normal = { textColor = new Color(0.85f, 0.9f, 0.95f) },
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(new Rect(0, 8, Screen.width, 36), currentLabel, labelStyle);

            // Step counter (right)
            var counterStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(1f, 1f, 1f, 0.5f) },
                alignment = TextAnchor.MiddleRight
            };
            string counter = finished ? "DONE" : $"{currentStep} / {totalSteps}";
            GUI.Label(new Rect(Screen.width - 116, 6, 100, 20), counter, counterStyle);

            // Progress bar
            float progress = totalSteps > 0 ? (float)currentStep / totalSteps : 0f;
            GUI.color = new Color(1f, 1f, 1f, 0.1f);
            GUI.DrawTexture(new Rect(0, barH - 3f, Screen.width, 3f), Texture2D.whiteTexture);
            GUI.color = new Color(0.72f, 0.53f, 0.04f);
            GUI.DrawTexture(new Rect(0, barH - 3f, Screen.width * progress, 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // AUTOPLAY badge bottom-right
            var autoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.72f, 0.53f, 0.04f, 0.6f) },
                alignment = TextAnchor.LowerRight
            };
            GUI.Label(new Rect(Screen.width - 130, Screen.height - 28, 120, 20), "AUTOPLAY", autoStyle);
        }
    }
}
