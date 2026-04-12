using System;
using System.Collections.Generic;

namespace FarmSimVR.MonoBehaviours.Farming
{
    public readonly struct FarmPlotFocusCandidate<T> where T : class
    {
        public FarmPlotFocusCandidate(T value, float distance, bool hasVisiblePrompt)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Distance = distance;
            HasVisiblePrompt = hasVisiblePrompt;
        }

        public T Value { get; }
        public float Distance { get; }
        public bool HasVisiblePrompt { get; }
    }

    public static class FarmPlotFocusSelector
    {
        public static T ChooseBest<T>(IReadOnlyList<FarmPlotFocusCandidate<T>> candidates) where T : class
        {
            if (candidates == null || candidates.Count == 0)
                return null;

            T best = null;
            var bestDistance = float.MaxValue;
            var bestHasPrompt = false;

            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate.Value == null)
                    continue;

                if (best == null ||
                    (candidate.HasVisiblePrompt && !bestHasPrompt) ||
                    (candidate.HasVisiblePrompt == bestHasPrompt && candidate.Distance < bestDistance))
                {
                    best = candidate.Value;
                    bestDistance = candidate.Distance;
                    bestHasPrompt = candidate.HasVisiblePrompt;
                }
            }

            return best;
        }
    }
}
