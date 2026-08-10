namespace ApexionGame.HFSM
{
    /// <summary>
    /// What happens when a transition is selected while an async transition is still in flight.
    /// </summary>
    public enum AsyncPolicy : byte
    {
        /// <summary>
        /// Cancel the in-flight chain, exit whatever it already entered, then run the new
        /// transition.
        /// </summary>
        /// <remarks>
        /// The default, because the common async case is a screen or ability transition where the
        /// newest intent is the correct one, and because a queue that silently plays a stale
        /// transition later is the harder bug to find.
        /// </remarks>
        CancelAndReplace = 0,

        /// <summary>
        /// Remember the new target and run it once the current chain settles. Only one is
        /// remembered; a third replaces the second.
        /// </summary>
        Queue = 1,

        /// <summary>
        /// Drop the new transition. Recorded in the transition log as ignored.
        /// </summary>
        Ignore = 2,
    }
}
