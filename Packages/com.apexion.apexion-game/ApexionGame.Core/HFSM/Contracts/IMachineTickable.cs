namespace ApexionGame.HFSM
{
    /// <summary>
    /// A machine seen without its type parameters, so one runner can hold machines of every shape
    /// in a single list.
    /// </summary>
    public interface IMachineTickable
    {
        /// <summary>
        /// Identifies the machine in logs and in the debugger.
        /// </summary>
        string DebugName { get; }

        bool IsRunning { get; }

        void Tick(float deltaTime);
    }
}
