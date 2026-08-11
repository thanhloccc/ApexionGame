using System.Runtime.CompilerServices;

namespace ApexionGame.HFSM
{
    /// <summary>
    /// One node of a baked machine. Immutable after <c>Build</c>.
    /// </summary>
    /// <remarks>
    /// Everything the tick path needs about a node's shape is here, so walking the tree never
    /// chases a reference. <see cref="Depth"/> in particular is baked precisely so
    /// <c>LowestCommonAncestor</c> is a loop over depths instead of a search.
    /// </remarks>
    public readonly struct StateNode
    {
        /// <summary>Index of the parent node, or −1 for the root.</summary>
        public readonly int Parent;

        /// <summary>Index of the first child. Children are contiguous. −1 when there are none.</summary>
        public readonly int FirstChild;

        public readonly int ChildCount;

        /// <summary>The child entered when no history applies. −1 for a leaf.</summary>
        public readonly int InitialChild;

        /// <summary>Start of this node's slice of the definition's transition array.</summary>
        public readonly int TransitionStart;

        public readonly int TransitionCount;

        /// <summary>Byte offset of this node's slot in the instance's state-data blob. −1 if none.</summary>
        public readonly int StateDataOffset;

        /// <summary>Index into the definition's behaviour array. −1 when the node has no behaviour.</summary>
        public readonly int BehaviourIndex;

        /// <summary>Distance from the root, which is depth 0.</summary>
        public readonly ushort Depth;

        public readonly StateNodeKind Kind;

        public readonly HistoryMode History;

        public StateNode(
              int parent
            , int firstChild
            , int childCount
            , int initialChild
            , int transitionStart
            , int transitionCount
            , int stateDataOffset
            , int behaviourIndex
            , ushort depth
            , StateNodeKind kind
            , HistoryMode history
        )
        {
            Parent = parent;
            FirstChild = firstChild;
            ChildCount = childCount;
            InitialChild = initialChild;
            TransitionStart = transitionStart;
            TransitionCount = transitionCount;
            StateDataOffset = stateDataOffset;
            BehaviourIndex = behaviourIndex;
            Depth = depth;
            Kind = kind;
            History = history;
        }

        public bool IsLeaf
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind == StateNodeKind.Leaf;
        }

        /// <summary>
        /// True for the two kinds that keep exactly one active child, and therefore participate in
        /// history.
        /// </summary>
        public bool IsSingleChildContainer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Kind is StateNodeKind.Composite or StateNodeKind.Region;
        }
    }
}
