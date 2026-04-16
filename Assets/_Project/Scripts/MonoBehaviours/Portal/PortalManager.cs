using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Farming;

namespace FarmSimVR.MonoBehaviours.Portal
{
    /// <summary>
    /// Singleton that orchestrates portal transitions between additive area scenes.
    /// Handles fade-to-black, scene swap, player repositioning, and fade-from-black.
    /// Lives in the Core scene and persists across area transitions.
    /// </summary>
    public class PortalManager : MonoBehaviour
    {
        private const float DEFAULT_FADE_DURATION = 0.5f;
        private const float DEFAULT_HOLD_BLACK_DURATION = 0.3f;
        /// <summary>
        /// After teleport, nudge the player slightly above the floor so the CharacterController
        /// does not start intersecting the ground mesh (avoids tunneling / fall-through).
        /// </summary>
        private const float TeleportGroundClearancePadding = 0.15f;

        /// <summary>Singleton accessor.</summary>
        public static PortalManager Instance { get; private set; }

        [Header("Player References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private CharacterController playerCharacterController;

        [Header("Scene Settings")]
        [Tooltip("The area scene to load additively on startup")]
        [SerializeField] private string initialScenePath;

        [Header("Transition Timing")]
        [SerializeField] private float fadeDuration = DEFAULT_FADE_DURATION;
        [SerializeField] private float holdBlackDuration = DEFAULT_HOLD_BLACK_DURATION;
        private bool bootstrapFromActiveScene;

        /// <summary>True while a transition coroutine is running.</summary>
        public bool IsTransitioning { get; private set; }

        /// <summary>Scene path of the currently loaded additive area.</summary>
        public string CurrentAreaScenePath { get; private set; }

        private void Awake()
        {
            RegisterAsInstance();
        }

        private void Start()
        {
            if (bootstrapFromActiveScene)
            {
                SetActiveAreaScene(CurrentAreaScenePath);
                return;
            }

            if (string.IsNullOrEmpty(initialScenePath))
            {
                Debug.LogError("[PortalManager] initialScenePath is not set. No area scene will be loaded.");
                return;
            }

            StartCoroutine(LoadInitialScene());
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Kicks off the full transition coroutine: fade out, unload current area,
        /// load destination area, reposition player, fade in.
        /// </summary>
        public void Transition(string destinationScenePath, string spawnPointName)
        {
            if (IsTransitioning)
            {
                Debug.LogWarning("[PortalManager] Transition already in progress, ignoring request.");
                return;
            }

            StartCoroutine(TransitionCoroutine(destinationScenePath, spawnPointName));
        }

        public void BootstrapFromActiveScene(
            Transform runtimePlayerTransform,
            CharacterController runtimePlayerCharacterController,
            string activeScenePath)
        {
            RegisterAsInstance();
            if (Instance != this)
                return;

            playerTransform = runtimePlayerTransform;
            playerCharacterController = runtimePlayerCharacterController;
            initialScenePath = activeScenePath;
            CurrentAreaScenePath = activeScenePath;
            bootstrapFromActiveScene = true;
        }

        private void RegisterAsInstance()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private IEnumerator LoadInitialScene()
        {
            var loadOp = SceneManager.LoadSceneAsync(initialScenePath, LoadSceneMode.Additive);
            if (loadOp == null)
            {
                Debug.LogError($"[PortalManager] Failed to start loading initial scene: {initialScenePath}");
                yield break;
            }

            yield return loadOp;
            CurrentAreaScenePath = initialScenePath;
            SetActiveAreaScene(initialScenePath);
        }

        private IEnumerator TransitionCoroutine(string destinationScenePath, string spawnPointName)
        {
            IsTransitioning = true;

            // Disable player input during transition
            var playerController = playerTransform != null
                ? playerTransform.GetComponent<TownPlayerController>()
                : null;

            if (playerController != null)
                playerController.enabled = false;

            // Fade to black
            bool fadeDone = false;
            if (ScreenEffects.Instance != null)
            {
                ScreenEffects.Instance.FadeToBlack(fadeDuration, () => fadeDone = true);
                while (!fadeDone)
                    yield return null;
            }
            else
            {
                Debug.LogWarning("[PortalManager] ScreenEffects.Instance is null, skipping fade out.");
            }

            // Unload current area
            if (!string.IsNullOrEmpty(CurrentAreaScenePath))
            {
                var unloadOp = SceneManager.UnloadSceneAsync(CurrentAreaScenePath);
                if (unloadOp != null)
                    yield return unloadOp;
                else
                    Debug.LogWarning($"[PortalManager] Failed to unload scene: {CurrentAreaScenePath}");
            }

            // Load destination area
            var loadOp = SceneManager.LoadSceneAsync(destinationScenePath, LoadSceneMode.Additive);
            if (loadOp == null)
            {
                Debug.LogError($"[PortalManager] Failed to load destination scene: {destinationScenePath}. Fading back in.");
                if (playerController != null) playerController.enabled = true;
                yield return FadeBackIn();
                yield break;
            }

            yield return loadOp;
            CurrentAreaScenePath = destinationScenePath;
            SetActiveAreaScene(destinationScenePath);

            // Wait one frame so Awake()/Start() on newly loaded scene objects run
            // (e.g. FarmPlotInteractionController creates ThirdPersonFarmExplorer).
            yield return null;

            // Destroy any duplicate player created by the loaded scene's rig system.
            // CoreScene's Player_FarmGirl is the only player we want.
            DestroyDuplicatePlayerRigs();

            // Reposition player at spawn point and snap camera
            TeleportPlayerToSpawn(spawnPointName);

            // Re-enable player input and camera
            var townCameraFollow = FindAnyObjectByType<TownCameraFollow>(FindObjectsInactive.Include);
            if (townCameraFollow != null)
            {
                townCameraFollow.enabled = true;
                townCameraFollow.SnapToTarget();
            }

            if (playerController != null)
            {
                playerController.enabled = true;
                playerController.RefreshInteractables();
            }

            // Brief hold while screen is black
            yield return new WaitForSeconds(holdBlackDuration);

            // Fade from black
            yield return FadeBackIn();
        }

        /// <summary>
        /// Fades the screen back in.
        /// </summary>
        private IEnumerator FadeBackIn()
        {
            bool fadeDone = false;
            if (ScreenEffects.Instance != null)
            {
                ScreenEffects.Instance.FadeFromBlack(fadeDuration, () => fadeDone = true);
                while (!fadeDone)
                    yield return null;
            }
            else
            {
                Debug.LogWarning("[PortalManager] ScreenEffects.Instance is null, skipping fade in.");
            }

            IsTransitioning = false;
        }

        /// <summary>
        /// Destroys any duplicate player rigs created by the loaded scene's
        /// rig system (ThirdPersonFarmExplorer, FirstPersonExplorer).
        /// These scenes were designed to run standalone and create their own player,
        /// but when loaded additively CoreScene already provides Player_FarmGirl.
        /// Two players = two scripts fighting over Camera.main = camera drift.
        /// </summary>
        private void DestroyDuplicatePlayerRigs()
        {
            var farmExplorers = FindObjectsByType<ThirdPersonFarmExplorer>(FindObjectsSortMode.None);
            foreach (var explorer in farmExplorers)
            {
                Debug.Log($"[PortalManager] Destroying duplicate player rig: {explorer.gameObject.name}");
                Destroy(explorer.gameObject);
            }

            var fpExplorers = FindObjectsByType<FirstPersonExplorer>(FindObjectsSortMode.None);
            foreach (var explorer in fpExplorers)
            {
                Debug.Log($"[PortalManager] Destroying duplicate FP player rig: {explorer.gameObject.name}");
                Destroy(explorer.gameObject);
            }
        }

        /// <summary>
        /// Teleports the player to the named spawn point in the destination scene.
        /// </summary>
        private void TeleportPlayerToSpawn(string spawnPointName)
        {
            if (playerTransform == null)
            {
                Debug.LogError("[PortalManager] playerTransform is not assigned.");
                return;
            }

            GameObject spawnPoint = GameObject.Find(spawnPointName);
            if (spawnPoint == null)
            {
                Debug.LogWarning($"[PortalManager] Spawn point '{spawnPointName}' not found. Defaulting to Vector3.zero.");
                SetPlayerPosition(Vector3.zero, Quaternion.identity);
                return;
            }

            SetPlayerPosition(spawnPoint.transform.position, spawnPoint.transform.rotation);
        }

        /// <summary>
        /// Sets the player position, temporarily disabling CharacterController
        /// to prevent its internal state from overriding the new position.
        /// </summary>
        private void SetPlayerPosition(Vector3 position, Quaternion rotation)
        {
            if (playerCharacterController != null)
                playerCharacterController.enabled = false;

            position += Vector3.up * TeleportGroundClearancePadding;
            playerTransform.SetPositionAndRotation(position, rotation);

            if (playerCharacterController != null)
            {
                playerCharacterController.enabled = true;
                Physics.SyncTransforms();
            }
        }

        /// <summary>
        /// Sets the newly loaded area scene as the active scene for proper
        /// object creation, then forces the lighting controller to snap
        /// its values so the area scene's baked lighting doesn't bleed through.
        /// </summary>
        private void SetActiveAreaScene(string scenePath)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (scene.IsValid() && scene.isLoaded)
                SceneManager.SetActiveScene(scene);

            // SetActiveScene stomps RenderSettings with the destination scene's
            // baked ambient/fog/skybox. Force the day-night controller to snap
            // all values back to the current time-of-day targets immediately.
            var lighting = FindAnyObjectByType<FarmLightingController>();
            if (lighting != null && FarmDayClockDriver.Instance != null)
                lighting.ForceApply(FarmDayClockDriver.Instance.Clock.NormalisedTime);
        }
    }
}
