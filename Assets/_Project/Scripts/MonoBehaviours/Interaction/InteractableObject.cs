using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Interaction
{
    /// <summary>
    /// Base class for objects the player can interact with by pressing E.
    /// Provides proximity detection, an interaction prompt, and a virtual OnInteract() method.
    /// </summary>
    public class InteractableObject : MonoBehaviour
    {
        private const float DEFAULT_INTERACTION_RANGE = 3f;

        [Header("Interaction Settings")]
        [SerializeField] private string interactionPrompt = "Press E to interact";
        [SerializeField] private float interactionRange = DEFAULT_INTERACTION_RANGE;

        private Transform _playerTransform;

        /// <summary>The prompt text shown to the player when in range.</summary>
        public string InteractionPrompt => interactionPrompt;

        /// <summary>The maximum interaction distance.</summary>
        public float InteractionRange => interactionRange;

        /// <summary>Whether the player is currently within interaction range.</summary>
        public bool IsPlayerInRange
        {
            get
            {
                if (_playerTransform == null) return false;
                return (_playerTransform.position - transform.position).sqrMagnitude
                       <= interactionRange * interactionRange;
            }
        }

        protected virtual void Start()
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                _playerTransform = playerGO.transform;
            }
            else
            {
                Debug.LogWarning($"[InteractableObject] No GameObject with tag 'Player' found for '{gameObject.name}'.");
            }
        }

        /// <summary>
        /// Called when the player presses E within range. Override in subclasses for specific behavior.
        /// </summary>
        public virtual void OnInteract()
        {
            Debug.Log($"[InteractableObject] Player interacted with '{gameObject.name}'.");
        }
    }
}
