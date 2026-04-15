using FarmSimVR.Core.Tutorial;
using FarmSimVR.MonoBehaviours.ChickenGame;
using FarmSimVR.MonoBehaviours.Cinematics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    public sealed class TutorialChickenSceneController : MonoBehaviour
    {
        private ChickenGameManager _manager;
        private float _advanceAt = -1f;
        private GUIStyle _style;

        public void FastCompleteForDev()
        {
            if (_advanceAt < 0f)
                _advanceAt = Time.time + 0.05f;
        }

        private void Start()
        {
            _manager = FindAnyObjectByType<ChickenGameManager>();
            if (TryConfigureRuntimeMode())
                return;

            TryConfigurePackageMode();
        }

        private void Update()
        {
            if (_manager == null || _advanceAt >= 0f)
            {
                if (_advanceAt >= 0f && Time.time >= _advanceAt)
                {
                    _advanceAt = -1f;
                    TutorialFlowController.Instance?.CompleteCurrentSceneAndLoadNext();
                }

                return;
            }

            if (_manager.IsGameOver && _manager.IsWon)
                _advanceAt = Time.time + TutorialDevTuning.PostMinigameAdvanceDelay;
        }

        private void OnGUI()
        {
            if (_manager == null)
                return;

            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            _style.normal.textColor = Color.white;

            var text = _advanceAt >= 0f
                ? "Chicken secured. Heading to the next tutorial beat..."
                : BuildActiveInstructionText();
            var height = text.Contains("\n") ? 56f : 34f;

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(18f, 120f, 420f, height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(30f, 126f, 396f, height - 12f), text, _style);
        }

        private bool TryConfigureRuntimeMode()
        {
            if (_manager == null)
                return false;

            if (!GenerativeTurnRuntimeState.TryGetMinigameContract(SceneManager.GetActiveScene().name, out var minigame))
                return false;

            if (minigame == null || !string.Equals(minigame.adapter_id, "tutorial.chicken_chase", System.StringComparison.Ordinal))
                return false;

            _manager.ApplyPackageConfig(GenerativeMinigameContractReader.ToLegacySnapshot(minigame));
            return true;
        }

        private void TryConfigurePackageMode()
        {
            if (_manager == null)
                return;

            if (!StoryPackageRuntimeCatalog.TryGetMinigameConfig(SceneManager.GetActiveScene().name, out _, out var minigame))
                return;

            _manager.ApplyPackageConfig(minigame);
        }

        private string BuildActiveInstructionText()
        {
            var objective = string.IsNullOrWhiteSpace(_manager.CurrentObjectiveText)
                ? "Catch the chicken and drop it in the coop."
                : _manager.CurrentObjectiveText;
            var guidance = ResolveGuidanceText();
            return string.IsNullOrWhiteSpace(guidance)
                ? $"Tutorial Goal: {objective}"
                : $"Tutorial Goal: {objective}\n{guidance}";
        }

        private string ResolveGuidanceText()
        {
            return _manager.GuidanceLevel switch
            {
                "low" => string.Empty,
                "medium" => _manager.IsHoldingChicken
                    ? "Keep hold of the chicken and head for the coop."
                    : "Catch the chicken and bring it to the coop.",
                _ => _manager.IsHoldingChicken
                    ? "Keep clicking to hold on, then drop the chicken in the coop."
                    : "Get close, press E to catch the chicken, then take it to the coop.",
            };
        }
    }
}
