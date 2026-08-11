namespace ApexionGame.Entities.Stats.Samples.Rts
{
    public enum RtsJournalKind : byte
    {
        MatchStarted,

        /// <summary>A plain note about the graph itself — the lines this sample exists to produce.</summary>
        GraphNote,

        UnitSpawned,
        HeroArrived,
        ResearchBought,
        SpellCast,
        EffectExpired,
        HeroLevelled,
        HeroFell,
        UnitDied,
        UnitsCulled,
        StrongholdFell,
        CycleRefused,
        ModifiersPruned,
        Recalculated,
        SloppyDeathsToggled,
        ModifierRefused,
    }

    /// <summary>
    /// One thing that happened, as data.
    /// </summary>
    /// <remarks>
    /// Deliberately <b>not</b> a formatted string. The simulation records facts and numbers; turning them
    /// into English is the presentation layer's job (<c>RtsJournalText</c> in the game assembly). That keeps
    /// wording, pluralisation and number formatting out of the systems, and it means the same journal could
    /// drive a different HUD, a test assertion, or a file.
    /// </remarks>
    public readonly record struct RtsJournalEntry(
          float Time
        , RtsJournalKind Kind
        , int TeamIndex = -1
        , string Subject = null
        , int Count = 0
        , int Secondary = 0
        , float Before = 0f
        , float After = 0f
    )
    {
        public bool IsGraphNews
            => Kind is RtsJournalKind.GraphNote
                or RtsJournalKind.ResearchBought
                or RtsJournalKind.HeroLevelled
                or RtsJournalKind.HeroFell
                or RtsJournalKind.HeroArrived
                or RtsJournalKind.CycleRefused
                or RtsJournalKind.ModifiersPruned
                or RtsJournalKind.Recalculated;

        public bool IsWarning
            => Kind is RtsJournalKind.SloppyDeathsToggled or RtsJournalKind.ModifierRefused;
    }
}
