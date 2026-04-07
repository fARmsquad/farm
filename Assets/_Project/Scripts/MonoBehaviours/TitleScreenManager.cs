using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FarmSimVR.MonoBehaviours
{
    public class TitleScreenManager : MonoBehaviour
    {
        [SerializeField] private string targetSceneName = "WorldMain";
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private float fadeDuration = 1.2f;

        private Canvas fadeCanvas;
        private Image fadeImage;
        private bool isTransitioning;

        private void Start()
        {
            if (musicSource != null && !musicSource.isPlaying)
                musicSource.Play();

            CreateFadeOverlay();
        }

        public void StartGame()
        {
            if (isTransitioning) return;
            isTransitioning = true;
            StartCoroutine(TransitionToGame());
        }

        private IEnumerator TransitionToGame()
        {
            // Fade to black
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeDuration;
                fadeImage.color = new Color(0, 0, 0, t);
                if (musicSource != null)
                    musicSource.volume = 1f - t;
                yield return null;
            }
            fadeImage.color = Color.black;

            if (musicSource != null)
                musicSource.Stop();

            SceneManager.LoadScene(targetSceneName);
        }

        private void CreateFadeOverlay()
        {
            var go = new GameObject("FadeOverlay");
            go.transform.SetParent(transform);
            fadeCanvas = go.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 999;

            var imgGo = new GameObject("FadeImage");
            imgGo.transform.SetParent(go.transform, false);
            fadeImage = imgGo.AddComponent<Image>();
            fadeImage.color = new Color(0, 0, 0, 0);
            fadeImage.raycastTarget = false;

            var rect = imgGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
        }
    }
}
