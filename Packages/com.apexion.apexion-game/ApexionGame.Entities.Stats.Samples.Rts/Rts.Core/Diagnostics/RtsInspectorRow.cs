namespace ApexionGame.Entities.Stats.Samples.Rts
{
    public enum RtsRowKind : byte
    {
        /// <summary>A heading.</summary>
        Section,

        /// <summary>One stat: base value and current value.</summary>
        Stat,

        /// <summary>One modifier under the stat above it — where that current value came from.</summary>
        Modifier,

        /// <summary>One timed effect, and how long it has left.</summary>
        Effect,

        /// <summary>A plain label/value line.</summary>
        Note,
    }

    /// <summary>
    /// One line of the inspector, read straight out of the store.
    /// </summary>
    /// <remarks>
    /// Text rather than numbers here, because a modifier row is genuinely a description ("+ 25% of
    /// Team A/node.AttackBonus") rather than a value. Everything else in the sample keeps formatting out of
    /// the simulation; this is the one place where the string <i>is</i> the data being reported.
    /// </remarks>
    public readonly record struct RtsInspectorRow(RtsRowKind Kind, string Label, string Value)
    {
        public static RtsInspectorRow Section(string label) => new(RtsRowKind.Section, label, string.Empty);

        public static RtsInspectorRow Note(string label, string value) => new(RtsRowKind.Note, label, value);
    }
}
