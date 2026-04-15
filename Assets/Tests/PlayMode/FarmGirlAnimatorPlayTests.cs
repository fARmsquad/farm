using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using FarmSimVR.MonoBehaviours;

namespace FarmSimVR.Tests.PlayMode
{
    [TestFixture]
    public class FarmGirlAnimatorPlayTests
    {
        private const string ControllerPath =
            "Assets/_Project/Animations/FarmGirl/FarmGirlAnimator.controller";

        private GameObject _playerGo;

        [SetUp]
        public void SetUp()
        {
            _playerGo = new GameObject("TestPlayer");
            _playerGo.AddComponent<CharacterController>();

            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            var anim = _playerGo.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;

            _playerGo.AddComponent<TownPlayerController>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_playerGo);
        }

        [UnityTest]
        public IEnumerator TownPlayerController_WhenAtRest_AnimatorSpeedIsZero()
        {
            yield return null; // let Awake + first Update run

            var anim = _playerGo.GetComponent<Animator>();
            Assert.AreEqual(0f, anim.GetFloat("Speed"), 0.01f,
                "Animator Speed should be 0 when the character is stationary");
        }
    }
}
