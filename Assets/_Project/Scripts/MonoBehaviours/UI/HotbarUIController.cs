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
    /// UGUI controller for the always-visible 5-slot hotbar at the bottom of the screen.
    /// Number keys 1-5 equip the corresponding slot.
    /// </summary>
    public sealed class HotbarUIController : MonoBehaviour
    {
        private const int HotbarSlotCount = 5;

        [Header("Hotbar")]
        [SerializeField] private Transform hotbarContainer;
        [SerializeField] private GameObject hotbarSlotPrefab;

        private IInventorySystem _inventory;
        private IItemDatabase _database;
        private ItemIconDatabase _iconDb;
        private ItemTooltipController _tooltip;
        private ToolEquipState _toolEquip;
        private readonly List<HotbarSlotView> _slotViews = new();

        private static readonly Color EquippedColor = new(1f, 0.85f, 0.2f, 1f);
        private static readonly Color DefaultColor = new(0.25f, 0.25f, 0.25f, 0.9f);
        private static readonly Color EmptySlotColor = new(0.15f, 0.15f, 0.15f, 0.7f);

        // Placeholder icon colors per category (fallback when no icon database assigned)
        private static readonly Dictionary<ItemCategory, Color> CategoryColors = new()
        {
            { ItemCategory.Seed, new Color(0.2f, 0.7f, 0.3f) },
            { ItemCategory.Crop, new Color(0.9f, 0.6f, 0.1f) },
            { ItemCategory.Tool, new Color(0.4f, 0.5f, 0.8f) },
            { ItemCategory.Material, new Color(0.6f, 0.6f, 0.6f) },
        };

        /// <summary>
        /// Initializes the hotbar. Called from FarmSimDriver.Start().
        /// </summary>
        public void Initialize(IInventorySystem inventory, IItemDatabase database, ToolEquipState toolEquip,
            ItemIconDatabase iconDb = null)
        {
            if (inventory == null)
            {
                Debug.LogWarning("[HotbarUIController] Inventory is null; disabling.");
                enabled = false;
                return;
            }

            _inventory = inventory;
            _database = database;
            _toolEquip = toolEquip;
            _iconDb = iconDb;
            _tooltip = FindAnyObjectByType<ItemTooltipController>();

            BuildSlotViews();
            Refresh();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            bool changed = false;
            if (keyboard.digit1Key.wasPressedThisFrame) { _toolEquip?.EquipSlot(0, _inventory); changed = true; }
            else if (keyboard.digit2Key.wasPressedThisFrame) { _toolEquip?.EquipSlot(1, _inventory); changed = true; }
            else if (keyboard.digit3Key.wasPressedThisFrame) { _toolEquip?.EquipSlot(2, _inventory); changed = true; }
            else if (keyboard.digit4Key.wasPressedThisFrame) { _toolEquip?.EquipSlot(3, _inventory); changed = true; }
            else if (keyboard.digit5Key.wasPressedThisFrame) { _toolEquip?.EquipSlot(4, _inventory); changed = true; }

            if (changed)
                Refresh();
        }

        /// <summary>
        /// Updates visuals for all 5 hotbar slots from inventory slots 0-4.
        /// </summary>
        public void Refresh()
        {
            if (_inventory == null)
                return;

            var slots = _inventory.Slots;
            for (int i = 0; i < _slotViews.Count && i < HotbarSlotCount; i++)
            {
                var view = _slotViews[i];
                bool isEquipped = _toolEquip != null && _toolEquip.EquippedHotbarSlot == i;

                if (i < slots.Count && !slots[i].IsEmpty)
                {
                    var slot = slots[i];
                    var iconColor = GetIconColor(slot.ItemId);
                    string displayName = ResolveDisplayName(slot.ItemId);
                    Sprite iconSprite = _iconDb != null ? _iconDb.GetIcon(slot.ItemId) : null;
                    view.SetItem(slot.ItemId, displayName, slot.Quantity, iconColor, iconSprite, isEquipped);
                }
                else
                {
                    view.SetEmpty(isEquipped);
                }
            }
        }

        private void BuildSlotViews()
        {
            _slotViews.Clear();

            if (hotbarContainer == null || hotbarSlotPrefab == null)
            {
                Debug.LogWarning("[HotbarUIController] hotbarContainer or hotbarSlotPrefab not assigned.");
                return;
            }

            for (int i = 0; i < HotbarSlotCount; i++)
            {
                var go = Instantiate(hotbarSlotPrefab, hotbarContainer);
                go.name = $"HotbarSlot_{i}";
                var view = new HotbarSlotView(go, i, OnSlotHoverEnter, OnSlotHoverExit);
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

        private string ResolveDisplayName(string itemId)
        {
            if (_database == null || string.IsNullOrEmpty(itemId))
                return itemId;

            if (!_database.Exists(itemId))
                return itemId;

            return _database.GetItem(itemId).DisplayName;
        }

        private static Color GetIconColor(string itemId)
        {
            if (itemId.StartsWith("seed_")) return CategoryColors[ItemCategory.Seed];
            if (itemId.StartsWith("crop_")) return CategoryColors[ItemCategory.Crop];
            if (itemId.StartsWith("tool_")) return CategoryColors[ItemCategory.Tool];
            return CategoryColors[ItemCategory.Material];
        }

        /// <summary>
        /// Internal view wrapper for a single hotbar slot.
        /// </summary>
        private sealed class HotbarSlotView
        {
            private readonly Image _background;
            private readonly Image _icon;
            private readonly TMP_Text _keyLabel;
            private readonly TMP_Text _nameText;
            private readonly TMP_Text _countText;
            private string _currentItemId;

            public HotbarSlotView(GameObject root, int slotIndex,
                System.Action<int, Vector2> onHoverEnter, System.Action<int> onHoverExit)
            {
                // Hierarchy: root (Image bg) > Icon (Image) > KeyLabel (TMP) > Name (TMP) > Count (TMP)
                _background = root.GetComponent<Image>();

                var iconTransform = root.transform.Find("Icon");
                _icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;

                var keyLabelTransform = root.transform.Find("KeyLabel");
                _keyLabel = keyLabelTransform != null ? keyLabelTransform.GetComponent<TMP_Text>() : null;

                var nameTransform = root.transform.Find("Name");
                _nameText = nameTransform != null ? nameTransform.GetComponent<TMP_Text>() : null;

                var countTransform = root.transform.Find("Count");
                _countText = countTransform != null ? countTransform.GetComponent<TMP_Text>() : null;

                if (_keyLabel != null)
                    _keyLabel.text = (slotIndex + 1).ToString();

                // Add hover events via EventTrigger
                var trigger = root.GetComponent<EventTrigger>();
                if (trigger == null)
                    trigger = root.AddComponent<EventTrigger>();

                var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enterEntry.callback.AddListener(eventData =>
                {
                    var pointerData = eventData as PointerEventData;
                    var pos = pointerData != null ? pointerData.position : (Vector2)Input.mousePosition;
                    onHoverEnter?.Invoke(slotIndex, pos);
                });
                trigger.triggers.Add(enterEntry);

                var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exitEntry.callback.AddListener(_ => onHoverExit?.Invoke(slotIndex));
                trigger.triggers.Add(exitEntry);
            }

            /// <summary>
            /// Shows an item in this hotbar slot with its display name and optional icon sprite.
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
                    _background.color = isEquipped ? EquippedColor : DefaultColor;
            }

            /// <summary>
            /// Clears this hotbar slot to empty state.
            /// </summary>
            public void SetEmpty(bool isEquipped)
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
                    _background.color = isEquipped ? EquippedColor : EmptySlotColor;
            }
        }
    }
}
