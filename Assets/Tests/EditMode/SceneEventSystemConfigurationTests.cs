using System;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class SceneEventSystemConfigurationTests
    {
        private static readonly Type StandaloneInputModuleType =
            Type.GetType("UnityEngine.EventSystems.StandaloneInputModule, UnityEngine.UI");

        private static readonly Type InputSystemUiInputModuleType =
            Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

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

        [TestCase("Assets/_Project/Scenes/Intro.unity")]
        [TestCase("Assets/_Project/Scenes/TitleScreen.unity")]
        public void SceneEventSystem_UsesInputSystemUiInputModule(string scenePath)
        {
            Assert.That(StandaloneInputModuleType, Is.Not.Null, "Unity UI StandaloneInputModule type should resolve.");
            Assert.That(InputSystemUiInputModuleType, Is.Not.Null, "Input System UI module type should resolve.");

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Assert.That(scene.IsValid(), Is.True, $"Failed to open scene '{scenePath}'.");

            var eventSystem = Object.FindFirstObjectByType<EventSystem>();

            Assert.That(eventSystem, Is.Not.Null, $"Scene '{scenePath}' should contain an EventSystem.");
            Assert.That(eventSystem.GetComponent(StandaloneInputModuleType), Is.Null,
                $"Scene '{scenePath}' should not use StandaloneInputModule when legacy input is disabled.");
            Assert.That(eventSystem.GetComponent(InputSystemUiInputModuleType), Is.Not.Null,
                $"Scene '{scenePath}' should use InputSystemUIInputModule.");
        }
    }
}
