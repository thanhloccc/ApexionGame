using System.Runtime.CompilerServices;
using EncosyTower.PolyEnumStructs;
using Unity.Collections;

namespace ApexionGame.HFSM
{
    /// <summary>
    /// Every way building or driving a <see cref="HierarchicalStateMachine{TContext, TState}"/> can
    /// fail, with the context needed to act on it.
    /// </summary>
    /// <remarks>
    /// A case union rather than a flat enum, so a failure carries the node, the trigger or the
    /// ordinal that caused it. Call sites use the generated factories with parentheses —
    /// <c>MachineError.UnknownState(ordinal)</c>.
    /// </remarks>
    [PolyEnumFactoryFor(typeof(Error))]
    public readonly partial struct MachineError
    {
        private readonly FixedString64Bytes _prefix;
        private readonly Error _error;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MachineError(in Error error) : this(error, default)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MachineError(in Error error, in FixedString64Bytes prefix)
        {
            _prefix = prefix;
            _error = error;
        }

        /// <summary>
        /// Tags the error with the operation that produced it, for when the message alone is
        /// ambiguous about where it came from.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MachineError Prefix(in FixedString64Bytes prefix)
            => new(_error, prefix);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FixedString512Bytes ToFixedString()
            => _error.ToMessage(_prefix);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
            => _error.ToMessage(_prefix).ToString();

        [PolyEnumStruct]
        readonly partial struct Error
        {
            private static FixedString512Bytes Init(in FixedString64Bytes prefix)
            {
                FixedString512Bytes fs = default;

                if (prefix.IsEmpty == false)
                {
                    fs.Append('[');
                    fs.Append(prefix);
                    fs.Append(']');
                    fs.Append(' ');
                }

                return fs;
            }

            partial interface IEnumCase
            {
                FixedString512Bytes ToMessage(in FixedString64Bytes prefix);
            }

            /// <summary>
            /// The value of a default-initialized <see cref="MachineError"/>.
            /// </summary>
            public readonly partial struct Undefined
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"An unknown state machine error has occurred.");
                    return fs;
                }
            }

            /// <summary>
            /// A machine was built with no states at all.
            /// </summary>
            public readonly partial struct EmptyMachine
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The machine declares no states.");
                    return fs;
                }
            }

            /// <summary>
            /// A transition names a state that was never declared as a node.
            /// </summary>
            /// <remarks>
            /// Carries the enum ordinal, not a <see cref="NodeIndex"/>: an undeclared state has no
            /// node, so there is no index to report.
            /// </remarks>
            public readonly partial record struct UnknownState(int StateOrdinal)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The state with ordinal '");
                    fs.Append(StateOrdinal);
                    fs.Append((FixedString128Bytes)"' was never declared as a node.");
                    return fs;
                }
            }

            /// <summary>
            /// The same enum member was declared as a node twice.
            /// </summary>
            public readonly partial record struct DuplicateState(int StateOrdinal)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The state with ordinal '");
                    fs.Append(StateOrdinal);
                    fs.Append((FixedString128Bytes)"' is declared more than once.");
                    return fs;
                }
            }

            /// <summary>
            /// <c>Initial</c> names a node that is not a child of the composite it was called on.
            /// </summary>
            public readonly partial record struct InitialChildNotAChild(NodeIndex Parent, NodeIndex Child)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"Node '");
                    fs.Append(Child.ToFixedString());
                    fs.Append((FixedString128Bytes)"' is not a child of composite '");
                    fs.Append(Parent.ToFixedString());
                    fs.Append((FixedString128Bytes)"' and cannot be its Initial child.");
                    return fs;
                }
            }

            /// <summary>
            /// A composite, region or parallel node was declared with no children, so entering it
            /// can never reach a leaf.
            /// </summary>
            public readonly partial record struct EmptyComposite(NodeIndex Node)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The container node '");
                    fs.Append(Node.ToFixedString());
                    fs.Append((FixedString128Bytes)"' has no children.");
                    return fs;
                }
            }

            /// <summary>
            /// A <c>Composite</c>/<c>Parallel</c>/<c>Region</c> scope was left open, or an
            /// <c>End*</c> call had no matching scope.
            /// </summary>
            public readonly partial record struct UnbalancedScope(NodeIndex Node, int Depth)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"Unbalanced builder scope at node '");
                    fs.Append(Node.ToFixedString());
                    fs.Append((FixedString128Bytes)"', depth ");
                    fs.Append(Depth);
                    fs.Append((FixedString64Bytes)". Check EndComposite/EndRegion/EndParallel.");
                    return fs;
                }
            }

            /// <summary>
            /// Source and target sit in two different regions of the same parallel node, which would
            /// mean one region pulling another's leaf out from under it.
            /// </summary>
            public readonly partial record struct TransitionCrossesParallelRegion(NodeIndex Source, NodeIndex Target)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The transition from '");
                    fs.Append(Source.ToFixedString());
                    fs.Append((FixedString64Bytes)"' to '");
                    fs.Append(Target.ToFixedString());
                    fs.Append((FixedString128Bytes)"' crosses two regions of one parallel node.");
                    return fs;
                }
            }

            /// <summary>
            /// A transition modifier (<c>When</c>, <c>On</c>, <c>After</c>, <c>Priority</c>, …) was
            /// called with no open transition — <c>To(...)</c> was never called, or the cursor moved
            /// on to a new node since.
            /// </summary>
            public readonly partial struct NoPendingTransition
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"A transition modifier was called with no open ");
                    fs.Append((FixedString64Bytes)"transition. Call To(...) first.");
                    return fs;
                }
            }

            /// <summary>
            /// <c>Fire</c> or a transition request arrived before <c>Start</c> or after
            /// <c>Dispose</c>.
            /// </summary>
            public readonly partial struct MachineNotRunning
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The machine is not running.");
                    return fs;
                }
            }

            /// <summary>
            /// More triggers were queued for one instance than the queue holds.
            /// </summary>
            public readonly partial record struct TriggerQueueFull(TriggerId Trigger, int Capacity)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The trigger queue is full at ");
                    fs.Append(Capacity);
                    fs.Append((FixedString64Bytes)" entries; dropped ");
                    fs.Append(Trigger.ToFixedString());
                    fs.Append('.');
                    return fs;
                }
            }

            /// <summary>
            /// A transition was requested while an async chain was in flight and the instance's
            /// policy is <see cref="AsyncPolicy.Ignore"/>.
            /// </summary>
            public readonly partial record struct TransitionInFlight(NodeIndex Source, NodeIndex Target)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"A transition from '");
                    fs.Append(Source.ToFixedString());
                    fs.Append((FixedString64Bytes)"' to '");
                    fs.Append(Target.ToFixedString());
                    fs.Append((FixedString128Bytes)"' is already in flight.");
                    return fs;
                }
            }
        }
    }
}
