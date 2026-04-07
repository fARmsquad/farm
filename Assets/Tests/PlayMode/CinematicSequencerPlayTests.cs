using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Hunting;

namespace FarmSimVR.Tests.PlayMode
{
    [TestFixture]
    public class CinematicSequencerPlayTests
    {
        private GameObject _sequencerGo;
        private CinematicSequencer _sequencer;

        [SetUp]
        public void SetUp()
        {
            _sequencerGo = new GameObject("TestSequencer");
            _sequencer = _sequencerGo.AddComponent<CinematicSequencer>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_sequencerGo);
        }

        [UnityTest]
        public IEnumerator Play_EmptySequence_FiresCompleteImmediately()
        {
            var sequence = ScriptableObject.CreateInstance<CinematicSequence>();
            bool completed = false;
            _sequencer.OnSequenceComplete.AddListener(() => completed = true);

            _sequencer.Play(sequence);
            yield return null;

            Assert.IsTrue(completed);
            Object.Destroy(sequence);
        }

        [UnityTest]
        public IEnumerator Play_WaitStep_CompletesAfterDuration()
        {
            var sequence = ScriptableObject.CreateInstance<CinematicSequence>();
            sequence.steps = new[]
            {
                new CinematicStep { type = CinematicStepType.Wait, duration = 0.1f, waitForCompletion = true }
            };
            bool completed = false;
            _sequencer.OnSequenceComplete.AddListener(() => completed = true);

            _sequencer.Play(sequence);
            Assert.IsFalse(completed);

            yield return new WaitForSecondsRealtime(0.2f);

            Assert.IsTrue(completed);
            Object.Destroy(sequence);
        }

        [UnityTest]
        public IEnumerator Play_PlayerControlSteps_TogglesMovement()
        {
            var playerGo = new GameObject("Player");
            var pm = playerGo.AddComponent<PlayerMovement>();
            playerGo.AddComponent<CharacterController>();
            pm.enabled = true;

            var field = typeof(CinematicSequencer).GetField("_playerMovement",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_sequencer, pm);

            var sequence = ScriptableObject.CreateInstance<CinematicSequence>();
            sequence.steps = new[]
            {
                new CinematicStep { type = CinematicStepType.DisablePlayerControl },
                new CinematicStep { type = CinematicStepType.Wait, duration = 0.05f, waitForCompletion = true },
                new CinematicStep { type = CinematicStepType.EnablePlayerControl }
            };

            _sequencer.Play(sequence);
            yield return null;

            Assert.IsFalse(pm.enabled, "Should be disabled after DisablePlayerControl");

            yield return new WaitForSecondsRealtime(0.1f);

            Assert.IsTrue(pm.enabled, "Should be re-enabled after EnablePlayerControl");

            Object.Destroy(sequence);
            Object.Destroy(playerGo);
        }

        [UnityTest]
        public IEnumerator Skip_FiresOnSequenceComplete()
        {
            var sequence = ScriptableObject.CreateInstance<CinematicSequence>();
            sequence.steps = new[]
            {
                new CinematicStep { type = CinematicStepType.Wait, duration = 10f, waitForCompletion = true }
            };
            bool completed = false;
            _sequencer.OnSequenceComplete.AddListener(() => completed = true);

            _sequencer.Play(sequence);
            yield return null;

            _sequencer.Skip();

            Assert.IsTrue(completed);
            Assert.IsFalse(_sequencer.IsPlaying);
            Object.Destroy(sequence);
        }

        [UnityTest]
        public IEnumerator Pause_HaltsExecution_ResumeResumes()
        {
            var sequence = ScriptableObject.CreateInstance<CinematicSequence>();
            sequence.steps = new[]
            {
                new CinematicStep { type = CinematicStepType.Wait, duration = 0.05f, waitForCompletion = true },
                new CinematicStep { type = CinematicStepType.Wait, duration = 0.05f, waitForCompletion = true }
            };
            bool completed = false;
            _sequencer.OnSequenceComplete.AddListener(() => completed = true);

            _sequencer.Play(sequence);
            yield return null;

            _sequencer.Pause();
            Assert.IsTrue(_sequencer.IsPaused);

            yield return new WaitForSecondsRealtime(0.2f);
            Assert.IsFalse(completed, "Should NOT complete while paused");

            _sequencer.Resume();
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.IsTrue(completed, "Should complete after resume");

            Object.Destroy(sequence);
        }
    }
}
