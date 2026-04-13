using FarmSimVR.Core.Story;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    public static class StoryPackageImporter
    {
        public static bool TryImport(TextAsset textAsset, out StoryPackageSnapshot package, out string error)
        {
            if (textAsset == null)
            {
                package = null;
                error = "Story package asset is null.";
                return false;
            }

            return TryImportJson(textAsset.text, out package, out error);
        }

        public static bool TryImportJson(string json, out StoryPackageSnapshot package, out string error)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                package = null;
                error = "Story package JSON is empty.";
                return false;
            }

            package = JsonUtility.FromJson<StoryPackageSnapshot>(json);
            Normalize(package);
            if (package == null)
            {
                error = "Failed to parse story package JSON.";
                return false;
            }

            var validation = StoryPackageContract.Validate(package);
            if (!validation.IsValid)
            {
                error = validation.FirstError;
                package = null;
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static void Normalize(StoryPackageSnapshot package)
        {
            if (package == null)
                return;

            package.Beats ??= System.Array.Empty<StoryBeatSnapshot>();
            for (int i = 0; i < package.Beats.Length; i++)
            {
                var beat = package.Beats[i];
                if (beat == null)
                    continue;

                beat.SequenceSteps ??= System.Array.Empty<StorySequenceStepSnapshot>();
            }
        }
    }
}
