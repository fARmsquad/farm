using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmSimVR.ScriptableObjects
{
    /// <summary>
    /// ScriptableObject mapping itemId strings to Sprite icons.
    /// Kept separate from the pure-C# ItemData so the data layer stays serialization-free.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemIcons", menuName = "FarmSimVR/Item Icon Database")]
    public sealed class ItemIconDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemIconEntry> entries = new();
        [SerializeField] private Sprite fallbackIcon;

        private Dictionary<string, Sprite> _lookup;

        /// <summary>
        /// Serializable entry pairing an itemId to a sprite icon.
        /// </summary>
        [Serializable]
        public struct ItemIconEntry
        {
            public string itemId;
            public Sprite icon;
        }

        private void OnEnable()
        {
            RebuildLookup();
        }

        /// <summary>
        /// Returns the icon sprite for the given itemId, or the fallback sprite if not found.
        /// </summary>
        public Sprite GetIcon(string itemId)
        {
            EnsureLookup();

            if (!string.IsNullOrEmpty(itemId) && _lookup.TryGetValue(itemId, out var sprite))
                return sprite;

            return fallbackIcon;
        }

        /// <summary>
        /// Returns true if an icon mapping exists for the given itemId.
        /// </summary>
        public bool HasIcon(string itemId)
        {
            EnsureLookup();
            return !string.IsNullOrEmpty(itemId) && _lookup.ContainsKey(itemId);
        }

        private void EnsureLookup()
        {
            if (_lookup == null)
                RebuildLookup();
        }

        private void RebuildLookup()
        {
            _lookup = new Dictionary<string, Sprite>(entries.Count, StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.itemId))
                    continue;

                _lookup[entry.itemId] = entry.icon;
            }
        }
    }
}
