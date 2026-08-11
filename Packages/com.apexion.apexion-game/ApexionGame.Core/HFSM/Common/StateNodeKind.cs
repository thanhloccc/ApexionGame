namespace ApexionGame.HFSM
{
    /// <summary>
    /// What a node does with its children.
    /// </summary>
    public enum StateNodeKind : byte
    {
        /// <summary>
        /// No children. The end of an active path.
        /// </summary>
        Leaf = 0,

        /// <summary>
        /// Exactly one child is active at a time, chosen by <c>Initial</c> or by history.
        /// </summary>
        Composite = 1,

        /// <summary>
        /// Every child is active at once. Children of a parallel node are always
        /// <see cref="Region"/>s.
        /// </summary>
        Parallel = 2,

        /// <summary>
        /// One branch of a <see cref="Parallel"/> node. Behaves as a <see cref="Composite"/>, but
        /// its lifetime is tied to its parallel parent rather than to a transition.
        /// </summary>
        Region = 3,
    }
}
