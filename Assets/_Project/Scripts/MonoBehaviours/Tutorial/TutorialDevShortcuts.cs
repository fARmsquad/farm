using FarmSimVR.Core.Tutorial;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    public sealed class TutorialDevShortcuts : MonoBehaviour
    {
        public const string ShortcutSummary =
            "Shift+. Next  Shift+, Back  Shift+/ Reload  Shift+1-7 Scene 01-07  Shift+0 Reset";

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;

        private void Update()
        {
            var controller = TutorialFlowController.Instance;
            if (controller == null)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null || !IsShiftHeld(keyboard))
                return;

            if (keyboard.periodKey.wasPressedThisFrame)
            {
                controller.CompleteCurrentSceneAndLoadNext();
                return;
            }

            if (keyboard.commaKey.wasPressedThisFrame)
            {
                controller.LoadPreviousScene();
                return;
            }

            if (keyboard.slashKey.wasPressedThisFrame)
            {
                controller.ReloadCurrentScene();
                return;
            }

            if (keyboard.digit0Key.wasPressedThisFrame)
            {
                controller.ResetTutorial();
                return;
            }

            HandleSceneJump(controller, keyboard);
        }

        private void OnGUI()
        {
            var controller = TutorialFlowController.Instance;
            if (controller == null)
                return;

            var activeScene = SceneManager.GetActiveScene().name;
            if (TutorialSceneCatalog.GetStepForScene(activeScene) == TutorialStep.None &&
                activeScene != TutorialSceneCatalog.TitleScreenSceneName)
                return;

            BuildStyles();

            var state = controller.Flow.State;
            var nextScene = activeScene == TutorialSceneCatalog.TitleScreenSceneName
                ? TutorialSceneCatalog.IntroSceneName
                : controller.Flow.GetNextScene() ?? "END";
            var activeLabel = BuildSceneLabel(activeScene);
            var nextLabel = BuildSceneLabel(nextScene);
            var text =
                $"Tutorial Step: {state.CurrentStep}\n" +
                $"Scene: {activeLabel}\n" +
                $"Next: {nextLabel}\n" +
                $"Flags: Intro={Flag(state.IntroComplete)}  Chicken={Flag(state.ChickenHuntComplete)}  " +
                $"Tools={Flag(state.FindToolsComplete)}  Farm={Flag(state.FarmTutorialComplete)}\n" +
                ShortcutSummary;

            GUI.Box(new Rect(16f, 16f, 620f, 92f), GUIContent.none, _boxStyle);
            GUI.Label(new Rect(28f, 26f, 596f, 76f), text, _labelStyle);

            if (!controller.ShowCompletionBanner)
                return;

            GUI.Box(new Rect((Screen.width - 380f) * 0.5f, 32f, 380f, 56f), GUIContent.none, _boxStyle);
            GUI.Label(
                new Rect((Screen.width - 360f) * 0.5f, 46f, 360f, 28f),
                "Tutorial complete. Shift+/ to replay the final scene.",
                _labelStyle);
        }

        private static bool IsShiftHeld(Keyboard keyboard)
        {
            return keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        }

        private static string Flag(bool value)
        {
            return value ? "Y" : "N";
        }

        private static string BuildSceneLabel(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || sceneName == "END")
                return sceneName;

            return SceneWorkCatalog.TryGetBySceneName(sceneName, out var scene)
                ? $"{scene.NumberLabel} {scene.DisplayName}"
                : sceneName;
        }

        private static void HandleSceneJump(TutorialFlowController controller, Keyboard keyboard)
        {
            if (keyboard.digit1Key.wasPressedThisFrame)
                controller.JumpToStep(TutorialStep.Intro);
            else if (keyboard.digit2Key.wasPressedThisFrame)
                controller.JumpToStep(TutorialStep.ChickenHunt);
            else if (keyboard.digit3Key.wasPressedThisFrame)
                controller.JumpToStep(TutorialStep.PostChickenCutscene);
            else if (keyboard.digit4Key.wasPressedThisFrame)
                controller.JumpToStep(TutorialStep.MidpointPlaceholder);
            else if (keyboard.digit5Key.wasPressedThisFrame)
                controller.JumpToStep(TutorialStep.FindTools);
            else if (keyboard.digit6Key.wasPressedThisFrame)
                controller.JumpToStep(TutorialStep.PreFarmCutscene);
            else if (keyboard.digit7Key.wasPressedThisFrame)
                controller.JumpToStep(TutorialStep.FarmTutorial);
        }

        private void BuildStyles()
        {
            if (_boxStyle != null)
                return;

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = Texture2D.whiteTexture;
            _boxStyle.normal.textColor = Color.white;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                richText = true
            };
            _labelStyle.normal.textColor = Color.white;
        }
    }
}
