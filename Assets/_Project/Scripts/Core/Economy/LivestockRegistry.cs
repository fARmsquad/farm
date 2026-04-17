using System;
using System.Collections.Generic;
using FarmSimVR.Core.Hunting;

namespace FarmSimVR.Core.Economy
{
    /// <summary>
    /// Tracks all animals the player owns. Survives scene transitions via EconomyManager.
    /// Both purchased and (future) hunted animals register here for cross-scene spawning.
    /// </summary>
    public class LivestockRegistry
    {
        private readonly List<AnimalType> _animals = new();

        public event Action OnChanged;

        public IReadOnlyList<AnimalType> Animals => _animals;

        public int Total => _animals.Count;

        public void Add(AnimalType type)
        {
            _animals.Add(type);
            OnChanged?.Invoke();
        }

        public int Count(AnimalType type)
        {
            int n = 0;
            foreach (var a in _animals)
                if (a == type) n++;
            return n;
        }
    }
}
