using System;
using System.IO;

namespace FarmSimVR.MonoBehaviours
{
    internal static class OpenAIConfigurationResolver
    {
        private const string OpenAiApiKeyEnvironmentVariable = "OPENAI_API_KEY";
        private const string GauntletDirectoryPath = "Development/gauntlet";

        private static readonly string[] PreferredGauntletEnvRelativePaths =
        {
            "collab-board/.env.vercel.production",
            "collab-board/.env",
            "chatbox-fork-pr-target-guard/.env.local",
            "chatbox-fork-pr-target-guard/.env",
            "chatbox/.env.local",
            "chatbox/.env"
        };

        internal static string ResolveApiKey(
            string serializedApiKey,
            ref bool gauntletApiKeyCached,
            ref string cachedGauntletApiKey,
            string gauntletEnvSearchRootOverride)
        {
            string envValue = Environment.GetEnvironmentVariable(OpenAiApiKeyEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(envValue))
                return envValue.Trim();

            string gauntletValue = ResolveGauntletApiKey(
                ref gauntletApiKeyCached,
                ref cachedGauntletApiKey,
                gauntletEnvSearchRootOverride);
            if (!string.IsNullOrWhiteSpace(gauntletValue))
                return gauntletValue;

            return string.IsNullOrWhiteSpace(serializedApiKey) ? null : serializedApiKey.Trim();
        }

        private static string ResolveGauntletApiKey(
            ref bool gauntletApiKeyCached,
            ref string cachedGauntletApiKey,
            string gauntletEnvSearchRootOverride)
        {
            if (gauntletApiKeyCached)
                return cachedGauntletApiKey;

            gauntletApiKeyCached = true;
            string searchRoot = ResolveGauntletSearchRoot(gauntletEnvSearchRootOverride);
            if (string.IsNullOrWhiteSpace(searchRoot) || !Directory.Exists(searchRoot))
                return null;

            if (TryReadPreferredEnvFiles(searchRoot, out cachedGauntletApiKey))
                return cachedGauntletApiKey;

            return TryReadAnyEnvFile(searchRoot, out cachedGauntletApiKey) ? cachedGauntletApiKey : null;
        }

        private static string ResolveGauntletSearchRoot(string gauntletEnvSearchRootOverride)
        {
            if (!string.IsNullOrWhiteSpace(gauntletEnvSearchRootOverride))
                return gauntletEnvSearchRootOverride;

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return string.IsNullOrWhiteSpace(home) ? null : Path.Combine(home, GauntletDirectoryPath);
        }

        private static bool TryReadPreferredEnvFiles(string searchRoot, out string apiKey)
        {
            for (int i = 0; i < PreferredGauntletEnvRelativePaths.Length; i++)
            {
                string preferredPath = Path.Combine(searchRoot, PreferredGauntletEnvRelativePaths[i]);
                if (File.Exists(preferredPath) && TryReadApiKeyFromEnvFile(preferredPath, out apiKey))
                    return true;
            }

            apiKey = null;
            return false;
        }

        private static bool TryReadAnyEnvFile(string searchRoot, out string apiKey)
        {
            try
            {
                foreach (string envFile in Directory.EnumerateFiles(searchRoot, ".env*", SearchOption.AllDirectories))
                {
                    if (ShouldSkipEnvFile(envFile))
                        continue;

                    if (TryReadApiKeyFromEnvFile(envFile, out apiKey))
                        return true;
                }
            }
            catch (Exception)
            {
                apiKey = null;
                return false;
            }

            apiKey = null;
            return false;
        }

        private static bool TryReadApiKeyFromEnvFile(string path, out string apiKeyValue)
        {
            apiKeyValue = null;
            foreach (string line in File.ReadLines(path))
            {
                if (!line.StartsWith("OPENAI_API_KEY=", StringComparison.Ordinal))
                    continue;

                string rawValue = line.Substring("OPENAI_API_KEY=".Length).Trim();
                apiKeyValue = TrimOptionalQuotes(rawValue);
                return !string.IsNullOrWhiteSpace(apiKeyValue);
            }

            return false;
        }

        private static bool ShouldSkipEnvFile(string path)
        {
            string fileName = Path.GetFileName(path);
            return fileName.EndsWith(".example", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".sample", StringComparison.OrdinalIgnoreCase);
        }

        private static string TrimOptionalQuotes(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (value.Length >= 2)
            {
                bool quotedWithDouble = value[0] == '"' && value[^1] == '"';
                bool quotedWithSingle = value[0] == '\'' && value[^1] == '\'';
                if (quotedWithDouble || quotedWithSingle)
                    value = value.Substring(1, value.Length - 2);
            }

            return value.Trim();
        }
    }
}
