using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using FarmSimVR.Core.Inventory;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.MonoBehaviours.UI
{
    /// <summary>
    /// UGUI controller for the full-screen backpack inventory grid.
    /// Toggle with Tab or I key. Shows 24 inventory slots with item icons and counts.
    /// </summary>
    public sealed class InventoryUIController : MonoBehaviour
    {
        private const int TotalSlotCount = 24;
        private const int GridColumns = 6;

        [Header("Panel")]
        [SerializeField] private GameObject backpackPanel;

        [Header("Slot Grid")]
        [SerializeField] private Transform slotContainer;
        [SerializeField] private GameObject slotPrefab;

        private IInventorySystem _inventory;
        private IItemDatabase _database;
        private ItemIconDatabase _iconDb;
        private ItemTooltipController _tooltip;
        private ToolEquipState _toolEquip;
        private HotbarUIController _hotbarUI;
        private readonly List<SlotView> _slotViews = new();
        private bool _isOpen;

        /// <summary>True when the backpack panel is visible and consuming input.</summary>
        public bool IsOpen => _isOpen;

        // Placeholder icon colors per category (fallback when no icon database assigned)
        private static readonly Dictionary<ItemCategory, Color> CategoryColors = new()
        {
            { ItemCategory.Seed, new Color(0.2f, 0.7f, 0.3f) },
            { ItemCategory.Crop, new Color(0.9f, 0.6f, 0.1f) },
            { ItemCategory.Tool, new Color(0.4f, 0.5f, 0.8f) },
            { ItemCategory.Material, new Color(0.6f, 0.6f, 0.6f) },
        };

        private static readonly Color EquippedBorderColor = new(1f, 0.85f, 0.2f, 1f);
        private static readonly Color DefaultBorderColor = new(0.2f, 0.2f, 0.2f, 1f);
        private static readonly Color EmptySlotColor = new(0.15f, 0.15f, 0.15f, 0.8f);

        /// <summary>
        /// Initializes the UI controller. Called from FarmSimDriver.Start().
        /// </summary>
        public void Initialize(IInventorySystem inventory, IItemDatabase database, ToolEquipState toolEquip,
            ItemIconDatabase iconDb = null)
        {
            if (inventory == null)
            {
                Debug.LogWarning("[InventoryUIController] Inventory is null; disabling.");
                enabled = false;
                return;
            }

            _inventory = inventory;
            _database = database;
            _toolEquip = toolEquip;
            _iconDb = iconDb;
            _tooltip = FindAnyObjectByType<ItemTooltipController>();

            BuildSlotViews();

            if (backpackPanel != null)
                backpackPanel.SetActive(false);

            _isOpen = false;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.tabKey.wasPressedThisFrame || keyboard.iKey.wasPressedThisFrame)
                TogglePanel();
        }

        /// <summary>
        /// Rebuilds all slot visuals from the current inventory state.
        /// </summary>
        public void RefreshAll()
        {
            if (_inventory == null)
                return;

            var slots = _inventory.Slots;
            for (int i = 0; i < _slotViews.Count; i++)
            {
                var view = _slotViews[i];
                if (i < slots.Count && !slots[i].IsEmpty)
                {
                    var slot = slots[i];
                    var toolId = FarmToolMap.FromItemId(slot.ItemId);
                    bool isEquipped = _toolEquip != null
                        && toolId != FarmToolId.None
                        && _toolEquip.EquippedHotbarSlot == i;

                    var iconColor = GetIconColor(slot.ItemId);
                    string displayName = ResolveDisplayName(slot.ItemId);
                    Sprite iconSprite = _iconDb != null ? _iconDb.GetIcon(slot.ItemId) : null;
                    view.SetItem(slot.ItemId, displayName, slot.Quantity, iconColor, iconSprite, isEquipped);
                }
                else
                {
                    view.SetEmpty();
                }
            }
        }

        private void TogglePanel()
        {
            _isOpen = !_isOpen;

            if (backpackPanel != null)
                backpackPanel.SetActive(_isOpen);

            if (_isOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                RefreshAll();
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void BuildSlotViews()
        {
            _slotViews.Clear();

            if (slotContainer == null || slotPrefab == null)
            {
                Debug.LogWarning("[InventoryUIController] slotContainer or slotPrefab not assigned.");
                return;
            }

            for (int i = 0; i < TotalSlotCount; i++)
            {
                var go = Instantiate(slotPrefab, slotContainer);
                go.name = $"Slot_{i}";
                var view = new SlotView(go, i, OnSlotClicked, OnSlotHoverEnter, OnSlotHoverExit);
                _slotViews.Add(view);
            }
        }

        private void OnSlotHoverEnter(int slotIndex, Vector2 screenPos)
        {
            if (_tooltip == null || _inventory == null || _database == null)
                return;

            var slots = _inventory.Slots;
            if (slotIndex >= slots.Count || slots[slotIndex].IsEmpty)
                return;

            var slot = slots[slotIndex];
            if (!_database.Exists(slot.ItemId))
                return;

            var data = _database.GetItem(slot.ItemId);
            Sprite icon = _iconDb != null ? _iconDb.GetIcon(slot.ItemId) : null;
            _tooltip.Show(data, icon, screenPos);
        }

        private void OnSlotHoverExit(int slotIndex)
        {
            if (_tooltip != null)
                _tooltip.Hide();
        }

        private void OnSlotClicked(int slotIndex)
        {
            if (_toolEquip == null || _inventory == null)
                return;

            const int hotbarSlotCount = 5;

            if (slotIndex < hotbarSlotCount)
            {
                // Clicking a hotbar-range slot: equip it directly
                _toolEquip.EquipSlot(slotIndex, _inventory);
            }
            else
            {
                // Clicking a backpack slot: swap it into the active hotbar slot and equip
                int targetHotbar = _toolEquip.EquippedHotbarSlot >= 0
                    ? _toolEquip.EquippedHotbarSlot
                    : 0;

                _inventory.SwapSlots(slotIndex, targetHotbar);
                _toolEquip.EquipSlot(targetHotbar, _inventory);

                // Sync the hotbar visuals
                if (_hotbarUI == null)
                    _hotbarUI = FindAnyObjectByType<HotbarUIController>();
                _hotbarUI?.Refresh();
            }

            RefreshAll();
        }

        private string ResolveDisplayName(string itemId)
        {
            if (_database == null || string.IsNullOrEmpty(itemId))
                return itemId;

            if (!_database.Exists(itemId))
                return itemId;

            return _database.GetItem(itemId).DisplayName;
        }

        private Color GetIconColor(string itemId)
        {
            if (_database != null && _database.Exists(itemId))
            {
                var data = _database.GetItem(itemId);
                if (CategoryColors.TryGetValue(data.Category, out var color))
                    return color;
            }

            // Fallback: prefix-based
            if (itemId.StartsWith("seed_")) return CategoryColors[ItemCategory.Seed];
            if (itemId.StartsWith("crop_")) return CategoryColors[ItemCategory.Crop];
            if (itemId.StartsWith("tool_")) return CategoryColors[ItemCategory.Tool];
            return CategoryColors[ItemCategory.Material];
        }

        /// <summary>
        /// Internal view wrapper for a single slot UI element.
        /// </summary>
        private sealed class SlotView
        {
            private readonly Image _background;
            private readonly Image _icon;
            private readonly TMP_Text _nameText;
            private readonly TMP_Text _countText;
            private readonly int _slotIndex;
            private string _currentItemId;

            public SlotView(GameObject root, int slotIndex, System.Action<int> onClick,
                System.Action<int, Vector2> onHoverEnter, System.Action<int> onHoverExit)
            {
                _slotIndex = slotIndex;

                // Hierarchy: root (Image bg + Button) > Icon (Image) > Name (TMP_Text) > Count (TMP_Text)
                _background = root.GetComponent<Image>();

                var iconTransform = root.transform.Find("Icon");
                _icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;

                var nameTransform = root.transform.Find("Name");
                _nameText = nameTransform != null ? nameTransform.GetComponent<TMP_Text>() : null;

                var countTransform = root.transform.Find("Count");
                _countText = countTransform != null ? countTransform.GetComponent<TMP_Text>() : null;

                var button = root.GetComponent<Button>();
                if (button != null)
                    button.onClick.AddListener(() => onClick?.Invoke(_slotIndex));

                // Add hover events via EventTrigger
                var trigger = root.GetComponent<EventTrigger>();
                if (trigger == null)
                    trigger = root.AddComponent<EventTrigger>();

                var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enterEntry.callback.AddListener(eventData =>
                {
                    var pointerData = eventData as PointerEventData;
                    var pos = pointerData != null ? pointerData.position : (Vector2)Input.mousePosition;
                    onHoverEnter?.Invoke(_slotIndex, pos);
                });
                trigger.triggers.Add(enterEntry);

                var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exitEntry.callback.AddListener(_ => onHoverExit?.Invoke(_slotIndex));
                trigger.triggers.Add(exitEntry);
            }

            /// <summary>
            /// Shows an item in this slot with its display name and optional icon sprite.
            /// </summary>
            public void SetItem(string itemId, string displayName, int quantity, Color iconColor,
                Sprite iconSprite, bool isEquipped)
            {
                _currentItemId = itemId;

                if (_icon != null)
                {
                    if (iconSprite != null)
                    {
                        _icon.sprite = iconSprite;
                        _icon.color = Color.white;
                        _icon.preserveAspect = true;
                    }
                    else
                    {
                        _icon.sprite = null;
                        _icon.color = iconColor;
                    }

                    _icon.enabled = true;
                }

                if (_nameText != null)
                {
                    _nameText.text = displayName ?? string.Empty;
                    _nameText.enabled = true;
                }

                if (_countText != null)
                {
                    _countText.text = quantity > 1 ? quantity.ToString() : string.Empty;
                    _countText.enabled = true;
                }

                if (_background != null)
                    _background.color = isEquipped ? EquippedBorderColor : DefaultBorderColor;
            }

            /// <summary>
            /// Clears this slot to empty state.
            /// </summary>
            public void SetEmpty()
            {
                _currentItemId = null;

                if (_icon != null)
                    _icon.enabled = false;

                if (_nameText != null)
                {
                    _nameText.text = string.Empty;
                    _nameText.enabled = false;
                }

                if (_countText != null)
                {
                    _countText.text = string.Empty;
                    _countText.enabled = false;
                }

                if (_background != null)
                    _background.color = EmptySlotColor;
            }
        }
    }
}
