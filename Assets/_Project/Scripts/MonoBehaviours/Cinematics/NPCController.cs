using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Controls an NPC capsule: faces the player, shows an interaction prompt,
    /// and starts dialogue when the player presses E within range.
    /// </summary>
    public class NPCController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Identity")]
        [SerializeField] private string npcName;
        [SerializeField] private Color capsuleColor = Color.cyan;

        [Header("Dialogue")]
        [SerializeField] private DialogueData dialogueData;

        [Header("Interaction")]
        [SerializeField] private float interactionRange = 5f;
        [SerializeField] private float turnSpeed = 5f;

        #endregion

        #region Private State

        private Transform playerTransform;
        private MeshRenderer capsuleRenderer;
        private TextMesh nameTag;
        private GameObject promptCanvas;

        #endregion

        #region Public Properties

        public string NpcName => npcName;

        public bool IsPlayerInRange
        {
            get
            {
                if (playerTransform == null) return false;
                return (playerTransform.position - transform.position).sqrMagnitude
                       <= interactionRange * interactionRange;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            capsuleRenderer = GetComponentInChildren<MeshRenderer>();
            nameTag = GetComponentInChildren<TextMesh>();
            promptCanvas = transform.Find("PromptCanvas")?.gameObject;
        }

        private void Start()
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
                playerTransform = playerGO.transform;

            if (capsuleRenderer != null)
                capsuleRenderer.material.color = capsuleColor;

            if (nameTag != null)
                nameTag.text = npcName;

            if (promptCanvas != null)
                promptCanvas.SetActive(false);
        }

        private void Update()
        {
            if (playerTransform == null) return;

            bool inRange = IsPlayerInRange;

            FacePlayer(inRange);
            UpdatePrompt(inRange);
            HandleInteraction(inRange);
        }

        #endregion

        #region Face Player

        private void FacePlayer(bool inRange)
        {
            if (!inRange) return;

            Vector3 direction = playerTransform.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }

        #endregion

        #region Interaction Prompt

        private void UpdatePrompt(bool inRange)
        {
            if (promptCanvas == null) return;

            bool dialogueAvailable = DialogueManager.Instance == null
                                     || !DialogueManager.Instance.IsPlaying;

            promptCanvas.SetActive(inRange && dialogueAvailable);
        }

        #endregion

        #region Interaction

        private void HandleInteraction(bool inRange)
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (!kb.eKey.wasPressedThisFrame) return;
            if (!inRange) return;

            if (DialogueManager.Instance != null && DialogueManager.Instance.IsPlaying)
                return;

            if (dialogueData == null)
            {
                Debug.LogWarning($"[NPCController] '{npcName}' has no DialogueData assigned.");
                return;
            }

            DialogueManager.Instance.StartDialogue(dialogueData);
        }

        #endregion

        #region Public Methods

        public void Activate()
        {
            gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            gameObject.SetActive(false);
        }

        #endregion
    }
}
