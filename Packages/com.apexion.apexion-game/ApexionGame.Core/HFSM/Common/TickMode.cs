namespace ApexionGame.HFSM
{
    /// <summary>
    /// Who is responsible for calling <c>Tick</c>.
    /// </summary>
    public enum TickMode : byte
    {
        /// <summary>
        /// The machine registers itself with a <see cref="MachineRunner"/> and is ticked in one
        /// batch pass with every other machine.
        /// </summary>
        Runner = 0,

        /// <summary>
        /// The owner calls <c>Tick</c> itself. Use when the machine must run inside
        /// <c>FixedUpdate</c>, or after some other system in a specific order.
        /// </summary>
        Manual = 1,
    }
}
