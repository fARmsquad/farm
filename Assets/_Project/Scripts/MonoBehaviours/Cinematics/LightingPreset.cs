using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// ScriptableObject defining a complete lighting state: ambient, directional light,
    /// fog, and skybox tint. Used by LightingTransition to interpolate between states.
    /// </summary>
    [CreateAssetMenu(fileName = "LightingPreset", menuName = "FarmSimVR/Lighting Preset")]
    public class LightingPreset : ScriptableObject
    {
        [Header("Ambient")]
        public Color ambientColor = new Color(0.2f, 0.2f, 0.2f);
        public float ambientIntensity = 1f;

        [Header("Directional Light")]
        public Color directionalColor = Color.white;
        public float directionalIntensity = 1f;
        public Vector3 directionalRotation = new Vector3(50f, -30f, 0f);

        [Header("Fog")]
        public Color fogColor = new Color(0.5f, 0.5f, 0.5f);
        public float fogDensity = 0.01f;

        [Header("Skybox")]
        public Color skyboxTint = new Color(0.5f, 0.5f, 0.5f);
    }
}
