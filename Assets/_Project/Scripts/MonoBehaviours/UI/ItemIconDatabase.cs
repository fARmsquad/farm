using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.UI
{
    /// <summary>
    /// ScriptableObject mapping itemId strings to Sprite icons.
    /// When no sprite is assigned for an item, auto-generates a colored placeholder
    /// with the item's initial letter so every slot is visually identifiable.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemIcons", menuName = "FarmSimVR/Item Icon Database")]
    public sealed class ItemIconDatabase : ScriptableObject
    {
        private const int PlaceholderSize = 64;
        private const int LetterInset = 8;

        [SerializeField] private List<ItemIconEntry> entries = new();
        [SerializeField] private Sprite fallbackIcon;

        private Dictionary<string, Sprite> _lookup;
        private readonly Dictionary<string, Sprite> _generatedCache = new();

        /// <summary>
        /// Serializable entry pairing an itemId to a sprite icon.
        /// </summary>
        [Serializable]
        public struct ItemIconEntry
        {
            public string itemId;
            public Sprite icon;
        }

        /// <summary>
        /// Category-to-color mapping used for auto-generated placeholder icons.
        /// </summary>
        private static readonly Dictionary<string, Color> PrefixColors = new(StringComparer.Ordinal)
        {
            { "seed_",  new Color(0.18f, 0.62f, 0.28f) },
            { "crop_",  new Color(0.85f, 0.55f, 0.10f) },
            { "tool_",  new Color(0.35f, 0.45f, 0.75f) },
        };

        private static readonly Color DefaultPlaceholderColor = new(0.5f, 0.5f, 0.5f);

        private void OnEnable()
        {
            RebuildLookup();
        }

        /// <summary>
        /// Returns the icon sprite for the given itemId. If no sprite is mapped,
        /// auto-generates a colored placeholder with the item's initial letter.
        /// </summary>
        public Sprite GetIcon(string itemId)
        {
            EnsureLookup();

            if (!string.IsNullOrEmpty(itemId) && _lookup.TryGetValue(itemId, out var sprite) && sprite != null)
                return sprite;

            if (fallbackIcon != null)
                return fallbackIcon;

            return GetOrCreatePlaceholder(itemId);
        }

        /// <summary>
        /// Returns true if a hand-authored icon mapping exists for the given itemId.
        /// </summary>
        public bool HasIcon(string itemId)
        {
            EnsureLookup();
            return !string.IsNullOrEmpty(itemId) && _lookup.TryGetValue(itemId, out var s) && s != null;
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

        private Sprite GetOrCreatePlaceholder(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;

            if (_generatedCache.TryGetValue(itemId, out var cached))
                return cached;

            var color = ResolveColor(itemId);
            char letter = ResolveDisplayLetter(itemId);
            var sprite = GeneratePlaceholderSprite(itemId, color, letter);
            _generatedCache[itemId] = sprite;
            return sprite;
        }

        private static Color ResolveColor(string itemId)
        {
            foreach (var kvp in PrefixColors)
            {
                if (itemId.StartsWith(kvp.Key, StringComparison.Ordinal))
                    return kvp.Value;
            }

            return DefaultPlaceholderColor;
        }

        private static char ResolveDisplayLetter(string itemId)
        {
            foreach (var prefix in PrefixColors.Keys)
            {
                if (itemId.StartsWith(prefix, StringComparison.Ordinal) && itemId.Length > prefix.Length)
                    return char.ToUpperInvariant(itemId[prefix.Length]);
            }

            return itemId.Length > 0 ? char.ToUpperInvariant(itemId[0]) : '?';
        }

        private static Sprite GeneratePlaceholderSprite(string itemId, Color bgColor, char letter)
        {
            var tex = new Texture2D(PlaceholderSize, PlaceholderSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var borderColor = bgColor * 0.7f;
            borderColor.a = 1f;
            var pixels = new Color[PlaceholderSize * PlaceholderSize];
            for (int y = 0; y < PlaceholderSize; y++)
            {
                for (int x = 0; x < PlaceholderSize; x++)
                {
                    bool isBorder = x < 2 || x >= PlaceholderSize - 2 || y < 2 || y >= PlaceholderSize - 2;
                    pixels[y * PlaceholderSize + x] = isBorder ? borderColor : bgColor;
                }
            }

            tex.SetPixels(pixels);
            DrawLetter(tex, letter, LetterInset, LetterInset, PlaceholderSize - LetterInset * 2, Color.white);
            tex.Apply();

            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, PlaceholderSize, PlaceholderSize),
                new Vector2(0.5f, 0.5f),
                PlaceholderSize);
            sprite.name = $"AutoIcon_{itemId}";

            return sprite;
        }

        /// <summary>
        /// Draws a letter glyph into the texture using a minimal 5x7 bitmap font.
        /// The letter is scaled to fill the given region.
        /// </summary>
        private static void DrawLetter(Texture2D tex, char letter, int startX, int startY, int size, Color color)
        {
            var glyph = GetGlyph(letter);
            if (glyph == null)
                return;

            const int glyphW = 5;
            const int glyphH = 7;
            float scaleX = size / (float)glyphW;
            float scaleY = size / (float)glyphH;

            for (int gy = 0; gy < glyphH; gy++)
            {
                for (int gx = 0; gx < glyphW; gx++)
                {
                    if (glyph[gy][gx] != '#')
                        continue;

                    int px0 = startX + Mathf.FloorToInt(gx * scaleX);
                    int py0 = startY + Mathf.FloorToInt((glyphH - 1 - gy) * scaleY);
                    int px1 = startX + Mathf.CeilToInt((gx + 1) * scaleX);
                    int py1 = startY + Mathf.CeilToInt((glyphH - gy) * scaleY);

                    for (int py = py0; py < py1 && py < tex.height; py++)
                    {
                        for (int px = px0; px < px1 && px < tex.width; px++)
                        {
                            tex.SetPixel(px, py, color);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a 5x7 bitmap glyph for uppercase letters.
        /// Each string is a row, top to bottom.
        /// </summary>
        private static string[] GetGlyph(char c)
        {
            return char.ToUpperInvariant(c) switch
            {
                'A' => new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
                'B' => new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
                'C' => new[] { ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###." },
                'D' => new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
                'E' => new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
                'F' => new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." },
                'G' => new[] { ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".###." },
                'H' => new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
                'I' => new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####" },
                'K' => new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" },
                'L' => new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
                'M' => new[] { "#...#", "##.##", "#.#.#", "#...#", "#...#", "#...#", "#...#" },
                'N' => new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" },
                'O' => new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
                'P' => new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
                'R' => new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
                'S' => new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
                'T' => new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
                'W' => new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" },
                _   => new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." },
            };
        }
    }
}
