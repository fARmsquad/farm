using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class FarmGirlAnimatorControllerTests
    {
        private const string ControllerPath =
            "Assets/_Project/Animations/FarmGirl/FarmGirlAnimator.controller";

        private AnimatorController LoadController()
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.IsNotNull(ctrl, $"FarmGirlAnimator.controller not found at {ControllerPath}");
            return ctrl;
        }

        [Test]
        public void FarmGirlAnimator_HasSpeedFloatParameter()
        {
            var ctrl = LoadController();
            bool found = System.Array.Exists(ctrl.parameters,
                p => p.name == "Speed" && p.type == AnimatorControllerParameterType.Float);
            Assert.IsTrue(found, "Expected a float parameter named 'Speed'");
        }

        [Test]
        public void FarmGirlAnimator_HasIdleAndWalkStates()
        {
            var ctrl = LoadController();
            var sm = ctrl.layers[0].stateMachine;
            bool hasIdle = System.Array.Exists(sm.states, s => s.state.name == "Idle");
            bool hasWalk = System.Array.Exists(sm.states, s => s.state.name == "Walk");
            Assert.IsTrue(hasIdle, "Missing 'Idle' state in Base Layer");
            Assert.IsTrue(hasWalk, "Missing 'Walk' state in Base Layer");
        }

        [Test]
        public void FarmGirlAnimator_DefaultStateIsIdle()
        {
            var ctrl = LoadController();
            var sm = ctrl.layers[0].stateMachine;
            Assert.AreEqual("Idle", sm.defaultState.name, "Default state should be 'Idle'");
        }

        [Test]
        public void FarmGirlAnimator_IdleToWalkTransition_UsesSpeedGreaterThan()
        {
            var ctrl = LoadController();
            var sm = ctrl.layers[0].stateMachine;
            AnimatorState idle = System.Array.Find(sm.states, s => s.state.name == "Idle").state;
            Assert.IsNotNull(idle);

            bool found = false;
            foreach (var t in idle.transitions)
            {
                if (t.destinationState != null && t.destinationState.name == "Walk")
                {
                    foreach (var c in t.conditions)
                    {
                        if (c.parameter == "Speed" && c.mode == AnimatorConditionMode.Greater)
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }
            Assert.IsTrue(found, "Idle→Walk transition should use Speed > threshold");
        }

        [Test]
        public void FarmGirlAnimator_WalkToIdleTransition_UsesSpeedLessThan()
        {
            var ctrl = LoadController();
            var sm = ctrl.layers[0].stateMachine;
            AnimatorState walk = System.Array.Find(sm.states, s => s.state.name == "Walk").state;
            Assert.IsNotNull(walk);

            bool found = false;
            foreach (var t in walk.transitions)
            {
                if (t.destinationState != null && t.destinationState.name == "Idle")
                {
                    foreach (var c in t.conditions)
                    {
                        if (c.parameter == "Speed" && c.mode == AnimatorConditionMode.Less)
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }
            Assert.IsTrue(found, "Walk→Idle transition should use Speed < threshold");
        }
    }
}
