namespace ApexionGame.HFSM
{
    /// <summary>
    /// Where a machine is in its own lifecycle.
    /// </summary>
    public enum MachinePhase : byte
    {
        /// <summary>
        /// Before <c>Start</c>, or after <c>Dispose</c>. Nothing ticks.
        /// </summary>
        Stopped = 0,

        /// <summary>
        /// Settled on a configuration. This is where guards are evaluated and states update.
        /// </summary>
        Idle = 1,

        /// <summary>
        /// Awaiting the exit half of an async transition. No state updates.
        /// </summary>
        Exiting = 2,

        /// <summary>
        /// Awaiting the enter half of an async transition. No state updates.
        /// </summary>
        Entering = 3,
    }
}
