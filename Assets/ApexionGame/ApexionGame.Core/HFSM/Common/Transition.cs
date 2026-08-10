using System;
using System.Runtime.CompilerServices;

namespace ApexionGame.HFSM
{
    /// <summary>
    /// Modifiers on a baked <see cref="Transition"/>.
    /// </summary>
    [Flags]
    public enum TransitionFlags : byte
    {
        None = 0,

        /// <summary>
        /// Runs its action without exiting or entering anything.
        /// </summary>
        Internal = 1 << 0,

        /// <summary>
        /// Declared with <c>After(seconds)</c>, so its cause is a timer rather than a guard.
        /// </summary>
        Timed = 1 << 1,

        /// <summary>
        /// The guard delegate is a <see cref="GuardWithInfo{TContext}"/> rather than a
        /// <see cref="Guard{TContext}"/>.
        /// </summary>
        GuardTakesInfo = 1 << 2,
    }

    /// <summary>
    /// One baked edge. Immutable after <c>Build</c>.
    /// </summary>
    /// <remarks>
    /// The definition's transition array is sorted by source, then priority descending, then
    /// declaration order — so a node's outgoing transitions are a contiguous slice, already in
    /// evaluation order, and evaluating a node is one linear walk.
    /// </remarks>
    public readonly struct Transition
    {
        /// <summary>Index of the source node, or −1 for an <c>AnyState</c> transition.</summary>
        public readonly int Source;

        public readonly int Target;

        /// <summary>Index into the definition's guard array. −1 when unguarded.</summary>
        public readonly int GuardIndex;

        /// <summary>Index into the definition's action array. −1 when there is no action.</summary>
        public readonly int ActionIndex;

        /// <summary>
        /// The trigger that arms this transition. <see cref="TriggerId.None"/> means it is polled
        /// every tick instead.
        /// </summary>
        public readonly TriggerId Trigger;

        /// <summary>Seconds the source must have been active before this may fire.</summary>
        public readonly float MinDuration;

        public readonly int Priority;

        public readonly TransitionFlags Flags;

        public Transition(
              int source
            , int target
            , int guardIndex
            , int actionIndex
            , TriggerId trigger
            , float minDuration
            , int priority
            , TransitionFlags flags
        )
        {
            Source = source;
            Target = target;
            GuardIndex = guardIndex;
            ActionIndex = actionIndex;
            Trigger = trigger;
            MinDuration = minDuration;
            Priority = priority;
            Flags = flags;
        }

        public bool IsAnyState
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Source < 0;
        }

        public bool IsTriggered
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Trigger.IsValid;
        }

        public bool IsInternal
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Flags & TransitionFlags.Internal) != 0;
        }

        public bool HasGuard
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GuardIndex >= 0;
        }

        /// <summary>
        /// The cause to record in the transition log when this one fires.
        /// </summary>
        public TransitionCause Cause
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsTriggered ? TransitionCause.Trigger
                : HasGuard ? TransitionCause.Guard
                : TransitionCause.Timer;
        }
    }
}
