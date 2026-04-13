using FarmSimVR.Core.Story;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    public sealed class StoryPackageSceneBinding : MonoBehaviour
    {
        [SerializeField] private TextAsset packageAsset;
        [SerializeField] private string sceneNameOverride;
        [SerializeField] private bool loadOnAwake = true;

        public TextAsset PackageAsset
        {
            get => packageAsset;
            set => packageAsset = value;
        }

        public string SceneNameOverride
        {
            get => sceneNameOverride;
            set => sceneNameOverride = value ?? string.Empty;
        }

        public StoryPackageSnapshot CurrentPackage { get; private set; }
        public StoryBeatSnapshot CurrentBeat { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public string NextSceneName => CurrentBeat?.NextSceneName ?? string.Empty;

        private void Awake()
        {
            if (loadOnAwake)
                LoadNow();
        }

        public bool LoadNow()
        {
            if (!StoryPackageImporter.TryImport(packageAsset, out var package, out var error))
                return Fail(error);

            var sceneName = ResolveSceneName();
            if (!StoryPackageNavigator.TryGetBeatBySceneName(package, sceneName, out var beat))
                return Fail($"No story beat found for scene '{sceneName}'.");

            CurrentPackage = package;
            CurrentBeat = beat;
            LastError = string.Empty;
            return true;
        }

        public bool TryBuildCurrentSequence(out CinematicSequence sequence, out string error)
        {
            sequence = null;
            if (CurrentBeat == null)
            {
                error = "No current beat is loaded.";
                return false;
            }

            return StoryPackageSequenceBuilder.TryBuildCutsceneSequence(CurrentBeat, out sequence, out error);
        }

        private bool Fail(string error)
        {
            CurrentPackage = null;
            CurrentBeat = null;
            LastError = error ?? "Story package scene binding failed.";
            return false;
        }

        private string ResolveSceneName()
        {
            return string.IsNullOrWhiteSpace(sceneNameOverride)
                ? SceneManager.GetActiveScene().name
                : sceneNameOverride;
        }
    }
}
