using System.Reflection;
using FarmSimVR.Core.Tutorial;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Tutorial;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class TutorialFastCompleteTests
    {
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void TutorialFlowController_TryFastCompleteCurrentScene_ShowsCompletionBanner_ForTerminalStoryPackageBeat()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Tutorial_PreFarmCutscene.unity", OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);

            StoryPackageRuntimeCatalog.ResetCacheForTests();

            var runtime = new GameObject("TutorialRuntime");
            var controller = runtime.AddComponent<TutorialFlowController>();
            var flow = new TutorialFlowService();
            flow.EnterScene(TutorialSceneCatalog.PreFarmCutsceneSceneName);
            SetPrivateField(controller, "<Flow>k__BackingField", flow);
            SetPrivateField(controller, "<ToolRecovery>k__BackingField", new ToolRecoveryService());

            controller.TryFastCompleteCurrentScene();

            Assert.That(controller.ShowCompletionBanner, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Tutorial_PreFarmCutscene"));
        }

        [Test]
        public void TutorialFindToolsSceneController_FastCompleteForDev_CompletesPackageObjective()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/FindToolsGame.unity", OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);

            StoryPackageRuntimeCatalog.ResetCacheForTests();

            var gameObject = new GameObject("TutorialFindToolsSceneController");
            var controller = gameObject.AddComponent<TutorialFindToolsSceneController>();

            InvokePrivateInstance(controller, "Start");
            controller.FastCompleteForDev();

            Assert.That(controller.CurrentObjectiveText, Is.EqualTo("Tools recovered."));
            Assert.That(ReadPrivateFloat(controller, "_completionAt"), Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void TutorialFarmSceneController_FastCompleteForDev_CompletesPackageObjective()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/FarmMain.unity", OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);

            StoryPackageRuntimeCatalog.ResetCacheForTests();

            var controller = Object.FindFirstObjectByType<TutorialFarmSceneController>();
            Assert.That(controller, Is.Not.Null);

            InvokePrivateInstance(controller, "Start");
            controller.FastCompleteForDev();

            Assert.That(controller.IsComplete, Is.True);
            Assert.That(ReadPrivateBool(controller, "_showCompletion"), Is.True);
        }

        private static void InvokePrivateInstance(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, $"Missing private method '{methodName}'.");
            method.Invoke(target, args);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            field.SetValue(target, value);
        }

        private static float ReadPrivateFloat(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            return (float)field.GetValue(target);
        }

        private static bool ReadPrivateBool(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            return (bool)field.GetValue(target);
        }
    }
}
