using UnityEngine;
using UnityEngine.SceneManagement;
using FarmSimVR.Core.Tutorial;
using FarmSimVR.MonoBehaviours.Tutorial;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Minimal scene-loading helper. Exposes a single method suitable for wiring
    /// to UnityEvent persistent calls (e.g. CinematicSequencer.OnSequenceComplete).
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public void LoadScene(string sceneName)
        {
            var controller = TutorialFlowController.Instance;
            var resolvedScene = controller != null
                ? controller.ResolveLoadableSceneRequest(sceneName)
                : SceneWorkCatalog.GetLoadableSceneName(sceneName);
            SceneManager.LoadScene(resolvedScene);
        }
    }
}
