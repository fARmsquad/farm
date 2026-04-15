using System;
using System.Collections.Generic;

namespace FarmSimVR.Core
{
    /// <summary>
    /// Builds the candidate local backend endpoints for Town voice token minting.
    /// </summary>
    public static class TownVoiceTokenServiceEndpointResolver
    {
        public const string EnvironmentVariableName = "FARMSIM_STORY_ORCHESTRATOR_URL";
        public const string ProductionBaseUrl = "https://story-orchestrator-production.up.railway.app";

        private static readonly string[] DefaultBaseUrls =
        {
            ProductionBaseUrl,
            "http://127.0.0.1:8012",
            "http://127.0.0.1:8000",
            "http://127.0.0.1:8011"
        };

        public static IReadOnlyList<string> BuildCandidateBaseUrls(
            string configuredBaseUrl,
            string environmentOverride)
        {
            var candidates = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddCandidate(environmentOverride, candidates, seen);

            if (!IsLoopbackAddress(configuredBaseUrl))
                AddCandidate(configuredBaseUrl, candidates, seen);

            for (int i = 0; i < DefaultBaseUrls.Length; i++)
                AddCandidate(DefaultBaseUrls[i], candidates, seen);

            if (IsLoopbackAddress(configuredBaseUrl))
                AddCandidate(configuredBaseUrl, candidates, seen);

            return candidates;
        }

        private static void AddCandidate(
            string rawValue,
            List<string> candidates,
            HashSet<string> seen)
        {
            string normalized = Normalize(rawValue);
            if (string.IsNullOrWhiteSpace(normalized))
                return;

            if (!Uri.TryCreate(normalized, UriKind.Absolute, out Uri uri))
                return;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return;

            if (seen.Add(normalized))
                candidates.Add(normalized);
        }

        private static string Normalize(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return null;

            return rawValue.Trim().TrimEnd('/');
        }

        private static bool IsLoopbackAddress(string rawValue)
        {
            string normalized = Normalize(rawValue);
            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            if (!Uri.TryCreate(normalized, UriKind.Absolute, out Uri uri))
                return false;

            return string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
        }
    }
}
