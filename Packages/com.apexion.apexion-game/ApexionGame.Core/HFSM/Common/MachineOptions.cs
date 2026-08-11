namespace ApexionGame.HFSM
{
    /// <summary>
    /// Everything about an instance that is not its context.
    /// </summary>
    /// <remarks>
    /// <see langword="default"/> is the intended configuration: ticked by
    /// <see cref="MachineRunner.Default"/>, cancelling an in-flight async transition when a new one
    /// is selected, and started on creation.
    /// </remarks>
    public readonly struct MachineOptions
    {
        /// <summary>
        /// Who calls <c>Tick</c>. Defaults to <see cref="HFSM.TickMode.Runner"/>.
        /// </summary>
        public TickMode TickMode { get; init; }

        /// <summary>
        /// What a transition selected mid-flight does. Defaults to
        /// <see cref="HFSM.AsyncPolicy.CancelAndReplace"/>.
        /// </summary>
        public AsyncPolicy AsyncPolicy { get; init; }

        /// <summary>
        /// The runner to register with. <see langword="null"/> means
        /// <see cref="MachineRunner.Default"/>. Ignored when <see cref="TickMode"/> is
        /// <see cref="HFSM.TickMode.Manual"/>.
        /// </summary>
        public MachineRunner Runner { get; init; }

        /// <summary>
        /// The label shown in the debugger. <see langword="null"/> means
        /// <c>"{DefinitionName} #{id}"</c>.
        /// </summary>
        /// <remarks>
        /// Must be stable for the lifetime of the instance. A label carrying a live number makes
        /// the debugger's machine selection reset every time that number changes.
        /// </remarks>
        public string DebugName { get; init; }

        /// <summary>
        /// When <see langword="true"/> (the meaning of <see langword="default"/>), the machine
        /// enters its initial configuration as soon as it is created.
        /// </summary>
        /// <remarks>
        /// Stored inverted so that <see langword="default"/> means "start on create" without the
        /// caller setting anything.
        /// </remarks>
        public bool DeferStart { get; init; }
    }
}
