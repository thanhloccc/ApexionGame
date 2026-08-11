using System.Runtime.CompilerServices;

namespace ApexionGame.HFSM
{
    /// <summary>
    /// What a guard is told about the transition it is deciding.
    /// </summary>
    /// <remarks>
    /// Passed <b>by value</b>, not <c>in</c>. With <c>in</c>, a lambda would need explicit parameter
    /// types — <c>static (EnemyContext c, in GuardInfo i) =&gt; …</c> — and the terseness that makes
    /// the builder readable would be gone. Copying these bytes is cheaper than that.
    /// </remarks>
    public readonly struct GuardInfo
    {
        /// <summary>
        /// The node the transition leaves, or <see cref="NodeIndex.Invalid"/> for an
        /// <c>AnyState</c> transition.
        /// </summary>
        public readonly NodeIndex Source;

        /// <summary>The active leaf this evaluation is being made for.</summary>
        public readonly NodeIndex Leaf;

        /// <summary>The leaf that was active before the current one.</summary>
        public readonly NodeIndex PreviousNode;

        /// <summary>Seconds the source has been active.</summary>
        public readonly float TimeInState;

        /// <summary>Seconds since the machine started.</summary>
        public readonly float TimeInMachine;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GuardInfo(
              NodeIndex source
            , NodeIndex leaf
            , NodeIndex previousNode
            , float timeInState
            , float timeInMachine
        )
        {
            Source = source;
            Leaf = leaf;
            PreviousNode = previousNode;
            TimeInState = timeInState;
            TimeInMachine = timeInMachine;
        }
    }
}
