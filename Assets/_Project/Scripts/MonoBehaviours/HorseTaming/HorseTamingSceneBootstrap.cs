using UnityEngine;

namespace FarmSimVR.MonoBehaviours.HorseTaming
{
    /// <summary>
    /// Minimal scene entry: builds the paddock on Awake if not already present (editor-baked scenes skip this).
    /// </summary>
    public sealed class HorseTamingSceneBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            HorseTamingWorldBuilder.BuildIfNeeded();
        }
    }
}
