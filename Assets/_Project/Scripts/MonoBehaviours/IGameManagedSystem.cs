namespace FarmSimVR.MonoBehaviours
{
    public interface IGameManagedSystem
    {
        string SystemName { get; }
        bool IsInitialized { get; }
        void InitializeSystem();
        void SetPaused(bool isPaused);
    }
}
