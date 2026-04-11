using System.Collections;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Shakes a UI RectTransform when this GameObject is activated
    /// (e.g. by a Timeline Activation Track). Works for Screen Space canvases
    /// where camera-based shake has no visible effect on UI.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SlideShakeTrigger : MonoBehaviour
    {
        [Header("Shake Settings")]
        [SerializeField] private float intensity = 15f;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private float frequency = 25f;

        [Header("Target")]
        [Tooltip("The RectTransform to shake. If empty, shakes this object's RectTransform.")]
        [SerializeField] private RectTransform target;

        private Coroutine shakeCoroutine;
        private Vector2 originalPosition;

        private void OnEnable()
        {
            if (target == null)
                target = GetComponent<RectTransform>();

            originalPosition = target.anchoredPosition;
            shakeCoroutine = StartCoroutine(ShakeCoroutine());
        }

        private void OnDisable()
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;
            }

            if (target != null)
                target.anchoredPosition = originalPosition;
        }

        private IEnumerator ShakeCoroutine()
        {
            float elapsed = 0f;
            float interval = 1f / frequency;
            float timer = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                timer += Time.deltaTime;

                if (timer >= interval)
                {
                    timer -= interval;
                    float decay = 1f - (elapsed / duration);
                    Vector2 offset = Random.insideUnitCircle * intensity * decay;
                    target.anchoredPosition = originalPosition + offset;
                }

                yield return null;
            }

            target.anchoredPosition = originalPosition;
            shakeCoroutine = null;
        }
    }
}
