using System;

namespace FarmSimVR.Core.GameState
{
    public class GameStateMachine
    {
        private readonly GameEventBus _eventBus;

        public GameStateMachine(GameEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            CurrentState = GameSessionState.Loading;
        }

        public GameSessionState CurrentState { get; private set; }
        public bool IsPaused => CurrentState == GameSessionState.Paused;

        public void CompleteLoading()
        {
            EnsureState(GameSessionState.Loading, "complete loading");
            TransitionTo(GameSessionState.Playing);
        }

        public void Pause()
        {
            EnsureState(GameSessionState.Playing, "pause");
            TransitionTo(GameSessionState.Paused);
        }

        public void Resume()
        {
            EnsureState(GameSessionState.Paused, "resume");
            TransitionTo(GameSessionState.Playing);
        }

        public void EnterGameOver()
        {
            if (CurrentState == GameSessionState.GameOver)
                return;

            if (CurrentState == GameSessionState.Loading)
                throw new InvalidOperationException("Cannot enter game over while still loading.");

            TransitionTo(GameSessionState.GameOver);
        }

        private void EnsureState(GameSessionState expectedState, string action)
        {
            if (CurrentState != expectedState)
            {
                throw new InvalidOperationException(
                    $"Cannot {action} while in state {CurrentState}.");
            }
        }

        private void TransitionTo(GameSessionState nextState)
        {
            var previousState = CurrentState;
            CurrentState = nextState;
            _eventBus.Publish(new GameStateChangedEvent(previousState, nextState));
        }
    }
}
