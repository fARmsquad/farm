using FarmSimVR.Core.Tutorial;
using FarmSimVR.MonoBehaviours.Cinematics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    public sealed class TutorialFlowController : MonoBehaviour
    {
        public static TutorialFlowController Instance { get; private set; }

        public TutorialFlowService Flow { get; private set; }
        public ToolRecoveryService ToolRecovery { get; private set; }

        public bool ShowCompletionBanner { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Flow = new TutorialFlowService();
            ToolRecovery = new ToolRecoveryService();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool IsTutorialSceneActive()
        {
            return TutorialSceneCatalog.GetStepForScene(SceneManager.GetActiveScene().name) != TutorialStep.None;
        }

        public string ResolveSceneRequest(string requestedSceneName)
        {
            var currentSceneName = SceneManager.GetActiveScene().name;
            if (TutorialSceneCatalog.GetStepForScene(currentSceneName) == TutorialStep.None)
                return requestedSceneName;

            if (StoryPackageRuntimeCatalog.TryGetNextScene(currentSceneName, out var packageNextScene))
                return packageNextScene;

            var nextScene = Flow.GetNextScene();
            return string.IsNullOrWhiteSpace(nextScene) ? requestedSceneName : nextScene;
        }

        public string ResolveLoadableSceneRequest(string requestedSceneName)
        {
            return SceneWorkCatalog.GetLoadableSceneName(ResolveSceneRequest(requestedSceneName));
        }

        public void TryFastCompleteCurrentScene()
        {
            var step = TutorialSceneCatalog.GetStepForScene(SceneManager.GetActiveScene().name);
            if (TryFastCompleteSceneController(step))
                return;

            CompleteCurrentSceneAndLoadNext();
        }

        public void CompleteCurrentSceneAndLoadNext()
        {
            if (SceneManager.GetActiveScene().name == TutorialSceneCatalog.TitleScreenSceneName)
            {
                LoadSceneByCatalogName(TutorialSceneCatalog.IntroSceneName);
                return;
            }

            var currentSceneName = SceneManager.GetActiveScene().name;
            var nextScene = Flow.CompleteCurrentStep();
            if (StoryPackageRuntimeCatalog.TryGetNextScene(currentSceneName, out var packageNextScene))
                nextScene = packageNextScene;

            if (string.IsNullOrWhiteSpace(nextScene))
            {
                var sequenceRuntime = StorySequenceRuntimeController.Instance;
                if (sequenceRuntime != null && sequenceRuntime.TryContinueGeneratedSequence(this, currentSceneName))
                    return;

                ShowCompletionBanner = true;
                return;
            }

            LoadSceneByCatalogName(nextScene);
        }

        public void LoadPreviousScene()
        {
            var previousScene = Flow.GetPreviousScene();
            if (string.IsNullOrWhiteSpace(previousScene))
                return;

            LoadSceneByCatalogName(previousScene);
        }

        public void ReloadCurrentScene()
        {
            var currentScene = SceneManager.GetActiveScene();
            if (!currentScene.IsValid())
                return;

            LoadSceneByCatalogName(currentScene.name);
        }

        public void JumpToStep(TutorialStep step)
        {
            var sceneName = Flow.JumpToStep(step);
            if (string.IsNullOrWhiteSpace(sceneName))
                return;

            LoadSceneByCatalogName(sceneName);
        }

        public void ResetTutorial()
        {
            StorySequenceRuntimeController.Instance?.ClearSequenceState();
            Flow.Reset();
            ToolRecovery = new ToolRecoveryService();
            ShowCompletionBanner = false;
            LoadSceneByCatalogName(TutorialSceneCatalog.IntroSceneName);
        }

        public void NotifyGeneratedSequenceContinuationUnavailable()
        {
            ShowCompletionBanner = true;
        }

        public bool MarkToolRecovered(TutorialToolId toolId)
        {
            return ToolRecovery.Recover(toolId);
        }

        private static bool TryFastCompleteSceneController(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.ChickenHunt:
                    var chicken = FindAnyObjectByType<TutorialChickenSceneController>();
                    if (chicken == null)
                        return false;

                    chicken.FastCompleteForDev();
                    return true;
                case TutorialStep.FindTools:
                    var findTools = FindAnyObjectByType<TutorialFindToolsSceneController>();
                    if (findTools == null)
                        return false;

                    findTools.FastCompleteForDev();
                    return true;
                case TutorialStep.FarmTutorial:
                    var farm = FindAnyObjectByType<TutorialFarmSceneController>();
                    if (farm == null)
                        return false;

                    farm.FastCompleteForDev();
                    return true;
                case TutorialStep.PostChickenCutscene:
                case TutorialStep.MidpointPlaceholder:
                case TutorialStep.PreFarmCutscene:
                    var cutscene = FindAnyObjectByType<TutorialCutsceneSceneController>();
                    if (cutscene == null)
                        return false;

                    cutscene.FastCompleteForDev();
                    return true;
                default:
                    return false;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ShowCompletionBanner = false;

            if (scene.name == TutorialSceneCatalog.TitleScreenSceneName)
            {
                StorySequenceRuntimeController.Instance?.ClearSequenceState();
                Flow.Reset();
                return;
            }

            Flow.EnterScene(scene.name);
            TutorialSceneInstaller.InstallForScene(scene.name, this);
        }

        private static void LoadSceneByCatalogName(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return;

            SceneManager.LoadScene(SceneWorkCatalog.GetLoadableSceneName(sceneName));
        }
    }
}
