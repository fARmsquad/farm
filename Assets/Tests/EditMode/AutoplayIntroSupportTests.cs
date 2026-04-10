using NUnit.Framework;
using UnityEngine;
using FarmSimVR.MonoBehaviours.Autoplay;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class AutoplayIntroSupportTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var meter in Object.FindObjectsByType<ChaosMeter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(meter.gameObject);

            foreach (var pollo in Object.FindObjectsByType<ElPolloController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(pollo.gameObject);

            foreach (var sequencer in Object.FindObjectsByType<CinematicSequencer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(sequencer.gameObject);
        }

        [Test]
        public void IntroSequenceBuilder_BuildsSequence_ThatValidates()
        {
            var sequencerObject = new GameObject("Sequencer");
            var sequencer = sequencerObject.AddComponent<CinematicSequencer>();

            var sequence = IntroSequenceBuilder.Build();

            Assert.That(sequence, Is.Not.Null);
            Assert.That(sequence.steps, Is.Not.Null);
            Assert.That(sequence.steps.Length, Is.GreaterThan(0));
            Assert.That(sequencer.Validate(sequence), Is.True);

            Object.DestroyImmediate(sequence);
        }

        [Test]
        public void ChaosMeter_AddFill_ClampsToValidRange()
        {
            var meterObject = new GameObject("ChaosMeter");
            var meter = meterObject.AddComponent<ChaosMeter>();

            meter.AddFill(0.35f);
            meter.AddFill(0.9f);

            Assert.That(meter.CurrentFill, Is.EqualTo(1f));

            meter.AddFill(-2f);

            Assert.That(meter.CurrentFill, Is.EqualTo(0f));
        }

        [Test]
        public void ElPolloController_HighChaos_BecomesCatchable()
        {
            var polloObject = new GameObject("ElPollo");
            var pollo = polloObject.AddComponent<ElPolloController>();

            pollo.UpdateFromChaosMeter(0.9f);

            Assert.That(pollo.CurrentPhase, Is.EqualTo(ElPolloPhase.Tired));
            Assert.That(pollo.IsCatchable, Is.True);
        }
    }
}
