namespace ApexionGame.HFSM.Debugging
{
    /// <summary>
    /// One line of a machine's transition history.
    /// </summary>
    /// <remarks>
    /// "What changed" without "why" is the half of the answer nobody needs, so every entry carries
    /// its <see cref="TransitionCause"/> and the index of the transition that produced it — which
    /// resolves back to the guard's source text.
    /// </remarks>
    public readonly struct TransitionLogEntry
    {
        public readonly NodeIndex From;
        public readonly NodeIndex To;
        public readonly TransitionCause Cause;

        /// <summary>
        /// Index into the definition's transition array, or −1 when the machine moved without one
        /// (a start, a reset, or a direct request).
        /// </summary>
        public readonly int TransitionIndex;

        /// <summary>
        /// The trigger involved, valid only when <see cref="Cause"/> is a trigger.
        /// </summary>
        public readonly TriggerId Trigger;

        /// <summary>Seconds the source had been active when it was left.</summary>
        public readonly float TimeInSource;

        public readonly float TimeInMachine;
        public readonly int Frame;

        public TransitionLogEntry(
              NodeIndex from
            , NodeIndex to
            , TransitionCause cause
            , int transitionIndex
            , TriggerId trigger
            , float timeInSource
            , float timeInMachine
            , int frame
        )
        {
            From = from;
            To = to;
            Cause = cause;
            TransitionIndex = transitionIndex;
            Trigger = trigger;
            TimeInSource = timeInSource;
            TimeInMachine = timeInMachine;
            Frame = frame;
        }

        /// <summary>
        /// True for a trigger that was drained but matched nothing, and was therefore dropped.
        /// </summary>
        public bool IsUnmatchedTrigger => Cause == TransitionCause.UnmatchedTrigger;
    }
}
