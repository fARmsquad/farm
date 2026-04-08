using System.Collections;
using UnityEngine;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.MonoBehaviours.Autoplay
{
    public class AutoplayLighting : AutoplayBase
    {
        private void Awake()
        {
            specId = "INT-009";
            specTitle = "Lighting Presets & Transitions";
            totalSteps = 5;
        }

        protected override IEnumerator RunDemo()
        {
            Step("Finding lighting systems");
            var transition = FindAnyObjectByType<LightingTransition>();
            var cascade = FindAnyObjectByType<WindowCascade>();

            if (transition == null)
            {
                currentLabel = "LightingTransition not found!";
                yield break;
            }

            Debug.Log($"[AutoplayLighting] transition={transition != null}, cascade={cascade != null}");
            yield return Wait(1.5f);

            // Build runtime presets
            var nightPreset = CreateNightPreset();
            var dawnPreset = CreateDawnPreset();

            Step("Applying Night preset");
            transition.Play(nightPreset, 0.1f);
            yield return Wait(2f);

            Step("Transitioning Night → Dawn (5s)");
            transition.Play(nightPreset, dawnPreset, 5f);
            yield return Wait(6f);

            Step("Window cascade");
            if (cascade != null)
            {
                cascade.Trigger(Vector3.zero, 0.3f);
                Debug.Log("[AutoplayLighting] Window cascade triggered.");
            }
            else
            {
                Debug.Log("[AutoplayLighting] No WindowCascade in scene, skipping.");
            }
            yield return Wait(3f);

            Step("Restoring original lighting");
            transition.Play(dawnPreset, 0.1f);
            yield return Wait(2f);
        }

        private static LightingPreset CreateNightPreset()
        {
            var preset = ScriptableObject.CreateInstance<LightingPreset>();
            preset.name = "Night";
            preset.ambientColor = new Color(0.05f, 0.05f, 0.15f);
            preset.ambientIntensity = 0.1f;
            preset.directionalColor = new Color(0.3f, 0.3f, 0.5f);
            preset.directionalIntensity = 0.2f;
            preset.directionalRotation = new Vector3(30f, -150f, 0f);
            preset.fogColor = new Color(0.05f, 0.05f, 0.15f);
            preset.fogDensity = 0.03f;
            preset.skyboxTint = new Color(0.05f, 0.05f, 0.1f);
            return preset;
        }

        private static LightingPreset CreateDawnPreset()
        {
            var preset = ScriptableObject.CreateInstance<LightingPreset>();
            preset.name = "Dawn";
            preset.ambientColor = new Color(0.8f, 0.6f, 0.3f);
            preset.ambientIntensity = 0.6f;
            preset.directionalColor = new Color(1f, 0.95f, 0.8f);
            preset.directionalIntensity = 1.2f;
            preset.directionalRotation = new Vector3(50f, -30f, 0f);
            preset.fogColor = new Color(0.8f, 0.6f, 0.3f);
            preset.fogDensity = 0.01f;
            preset.skyboxTint = new Color(0.8f, 0.6f, 0.4f);
            return preset;
        }
    }
}
