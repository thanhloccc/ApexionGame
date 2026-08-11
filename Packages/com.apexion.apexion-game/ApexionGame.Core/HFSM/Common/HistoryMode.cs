namespace ApexionGame.HFSM
{
    /// <summary>
    /// What a composite remembers when it is left, and restores when it is re-entered.
    /// </summary>
    public enum HistoryMode : byte
    {
        /// <summary>
        /// Nothing is remembered. Re-entry always uses the <c>Initial</c> child.
        /// </summary>
        None = 0,

        /// <summary>
        /// The direct child that was active is restored; below it, each node uses its own
        /// <c>Initial</c> unless it declares history of its own.
        /// </summary>
        Shallow = 1,

        /// <summary>
        /// The whole active path below this node is restored, down to the leaf.
        /// </summary>
        Deep = 2,
    }
}
