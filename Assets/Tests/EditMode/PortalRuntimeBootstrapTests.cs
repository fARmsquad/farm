using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Portal;
using FarmSimVR.MonoBehaviours.Farming;
using NUnit.Framework;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class PortalRuntimeBootstrapTests
    {
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            DestroyPortalManager();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyPortalManager();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void TryBootstrapForScene_WithPortalSceneAndNoManager_CreatesRuntimePortalManager()
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.AddComponent<CharacterController>();
            player.AddComponent<TownPlayerController>();

            var portal = new GameObject("Portal");
            var collider = portal.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            portal.AddComponent<PortalTrigger>();

            bool bootstrapped = PortalRuntimeBootstrap.TryBootstrapForScene(SceneManager.GetActiveScene());

            Assert.That(bootstrapped, Is.True);
            Assert.That(PortalManager.Instance, Is.Not.Null);
            Assert.That(PortalManager.Instance.CurrentAreaScenePath, Is.EqualTo(SceneManager.GetActiveScene().path));
        }

        [Test]
        public void TryBootstrapForScene_WithoutPortalTrigger_DoesNothing()
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.AddComponent<CharacterController>();
            player.AddComponent<FirstPersonExplorer>();

            bool bootstrapped = PortalRuntimeBootstrap.TryBootstrapForScene(SceneManager.GetActiveScene());

            Assert.That(bootstrapped, Is.False);
            Assert.That(PortalManager.Instance == null, Is.True);
        }

        [Test]
        public void TryBootstrapForScene_WithGenericCharacterController_CreatesRuntimePortalManager()
        {
            var player = new GameObject("GenericPlayer");
            player.AddComponent<CharacterController>();

            var portal = new GameObject("Portal");
            var collider = portal.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            portal.AddComponent<PortalTrigger>();

            bool bootstrapped = PortalRuntimeBootstrap.TryBootstrapForScene(SceneManager.GetActiveScene());

            Assert.That(bootstrapped, Is.True);
            Assert.That(PortalManager.Instance, Is.Not.Null);
        }

        private static void DestroyPortalManager()
        {
            if (PortalManager.Instance != null)
                Object.DestroyImmediate(PortalManager.Instance.gameObject);

            foreach (var bootstrap in Object.FindObjectsByType<PortalManager>(FindObjectsInactive.Include))
                Object.DestroyImmediate(bootstrap.gameObject);

            typeof(PortalManager)
                .GetProperty(nameof(PortalManager.Instance), BindingFlags.Public | BindingFlags.Static)
                ?.SetValue(null, null);
        }
    }
}
