using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    public static class TutorialRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureTutorialRuntime()
        {
            if (Object.FindAnyObjectByType<TutorialFlowController>() != null)
                return;

            var runtime = new GameObject("TutorialRuntime");
            runtime.AddComponent<TutorialFlowController>();
            runtime.AddComponent<TutorialDevShortcuts>();
            runtime.AddComponent<SceneWorkLabelOverlay>();
            Object.DontDestroyOnLoad(runtime);
        }
    }
}
