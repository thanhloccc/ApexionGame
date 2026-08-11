namespace ApexionGame.HFSM
{
    /// <summary>
    /// One state's behaviour. Derive, override what you need, and hand the type to the builder.
    /// </summary>
    /// <remarks>
    /// <b>A behaviour instance is a flyweight: one instance serves every machine built from the
    /// same definition.</b> It must therefore hold no per-agent field. Put per-agent data either on
    /// the context, or in a <see cref="StateBehaviour{TContext, TData}"/> slot — that is what makes
    /// one definition serve hundreds of agents.
    /// <para>
    /// Not generic over the state enum, so one behaviour class can be reused across machines whose
    /// states are different enums. <see cref="StateInfo"/> carries a <see cref="NodeIndex"/> for the
    /// same reason.
    /// </para>
    /// </remarks>
    public abstract partial class StateBehaviour<TContext>
        where TContext : class
    {
        /// <summary>
        /// Runs when this node becomes active, after its parent's <see cref="OnEnter"/> and before
        /// its child's.
        /// </summary>
        protected virtual void OnEnter(TContext context, in StateInfo info)
        {
        }

        /// <summary>
        /// Runs once per tick while this node is active, after its parent's <see cref="OnUpdate"/>
        /// and before its child's.
        /// </summary>
        /// <remarks>
        /// Never runs in the same tick the node exits: the machine settles on a configuration
        /// before updating it.
        /// </remarks>
        protected virtual void OnUpdate(TContext context, in StateInfo info, float deltaTime)
        {
        }

        /// <summary>
        /// Runs when this node stops being active, after its child's <see cref="OnExit"/> and
        /// before its parent's.
        /// </summary>
        protected virtual void OnExit(TContext context, in StateInfo info)
        {
        }

        /// <summary>
        /// Bytes this behaviour needs in each instance's state-data blob.
        /// </summary>
        internal virtual int DataSize => 0;

        /// <summary>
        /// Alignment its slot must satisfy. An unaligned <see langword="ref"/> to a
        /// <see langword="double"/> is undefined behaviour on some platforms.
        /// </summary>
        internal virtual int DataAlign => 1;

        /// <summary>
        /// Whether transitions through this node have to be driven across several ticks.
        /// </summary>
        internal virtual bool IsAsync => false;

        // The dispatch seam. `internal virtual` rather than `abstract`, so a consumer assembly can
        // derive from this class directly and still cannot reach — or break — the blob contract.

        internal virtual void EnterCore(TContext context, byte[] blob, int offset, in StateInfo info)
            => OnEnter(context, info);

        internal virtual void UpdateCore(
              TContext context
            , byte[] blob
            , int offset
            , in StateInfo info
            , float deltaTime
        )
            => OnUpdate(context, info, deltaTime);

        internal virtual void ExitCore(TContext context, byte[] blob, int offset, in StateInfo info)
            => OnExit(context, info);
    }
}
