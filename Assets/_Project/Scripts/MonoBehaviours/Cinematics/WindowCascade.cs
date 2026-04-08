using System;
using System.Collections;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Enables a set of lights sequentially sorted by distance from an origin point,
    /// creating a cascade/wave effect (e.g., windows lighting up across a building).
    /// Uses Time.unscaledDeltaTime for delays.
    /// </summary>
    public class WindowCascade : MonoBehaviour
    {
        [SerializeField] private Light[] windowLights = Array.Empty<Light>();

        private Coroutine cascadeCoroutine;

        /// <summary>
        /// Sorts lights by distance from origin and enables them sequentially with a delay.
        /// </summary>
        /// <param name="origin">The point from which distances are measured.</param>
        /// <param name="delayPerLight">Delay in seconds between each light activation.</param>
        public void Trigger(Vector3 origin, float delayPerLight = 0.2f)
        {
            if (cascadeCoroutine != null)
                StopCoroutine(cascadeCoroutine);

            cascadeCoroutine = StartCoroutine(CascadeCoroutine(origin, delayPerLight));
        }

        /// <summary>
        /// Cancels the cascade, leaving lights in their current state.
        /// </summary>
        public void Stop()
        {
            if (cascadeCoroutine != null)
            {
                StopCoroutine(cascadeCoroutine);
                cascadeCoroutine = null;
            }
        }

        /// <summary>
        /// Disables all window lights.
        /// </summary>
        public void ResetAll()
        {
            Stop();
            for (int i = 0; i < windowLights.Length; i++)
            {
                if (windowLights[i] != null)
                    windowLights[i].enabled = false;
            }
        }

        private IEnumerator CascadeCoroutine(Vector3 origin, float delayPerLight)
        {
            if (windowLights == null || windowLights.Length == 0)
            {
                cascadeCoroutine = null;
                yield break;
            }

            // Build sorted indices by distance from origin
            int[] indices = new int[windowLights.Length];
            float[] distances = new float[windowLights.Length];
            for (int i = 0; i < windowLights.Length; i++)
            {
                indices[i] = i;
                distances[i] = windowLights[i] != null
                    ? Vector3.Distance(windowLights[i].transform.position, origin)
                    : float.MaxValue;
            }

            // Simple insertion sort (small array, no allocations beyond the arrays above)
            for (int i = 1; i < indices.Length; i++)
            {
                int keyIdx = indices[i];
                float keyDist = distances[i];
                int j = i - 1;
                while (j >= 0 && distances[j] > keyDist)
                {
                    indices[j + 1] = indices[j];
                    distances[j + 1] = distances[j];
                    j--;
                }
                indices[j + 1] = keyIdx;
                distances[j + 1] = keyDist;
            }

            // Enable lights sequentially with unscaled delay
            for (int i = 0; i < indices.Length; i++)
            {
                var light = windowLights[indices[i]];
                if (light != null)
                    light.enabled = true;

                if (delayPerLight > 0f && i < indices.Length - 1)
                {
                    float waited = 0f;
                    while (waited < delayPerLight)
                    {
                        waited += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }
            }

            cascadeCoroutine = null;
        }
    }
}
