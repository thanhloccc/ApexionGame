using System.Collections.Generic;

namespace ApexionGame.HFSM.Debugging
{
    /// <summary>
    /// A live machine seen without its type parameters, so tooling can inspect machines of every
    /// shape through one interface.
    /// </summary>
    /// <remarks>
    /// Every method <b>appends</b> to the caller's list and never clears it, so a polling window can
    /// reuse the same lists on every pass. Same contract as the stats module's
    /// <c>IStatStoreDebug</c>.
    /// </remarks>
    public interface IMachineDebug
    {
        string Name { get; }

        string DefinitionName { get; }

        bool IsAlive { get; }

        MachinePhase Phase { get; }

        int NodeCount { get; }

        float TimeInMachine { get; }

        /// <summary>
        /// Appends every active node, outermost first, one branch per parallel region.
        /// </summary>
        void GetActiveNodes(List<NodeIndex> result);

        /// <summary>
        /// Appends every node's shape and current timing.
        /// </summary>
        void GetNodes(List<NodeDebugInfo> result);

        /// <summary>
        /// Appends the retained transition history, oldest first.
        /// </summary>
        void GetLog(List<TransitionLogEntry> result);

        /// <summary>
        /// Appends every baked transition, for a graph view to draw as an edge. Static per
        /// definition — safe to call once per machine selection rather than on every poll.
        /// </summary>
        void GetTransitions(List<TransitionDebugInfo> result);

        /// <summary>
        /// Appends every transition that could fire from <paramref name="leaf"/> this tick, in
        /// evaluation order — <c>AnyState</c> first, then the leaf's own node up to the root.
        /// </summary>
        /// <remarks>
        /// Evaluates the same guard delegates the tick path calls, so it is read-only only because
        /// guards are expected to be pure — see <see cref="HFSM.Debugging"/> docs §4.2. Never
        /// consumes a queued trigger and never executes a transition.
        /// </remarks>
        void GetOutgoingGuards(NodeIndex leaf, List<GuardDebugInfo> result);

        /// <summary>
        /// A node's display name, for tooling that has an index and needs a label.
        /// </summary>
        string NameOf(NodeIndex node);
    }

    /// <summary>
    /// One node as tooling sees it.
    /// </summary>
    public readonly struct NodeDebugInfo
    {
        public readonly NodeIndex Node;
        public readonly NodeIndex Parent;
        public readonly StateNodeKind Kind;
        public readonly HistoryMode History;
        public readonly ushort Depth;
        public readonly bool IsActive;

        /// <summary>The child this node remembers, or <see cref="NodeIndex.Invalid"/>.</summary>
        public readonly NodeIndex HistoryChild;

        public readonly float TimeInNode;

        public NodeDebugInfo(
              NodeIndex node
            , NodeIndex parent
            , StateNodeKind kind
            , HistoryMode history
            , ushort depth
            , bool isActive
            , NodeIndex historyChild
            , float timeInNode
        )
        {
            Node = node;
            Parent = parent;
            Kind = kind;
            History = history;
            Depth = depth;
            IsActive = isActive;
            HistoryChild = historyChild;
            TimeInNode = timeInNode;
        }
    }

    /// <summary>
    /// Whether one outgoing transition would fire this tick, for the guard inspector.
    /// </summary>
    public enum GuardMarker : byte
    {
        /// <summary>Would fire this tick — the guard is true, or the transition is trigger-armed.</summary>
        Eligible,

        /// <summary>Evaluated; the guard returned false.</summary>
        False,

        /// <summary>Not evaluated this tick — a higher-priority transition already won.</summary>
        NotEvaluated,

        /// <summary><see cref="Transition.MinDuration"/> has not elapsed yet.</summary>
        BlockedByMinDuration,
    }

    /// <summary>
    /// One outgoing transition as the guard inspector sees it: is it eligible, what condition gates
    /// it, and — best-effort — the live value behind that condition.
    /// </summary>
    public readonly struct GuardDebugInfo
    {
        /// <summary>The node the transition leaves, or <see cref="NodeIndex.Invalid"/> for <c>AnyState</c>.</summary>
        public readonly NodeIndex Source;

        public readonly NodeIndex Target;

        public readonly GuardMarker Marker;

        /// <summary>
        /// The guard's source text, <c>"on &lt;Trigger&gt;"</c>, or <c>"after &lt;n&gt;s"</c>.
        /// </summary>
        public readonly string ConditionText;

        /// <summary>
        /// A best-effort <c>"Field = value"</c> read off the guard's blackboard field, or
        /// <see langword="null"/> when the guard's source text did not match the simple
        /// <c>c.&lt;Field&gt; &lt;op&gt; &lt;literal&gt;</c> pattern.
        /// </summary>
        public readonly string LiveValueText;

        /// <summary>Seconds still remaining, valid only when <see cref="Marker"/> is <see cref="GuardMarker.BlockedByMinDuration"/>.</summary>
        public readonly float RemainingMinDuration;

        public GuardDebugInfo(
              NodeIndex source
            , NodeIndex target
            , GuardMarker marker
            , string conditionText
            , string liveValueText
            , float remainingMinDuration
        )
        {
            Source = source;
            Target = target;
            Marker = marker;
            ConditionText = conditionText;
            LiveValueText = liveValueText;
            RemainingMinDuration = remainingMinDuration;
        }

        public bool IsAnyState => Source.IsValid == false;
    }

    /// <summary>
    /// One baked transition, for the graph view — an edge, not an evaluation result.
    /// </summary>
    public readonly struct TransitionDebugInfo
    {
        /// <summary>The node the transition leaves, or <see cref="NodeIndex.Invalid"/> for <c>AnyState</c>.</summary>
        public readonly NodeIndex Source;

        public readonly NodeIndex Target;

        public readonly bool IsTriggered;

        public readonly bool IsTimed;

        public TransitionDebugInfo(NodeIndex source, NodeIndex target, bool isTriggered, bool isTimed)
        {
            Source = source;
            Target = target;
            IsTriggered = isTriggered;
            IsTimed = isTimed;
        }

        public bool IsAnyState => Source.IsValid == false;
    }
}
