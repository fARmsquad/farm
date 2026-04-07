using System.Collections.Generic;

namespace FarmSimVR.Core.Hunting
{
    public class CaughtAnimalTracker
    {
        private readonly List<CaughtAnimalRecord> _carried = new();
        private readonly List<CaughtAnimalRecord> _deposited = new();

        public int CarriedCount => _carried.Count;
        public int DepositedCount => _deposited.Count;

        public void Catch(CaughtAnimalRecord record)
        {
            _carried.Add(record);
        }

        public IReadOnlyList<CaughtAnimalRecord> GetCarried() => _carried;
        public IReadOnlyList<CaughtAnimalRecord> GetDeposited() => _deposited;

        public int DepositAll()
        {
            int count = _carried.Count;
            _deposited.AddRange(_carried);
            _carried.Clear();
            return count;
        }
    }
}
