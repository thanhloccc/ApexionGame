namespace ApexionGame.HFSM
{
    /// <summary>
    /// Why a transition ran. Recorded on every entry of the transition log, because "what changed"
    /// without "why" is the half of the answer nobody needs.
    /// </summary>
    public enum TransitionCause : byte
    {
        /// <summary>
        /// The machine started, was reset, or descended into a composite's initial child.
        /// </summary>
        Initial = 0,

        /// <summary>
        /// A polled guard returned <see langword="true"/>.
        /// </summary>
        Guard = 1,

        /// <summary>
        /// A queued trigger matched a transition leaving the active configuration.
        /// </summary>
        Trigger = 2,

        /// <summary>
        /// A transition with no guard and no trigger became eligible once its time elapsed.
        /// </summary>
        Timer = 3,

        /// <summary>
        /// Requested directly through <c>RequestTransitionOrError</c>.
        /// </summary>
        Request = 4,

        /// <summary>
        /// A composite restored a remembered child instead of its initial one.
        /// </summary>
        History = 5,

        /// <summary>
        /// A trigger was drained but matched nothing in the active configuration, and was dropped.
        /// </summary>
        UnmatchedTrigger = 6,
    }
}
