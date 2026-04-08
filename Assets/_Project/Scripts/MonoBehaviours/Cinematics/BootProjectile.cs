using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Parabolic arc projectile that moves from start to target over a duration.
    /// </summary>
    public class BootProjectile : MonoBehaviour
    {
        public UnityEvent OnLanded;

        private void Awake()
        {
            if (OnLanded == null) OnLanded = new UnityEvent();

            // Placeholder visual — small brown cube
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "BootVisual";
            cube.transform.SetParent(transform, false);
            cube.transform.localScale = new Vector3(0.15f, 0.1f, 0.25f);
            cube.transform.localPosition = Vector3.zero;
            var cubeRenderer = cube.GetComponent<Renderer>();
            if (cubeRenderer != null)
                cubeRenderer.material.color = new Color(0.4f, 0.25f, 0.1f); // brown
            var cubeCollider = cube.GetComponent<Collider>();
            if (cubeCollider != null) Object.Destroy(cubeCollider);
        }

        /// <summary>
        /// Launch the projectile along a parabolic arc from start to target.
        /// </summary>
        public void Launch(Vector3 start, Vector3 target, float duration = 1.2f, float arcHeight = 5f)
        {
            StartCoroutine(LaunchRoutine(start, target, duration, arcHeight));
        }

        private IEnumerator LaunchRoutine(Vector3 start, Vector3 target, float duration, float arcHeight)
        {
            transform.position = start;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // XZ: linear interpolation
                Vector3 pos = Vector3.Lerp(start, target, t);

                // Y: parabolic arc peaking at midpoint
                pos.y = Mathf.Lerp(start.y, target.y, t) + arcHeight * 4f * t * (1f - t);

                transform.position = pos;

                // Rotate to face movement direction
                if (t < 1f)
                {
                    float nextT = Mathf.Clamp01((elapsed + 0.01f) / duration);
                    Vector3 nextPos = Vector3.Lerp(start, target, nextT);
                    nextPos.y = Mathf.Lerp(start.y, target.y, nextT) + arcHeight * 4f * nextT * (1f - nextT);
                    Vector3 dir = nextPos - pos;
                    if (dir.sqrMagnitude > 0.0001f)
                        transform.rotation = Quaternion.LookRotation(dir);
                }

                yield return null;
            }

            // Snap to final position
            transform.position = target;

            OnLanded?.Invoke();
        }
    }
}
