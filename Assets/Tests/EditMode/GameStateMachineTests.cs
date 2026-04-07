using System;
using System.Collections.Generic;
using NUnit.Framework;
using FarmSimVR.Core.GameState;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class GameStateMachineTests
    {
        private GameEventBus _eventBus;
        private GameStateMachine _stateMachine;
        private List<GameStateChangedEvent> _publishedEvents;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new GameEventBus();
            _stateMachine = new GameStateMachine(_eventBus);
            _publishedEvents = new List<GameStateChangedEvent>();
            _eventBus.Subscribe<GameStateChangedEvent>(_publishedEvents.Add);
        }

        [Test]
        public void NewStateMachine_StartsInLoading()
        {
            Assert.AreEqual(GameSessionState.Loading, _stateMachine.CurrentState);
            Assert.IsFalse(_stateMachine.IsPaused);
        }

        [Test]
        public void CompleteLoading_TransitionsToPlaying_AndPublishesEvent()
        {
            _stateMachine.CompleteLoading();

            Assert.AreEqual(GameSessionState.Playing, _stateMachine.CurrentState);
            Assert.That(_publishedEvents, Has.Count.EqualTo(1));
            Assert.AreEqual(GameSessionState.Loading, _publishedEvents[0].PreviousState);
            Assert.AreEqual(GameSessionState.Playing, _publishedEvents[0].CurrentState);
        }

        [Test]
        public void Pause_WhilePlaying_TransitionsToPaused()
        {
            _stateMachine.CompleteLoading();

            _stateMachine.Pause();

            Assert.AreEqual(GameSessionState.Paused, _stateMachine.CurrentState);
            Assert.IsTrue(_stateMachine.IsPaused);
        }

        [Test]
        public void Resume_WhilePaused_TransitionsBackToPlaying()
        {
            _stateMachine.CompleteLoading();
            _stateMachine.Pause();

            _stateMachine.Resume();

            Assert.AreEqual(GameSessionState.Playing, _stateMachine.CurrentState);
            Assert.IsFalse(_stateMachine.IsPaused);
        }

        [Test]
        public void EnterGameOver_FromPaused_TransitionsToGameOver()
        {
            _stateMachine.CompleteLoading();
            _stateMachine.Pause();

            _stateMachine.EnterGameOver();

            Assert.AreEqual(GameSessionState.GameOver, _stateMachine.CurrentState);
        }

        [Test]
        public void Pause_WhileLoading_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => _stateMachine.Pause());
        }

        [Test]
        public void Resume_WhilePlaying_ThrowsInvalidOperationException()
        {
            _stateMachine.CompleteLoading();

            Assert.Throws<InvalidOperationException>(() => _stateMachine.Resume());
        }
    }

    [TestFixture]
    public class GameEventBusTests
    {
        [Test]
        public void Publish_WithMultipleSubscribers_InvokesHandlersInSubscriptionOrder()
        {
            var bus = new GameEventBus();
            var events = new List<string>();

            bus.Subscribe<string>(value => events.Add($"first:{value}"));
            bus.Subscribe<string>(value => events.Add($"second:{value}"));

            bus.Publish("boot");

            CollectionAssert.AreEqual(
                new[] { "first:boot", "second:boot" },
                events);
        }

        [Test]
        public void Unsubscribe_RemovesHandler()
        {
            var bus = new GameEventBus();
            var events = new List<string>();
            Action<string> handler = value => events.Add(value);

            bus.Subscribe(handler);
            bus.Unsubscribe(handler);

            bus.Publish("paused");

            Assert.That(events, Is.Empty);
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNothing()
        {
            var bus = new GameEventBus();

            Assert.DoesNotThrow(() => bus.Publish(new GameSystemInitializedEvent("Simulation", 0)));
        }
    }
}
