using System;
using NUnit.Framework;
using UnityEngine;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class CinematicStepTypeTests
    {
        [Test]
        public void StepType_Has17Values()
        {
            var values = Enum.GetValues(typeof(CinematicStepType));
            Assert.AreEqual(17, values.Length);
        }

        [Test]
        [TestCase(CinematicStepType.CameraMove)]
        [TestCase(CinematicStepType.Dialogue)]
        [TestCase(CinematicStepType.Wait)]
        [TestCase(CinematicStepType.PlaySFX)]
        [TestCase(CinematicStepType.PlayMusic)]
        [TestCase(CinematicStepType.StopMusic)]
        [TestCase(CinematicStepType.Fade)]
        [TestCase(CinematicStepType.Shake)]
        [TestCase(CinematicStepType.Letterbox)]
        [TestCase(CinematicStepType.ObjectivePopup)]
        [TestCase(CinematicStepType.MissionStart)]
        [TestCase(CinematicStepType.MissionComplete)]
        [TestCase(CinematicStepType.EnablePlayerControl)]
        [TestCase(CinematicStepType.DisablePlayerControl)]
        [TestCase(CinematicStepType.ActivateNPC)]
        [TestCase(CinematicStepType.DeactivateNPC)]
        [TestCase(CinematicStepType.SetLighting)]
        public void StepType_ContainsExpectedValue(CinematicStepType type)
        {
            Assert.IsTrue(Enum.IsDefined(typeof(CinematicStepType), type));
        }
    }

    [TestFixture]
    public class CinematicStepTests
    {
        [Test]
        public void Step_DefaultValues_AreZeroAndFalse()
        {
            var step = new CinematicStep();

            Assert.AreEqual(CinematicStepType.CameraMove, step.type);
            Assert.IsNull(step.stringParam);
            Assert.AreEqual(0f, step.floatParam);
            Assert.AreEqual(0, step.intParam);
            Assert.AreEqual(0f, step.duration);
            Assert.IsFalse(step.waitForCompletion);
        }

        [Test]
        public void MissionStart_ParsesPipeDelimiter()
        {
            var step = new CinematicStep
            {
                type = CinematicStepType.MissionStart,
                stringParam = "POLLO LOCO|Capture El Pollo Loco"
            };

            string[] parts = step.stringParam.Split('|');
            Assert.AreEqual(2, parts.Length);
            Assert.AreEqual("POLLO LOCO", parts[0]);
            Assert.AreEqual("Capture El Pollo Loco", parts[1]);
        }
    }

    [TestFixture]
    public class CinematicSequenceTests
    {
        [Test]
        public void Sequence_CreatedWithEmptySteps()
        {
            var sequence = ScriptableObject.CreateInstance<CinematicSequence>();

            Assert.IsNotNull(sequence);
            Assert.IsNotNull(sequence.steps);
            Assert.AreEqual(0, sequence.steps.Length);

            Object.DestroyImmediate(sequence);
        }

        [Test]
        public void Sequence_StepsAreAssignable()
        {
            var sequence = ScriptableObject.CreateInstance<CinematicSequence>();
            sequence.steps = new[]
            {
                new CinematicStep { type = CinematicStepType.Wait, duration = 1f },
                new CinematicStep { type = CinematicStepType.Fade, floatParam = 1f, duration = 0.5f }
            };

            Assert.AreEqual(2, sequence.steps.Length);
            Assert.AreEqual(CinematicStepType.Wait, sequence.steps[0].type);
            Assert.AreEqual(CinematicStepType.Fade, sequence.steps[1].type);

            Object.DestroyImmediate(sequence);
        }
    }
}
