using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Attaches to the player and creates a swinging lantern visual with flickering light.
    /// </summary>
    public class LanternHolder : MonoBehaviour
    {
        [SerializeField] private Vector3 holdOffset = new Vector3(-0.5f, 0.8f, 0.3f);
        [SerializeField] private float swingAngle = 15f;
        [SerializeField] private float swingSpeed = 4f;

        private Light lanternLight;
        private float baseIntensity = 1.2f;
        private float targetSwingAngle;
        private bool swinging = true;

        private void Start()
        {
            transform.localPosition = holdOffset;

            // Lantern handle — small cylinder
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "LanternHandle";
            handle.transform.SetParent(transform, false);
            handle.transform.localScale = new Vector3(0.05f, 0.3f, 0.05f);
            handle.transform.localPosition = Vector3.zero;
            var handleCollider = handle.GetComponent<Collider>();
            if (handleCollider != null) Object.Destroy(handleCollider);

            // Lantern glass — small sphere
            var glass = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glass.name = "LanternGlass";
            glass.transform.SetParent(transform, false);
            glass.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            glass.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            var glassRenderer = glass.GetComponent<Renderer>();
            if (glassRenderer != null)
            {
                glassRenderer.material.color = new Color(1f, 0.84f, 0.6f, 0.8f); // warm amber
            }
            var glassCollider = glass.GetComponent<Collider>();
            if (glassCollider != null) Object.Destroy(glassCollider);

            // Point light
            var lightGo = new GameObject("LanternLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            lanternLight = lightGo.AddComponent<Light>();
            lanternLight.type = LightType.Point;
            lanternLight.color = new Color(1f, 0.84f, 0.6f); // #FFD699
            lanternLight.range = 5f;
            lanternLight.intensity = baseIntensity;
            lanternLight.shadows = LightShadows.None;

            targetSwingAngle = swingAngle;
        }

        private void Update()
        {
            // Dampen swing angle toward target
            float target = swinging ? 15f : 3f;
            targetSwingAngle = Mathf.MoveTowards(targetSwingAngle, target, 10f * Time.unscaledDeltaTime);

            // Pendulum swing on Z axis
            float angle = Mathf.Sin(Time.time * swingSpeed) * targetSwingAngle;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            // Light flicker
            if (lanternLight != null)
            {
                lanternLight.intensity = baseIntensity + Mathf.PerlinNoise(Time.time * 3f, 0f) * 0.2f - 0.1f;
            }
        }

        /// <summary>
        /// When false, dampen swing angle toward 3 degrees (idle sway).
        /// </summary>
        public void SetSwinging(bool active)
        {
            swinging = active;
        }
    }
}
