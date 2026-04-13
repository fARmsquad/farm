using UnityEngine;
using FarmSimVR.Core.Farming;
using FarmSimVR.Core.Inventory;

namespace FarmSimVR.MonoBehaviours.Farming
{
    /// <summary>
    /// Instantiates and swaps 3D tool prefabs parented to the player's hand
    /// whenever the equipped tool changes.
    /// </summary>
    public sealed class ToolVisualController : MonoBehaviour
    {
        [Header("Tool Prefabs (indexed by FarmToolId: 0=None, 1=Hoe, 2=WateringCan, 3=SeedPouch, 4=HarvestBasket)")]
        [SerializeField] private GameObject[] toolPrefabs = new GameObject[5];

        [Header("Hand Attachment")]
        [SerializeField] private Transform handAttachPoint;

        [Header("Per-tool Offsets")]
        [SerializeField] private Vector3[] positionOffsets = new Vector3[5];
        [SerializeField] private Vector3[] rotationOffsets = new Vector3[5];

        private const float HandAttachHeight = 0.8f;
        private const float HandAttachForward = 0.3f;
        private const float HandAttachRight = 0.4f;

        private ToolEquipState _toolEquip;
        private GameObject _currentToolInstance;

        /// <summary>
        /// Initializes the controller with the tool equip state. Call from FarmSimDriver.
        /// </summary>
        public void Initialize(ToolEquipState toolEquip)
        {
            if (toolEquip == null)
            {
                Debug.LogWarning("[ToolVisualController] ToolEquipState is null; disabling.");
                enabled = false;
                return;
            }

            _toolEquip = toolEquip;
            _toolEquip.OnToolChanged += OnToolChanged;

            // If no hand attach point assigned, find/create one on the Player
            if (handAttachPoint == null)
                handAttachPoint = FindOrCreateHandAttach();

            OnToolChanged(_toolEquip.EquippedTool);
        }

        private void OnDestroy()
        {
            if (_toolEquip != null)
                _toolEquip.OnToolChanged -= OnToolChanged;

            DestroyCurrentTool();
        }

        private void OnToolChanged(FarmToolId newTool)
        {
            DestroyCurrentTool();

            if (newTool == FarmToolId.None)
                return;

            int index = (int)newTool;
            if (index < 0 || index >= toolPrefabs.Length || toolPrefabs[index] == null)
                return;

            if (handAttachPoint == null)
            {
                Debug.LogWarning("[ToolVisualController] handAttachPoint is not available.");
                return;
            }

            _currentToolInstance = Instantiate(toolPrefabs[index], handAttachPoint);

            var offset = index < positionOffsets.Length ? positionOffsets[index] : Vector3.zero;
            var rotation = index < rotationOffsets.Length ? rotationOffsets[index] : Vector3.zero;

            _currentToolInstance.transform.localPosition = offset;
            _currentToolInstance.transform.localRotation = Quaternion.Euler(rotation);
        }

        private void DestroyCurrentTool()
        {
            if (_currentToolInstance != null)
            {
                Destroy(_currentToolInstance);
                _currentToolInstance = null;
            }
        }

        /// <summary>
        /// Finds the Player GameObject and creates a HandAttach child transform on it.
        /// </summary>
        private static Transform FindOrCreateHandAttach()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[ToolVisualController] No Player found; tool visuals will not appear.");
                return null;
            }

            var existing = player.transform.Find("HandAttach");
            if (existing != null)
                return existing;

            var attachGo = new GameObject("HandAttach");
            attachGo.transform.SetParent(player.transform, false);
            attachGo.transform.localPosition = new Vector3(HandAttachRight, HandAttachHeight, HandAttachForward);
            return attachGo.transform;
        }
    }
}
