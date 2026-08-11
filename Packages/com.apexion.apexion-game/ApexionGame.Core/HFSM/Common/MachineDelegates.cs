namespace ApexionGame.HFSM
{
    // The three delegate shapes the builder accepts. One file rather than three, because a
    // one-line delegate per file is noise; called out here so it does not read as an accident.

    /// <summary>
    /// The terse guard shape. Covers the common case, where the decision is a plain read of the
    /// blackboard.
    /// </summary>
    /// <remarks>
    /// Guards are expected to be <b>pure</b>. The debugger evaluates them again to show live
    /// results, so a guard with side effects will apply them twice while the debugger is open.
    /// Declare them <c>static</c> so no closure is allocated.
    /// </remarks>
    public delegate bool Guard<in TContext>(TContext context)
        where TContext : class;

    /// <summary>
    /// The guard shape for decisions that also need timing or the source node.
    /// </summary>
    /// <inheritdoc cref="Guard{TContext}"/>
    public delegate bool GuardWithInfo<in TContext>(TContext context, GuardInfo info)
        where TContext : class;

    /// <summary>
    /// Runs between the exit chain and the enter chain of a transition, or on its own for an
    /// internal transition.
    /// </summary>
    public delegate void TransitionAction<in TContext>(TContext context)
        where TContext : class;
}
