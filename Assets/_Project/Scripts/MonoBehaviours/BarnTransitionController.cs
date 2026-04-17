using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Farming;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// DontDestroyOnLoad singleton that manages barn entry/exit transitions.
    /// FarmMain is never unloaded — Barn.unity is loaded additively on top.
    /// Coroutines run here so they survive Barn scene unload.
    /// </summary>
    public class BarnTransitionController : MonoBehaviour
    {
        private const float FadeDuration = 0.5f;
        private const float HoldBlackDuration = 0.3f;

        public static BarnTransitionController Instance { get; private set; }
        public bool IsTransitioning { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static BarnTransitionController GetOrCreate()
        {
            if (Instance != null)
                return Instance;
            var go = new GameObject("BarnTransitionController");
            return go.AddComponent<BarnTransitionController>();
        }

        public void EnterBarn(string barnScenePath, string spawnPointName)
        {
            if (IsTransitioning) return;
            StartCoroutine(EnterBarnCoroutine(barnScenePath, spawnPointName));
        }

        public void ExitBarn(string barnScenePath, string spawnPointName)
        {
            if (IsTransitioning) return;
            StartCoroutine(ExitBarnCoroutine(barnScenePath, spawnPointName));
        }

        private IEnumerator EnterBarnCoroutine(string barnScenePath, string spawnPointName)
        {
            IsTransitioning = true;

            var explorer = FindAnyObjectByType<ThirdPersonFarmExplorer>(FindObjectsInactive.Include);
            if (explorer != null) explorer.enabled = false;

            yield return FadeOut();

            var loadOp = SceneManager.LoadSceneAsync(barnScenePath, LoadSceneMode.Additive);
            if (loadOp == null)
            {
                yield return FadeIn();
                if (explorer != null) explorer.enabled = true;
                IsTransitioning = false;
                yield break;
            }
            yield return loadOp;
            yield return null;

            TeleportPlayer(spawnPointName);

            yield return new WaitForSeconds(HoldBlackDuration);
            yield return FadeIn();

            if (explorer != null) explorer.enabled = true;
            IsTransitioning = false;
        }

        private IEnumerator ExitBarnCoroutine(string barnScenePath, string spawnPointName)
        {
            IsTransitioning = true;

            var explorer = FindAnyObjectByType<ThirdPersonFarmExplorer>(FindObjectsInactive.Include);
            if (explorer != null) explorer.enabled = false;

            yield return FadeOut();

            TeleportPlayer(spawnPointName);

            yield return new WaitForSeconds(HoldBlackDuration);

            var scene = SceneManager.GetSceneByPath(barnScenePath);
            if (scene.IsValid() && scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(barnScenePath);

            yield return FadeIn();

            if (explorer != null) explorer.enabled = true;
            IsTransitioning = false;
        }

        private static void TeleportPlayer(string spawnPointName)
        {
            var spawn = GameObject.Find(spawnPointName);
            if (spawn == null) return;

            var cc = FindAnyObjectByType<CharacterController>(FindObjectsInactive.Include);
            if (cc == null) return;

            cc.enabled = false;
            cc.transform.SetPositionAndRotation(
                spawn.transform.position + Vector3.up * 0.15f,
                spawn.transform.rotation);
            cc.enabled = true;
            Physics.SyncTransforms();
        }

        private IEnumerator FadeOut()
        {
            if (ScreenEffects.Instance == null) yield break;
            bool done = false;
            ScreenEffects.Instance.FadeToBlack(FadeDuration, () => done = true);
            while (!done) yield return null;
        }

        private IEnumerator FadeIn()
        {
            if (ScreenEffects.Instance == null) yield break;
            bool done = false;
            ScreenEffects.Instance.FadeFromBlack(FadeDuration, () => done = true);
            while (!done) yield return null;
        }
    }
}
