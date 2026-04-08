using UnityEngine;
using UnityEngine.SceneManagement;

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
            SceneManager.LoadScene(sceneName);
        }
    }
}
