using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Farming;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class FarmFirstPersonRigUtilityTests
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
        public void EnsureRig_CreatesExplorerRigAtSpawnAndReusesMainCamera()
        {
            var spawn = new GameObject("SpawnPoint");
            spawn.transform.position = new Vector3(2f, 0f, 3f);

            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            cam.AddComponent<Camera>();

            var rig = FarmFirstPersonRigUtility.EnsureRig();

            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.GetComponent<CharacterController>(), Is.Not.Null);
            Assert.That(rig.GetComponent<FirstPersonExplorer>(), Is.Not.Null);
            Assert.That(cam.transform.parent, Is.EqualTo(rig.transform));
            Assert.That(rig.transform.position, Is.EqualTo(spawn.transform.position));
        }

        [Test]
        public void EnsureRig_WhenExplorerAlreadyExists_ReturnsExistingRig()
        {
            var player = new GameObject("Player");
            player.AddComponent<CharacterController>();
            var cameraChild = new GameObject("Camera");
            cameraChild.transform.SetParent(player.transform);
            cameraChild.AddComponent<Camera>();
            var existing = player.AddComponent<FirstPersonExplorer>();

            var rig = FarmFirstPersonRigUtility.EnsureRig();

            Assert.That(rig, Is.SameAs(existing));
            Assert.That(Object.FindObjectsByType<FirstPersonExplorer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }
    }
}
