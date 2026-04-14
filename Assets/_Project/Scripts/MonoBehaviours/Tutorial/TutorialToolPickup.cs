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
        public bool IsCollected { get; private set; }

        public void Configure(string displayName)
        {
            toolName = string.IsNullOrWhiteSpace(displayName) ? "Tool" : displayName.Trim();
        }

        public bool Collect()
        {
            if (IsCollected)
                return false;

            IsCollected = true;
            gameObject.SetActive(false);
            return true;
        }
    }
}
