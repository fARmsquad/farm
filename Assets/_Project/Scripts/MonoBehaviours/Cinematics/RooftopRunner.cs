using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Simple A-to-B movement for a rooftop runner silhouette.
    /// </summary>
    public class RooftopRunner : MonoBehaviour
    {
        public UnityEvent OnRunComplete;

        private void Awake()
        {
            if (OnRunComplete == null) OnRunComplete = new UnityEvent();

            // Placeholder visual — black unlit flattened capsule
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "RunnerVisual";
            capsule.transform.SetParent(transform, false);
            capsule.transform.localScale = new Vector3(0.3f, 0.15f, 0.6f);
            capsule.transform.localPosition = Vector3.zero;
            var capsuleRenderer = capsule.GetComponent<Renderer>();
            if (capsuleRenderer != null)
            {
                capsuleRenderer.material.color = Color.black;
                // Unlit appearance — set emission to black and remove specular
                capsuleRenderer.material.SetColor("_EmissionColor", Color.black);
            }
            var capsuleCollider = capsule.GetComponent<Collider>();
            if (capsuleCollider != null) Object.Destroy(capsuleCollider);
        }

        /// <summary>
        /// Run from start to end position over the given duration.
        /// Uses Time.unscaledDeltaTime for animation.
        /// </summary>
        public void Run(Vector3 start, Vector3 end, float duration = 1.5f)
        {
            StartCoroutine(RunRoutine(start, end, duration));
        }

        private IEnumerator RunRoutine(Vector3 start, Vector3 end, float duration)
        {
            transform.position = start;

            // Face the movement direction
            Vector3 dir = (end - start).normalized;
            if (dir.sqrMagnitude > 0.001f)
                transform.forward = dir;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }

            // Snap to end
            transform.position = end;

            OnRunComplete?.Invoke();
        }
    }
}
