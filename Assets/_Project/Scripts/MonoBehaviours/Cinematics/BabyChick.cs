using System.Collections;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Simple wander + scatter behaviour for a baby chick placeholder.
    /// </summary>
    public class BabyChick : MonoBehaviour
    {
        [SerializeField] private float wanderRadius = 3f;
        [SerializeField] private float wanderSpeed = 0.5f;
        [SerializeField] private float scatterSpeed = 3f;

        private Vector3 spawnPosition;
        private bool isScattering;

        private void Start()
        {
            spawnPosition = transform.position;

            // Body — small yellow sphere
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "ChickBody";
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            body.transform.localPosition = Vector3.zero;
            var bodyRenderer = body.GetComponent<Renderer>();
            if (bodyRenderer != null)
                bodyRenderer.material.color = Color.yellow;
            var bodyCollider = body.GetComponent<Collider>();
            if (bodyCollider != null) Object.Destroy(bodyCollider);

            // Beak — smaller orange sphere
            var beak = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beak.name = "ChickBeak";
            beak.transform.SetParent(transform, false);
            beak.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            beak.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            var beakRenderer = beak.GetComponent<Renderer>();
            if (beakRenderer != null)
                beakRenderer.material.color = new Color(1f, 0.5f, 0f); // orange
            var beakCollider = beak.GetComponent<Collider>();
            if (beakCollider != null) Object.Destroy(beakCollider);

            StartCoroutine(WanderRoutine());
            StartCoroutine(PeckRoutine());
        }

        private IEnumerator WanderRoutine()
        {
            while (true)
            {
                if (isScattering)
                {
                    yield return null;
                    continue;
                }

                // Pick a random wander target
                Vector2 offset = Random.insideUnitCircle * wanderRadius;
                Vector3 target = spawnPosition + new Vector3(offset.x, 0f, offset.y);

                // Sample terrain height
                if (Terrain.activeTerrain != null)
                    target.y = Terrain.activeTerrain.SampleHeight(target);

                // Move toward target
                while (!isScattering && Vector3.Distance(FlatPosition(transform.position), FlatPosition(target)) > 0.1f)
                {
                    Vector3 dir = (target - transform.position).normalized;
                    dir.y = 0f;
                    transform.position += dir * wanderSpeed * Time.unscaledDeltaTime;

                    // Keep on terrain
                    if (Terrain.activeTerrain != null)
                    {
                        Vector3 pos = transform.position;
                        pos.y = Terrain.activeTerrain.SampleHeight(pos);
                        transform.position = pos;
                    }

                    // Face movement direction
                    if (dir.sqrMagnitude > 0.001f)
                        transform.forward = dir;

                    yield return null;
                }

                // Wait 1-3 seconds before picking a new point
                float waitTime = Random.Range(1f, 3f);
                float elapsed = 0f;
                while (elapsed < waitTime && !isScattering)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
        }

        private IEnumerator PeckRoutine()
        {
            while (true)
            {
                float waitTime = Random.Range(2f, 4f);
                float elapsed = 0f;
                while (elapsed < waitTime)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (isScattering) continue;

                // Bob down
                Vector3 startPos = transform.position;
                float peckDuration = 0.3f;
                float peckElapsed = 0f;

                // Down
                while (peckElapsed < peckDuration)
                {
                    peckElapsed += Time.unscaledDeltaTime;
                    float t = peckElapsed / peckDuration;
                    Vector3 pos = transform.position;
                    pos.y = startPos.y - 0.1f * t;
                    transform.position = pos;
                    yield return null;
                }

                // Up
                peckElapsed = 0f;
                while (peckElapsed < peckDuration)
                {
                    peckElapsed += Time.unscaledDeltaTime;
                    float t = peckElapsed / peckDuration;
                    Vector3 pos = transform.position;
                    pos.y = startPos.y - 0.1f * (1f - t);
                    transform.position = pos;
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Flee away from the given point at scatter speed for 1.5 seconds, then resume wandering.
        /// </summary>
        public void Scatter(Vector3 fromPoint)
        {
            StartCoroutine(ScatterRoutine(fromPoint));
        }

        private IEnumerator ScatterRoutine(Vector3 fromPoint)
        {
            isScattering = true;

            Vector3 fleeDir = (transform.position - fromPoint).normalized;
            fleeDir.y = 0f;
            if (fleeDir.sqrMagnitude < 0.001f)
                fleeDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;

            transform.forward = fleeDir;

            float duration = 1.5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.position += fleeDir * scatterSpeed * Time.unscaledDeltaTime;

                // Keep on terrain
                if (Terrain.activeTerrain != null)
                {
                    Vector3 pos = transform.position;
                    pos.y = Terrain.activeTerrain.SampleHeight(pos);
                    transform.position = pos;
                }

                yield return null;
            }

            isScattering = false;
        }

        private static Vector3 FlatPosition(Vector3 p)
        {
            return new Vector3(p.x, 0f, p.z);
        }
    }
}
