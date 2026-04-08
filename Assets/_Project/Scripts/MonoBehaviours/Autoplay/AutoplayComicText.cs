using System.Collections;
using UnityEngine;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.MonoBehaviours.Autoplay
{
    public class AutoplayComicText : AutoplayBase
    {
        private void Awake()
        {
            specId = "INT-011";
            specTitle = "Comic Text & Speech Bubbles";
            totalSteps = 5;
        }

        protected override IEnumerator RunDemo()
        {
            Step("Finding ComicTextManager");
            var manager = ComicTextManager.Instance;
            if (manager == null)
                manager = FindAnyObjectByType<ComicTextManager>();

            if (manager == null)
            {
                currentLabel = "ComicTextManager not found!";
                yield break;
            }

            Debug.Log("[AutoplayComicText] ComicTextManager found.");
            yield return Wait(1f);

            Step("Panel text \u2014 narration");
            manager.ShowPanelText("The sun had barely kissed the horizon...", 3f);
            yield return Wait(5f);

            Step("Comic burst \u2014 rooster crow");
            manager.ShowComicBurst("COCK-A-DOODLE-DOO!", 2f, 72, Color.red, Color.black);
            yield return Wait(4f);

            // Find a target for the speech bubble
            Transform target = FindSpeechBubbleTarget();
            if (target == null)
            {
                // Create a dummy target so the demo can proceed
                var dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                dummy.name = "AutoplayBubbleTarget";
                dummy.transform.position = new Vector3(0f, 1f, 3f);
                var col = dummy.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
                target = dummy.transform;
                Debug.Log("[AutoplayComicText] No player found, created dummy target.");
            }

            Step("Speech bubble on target");
            manager.ShowSpeechBubble(target, "I should explore the farm...", null, 3f);
            yield return Wait(5f);

            Step("Speech bubble with translation");
            manager.ShowSpeechBubble(target, "Bawk bawk BAWK!", "(Translation: Good morning, humans)", 3f);
            yield return Wait(5f);
        }

        private Transform FindSpeechBubbleTarget()
        {
            // Try player tag first
            var player = GameObject.FindWithTag("Player");
            if (player != null) return player.transform;

            // Try to find any capsule-like object
            var capsule = FindAnyObjectByType<CapsuleCollider>();
            if (capsule != null) return capsule.transform;

            return null;
        }
    }
}
