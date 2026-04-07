namespace FarmSimVR.Core.GameState
{
    public enum GameSessionState
    {
        Loading,
        Playing,
        Paused,
        GameOver
    }

    public readonly struct GameStateChangedEvent
    {
        public GameStateChangedEvent(GameSessionState previousState, GameSessionState currentState)
        {
            PreviousState = previousState;
            CurrentState = currentState;
        }

        public GameSessionState PreviousState { get; }
        public GameSessionState CurrentState { get; }
    }

    public readonly struct GameSystemInitializedEvent
    {
        public GameSystemInitializedEvent(string systemName, int initializationIndex)
        {
            SystemName = systemName;
            InitializationIndex = initializationIndex;
        }

        public string SystemName { get; }
        public int InitializationIndex { get; }
    }
}
