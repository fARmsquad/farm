using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    /// <summary>
    /// Marks a GameObject as a tool pickup during the tutorial.
    /// The player interacts with these to collect tools.
    /// </summary>
    public sealed class TutorialToolPickup : MonoBehaviour
    {
        [SerializeField] private string toolName;

        public string ToolName => toolName;

        public void Collect()
        {
            Debug.Log($"[TutorialToolPickup] Collected tool: {toolName}");
            gameObject.SetActive(false);
        }
    }
}
