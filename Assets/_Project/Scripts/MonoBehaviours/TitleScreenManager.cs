using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmSimVR.MonoBehaviours
{
    public class TitleScreenManager : MonoBehaviour
    {
        [SerializeField] private string farmMainSceneName = "Intro";
        [SerializeField] private AudioSource musicSource;

        private void Start()
        {
            if (musicSource != null && !musicSource.isPlaying)
                musicSource.Play();
        }

        public void StartGame()
        {
            if (musicSource != null)
                musicSource.Stop();
            SceneManager.LoadScene(farmMainSceneName);
        }
    }
}
