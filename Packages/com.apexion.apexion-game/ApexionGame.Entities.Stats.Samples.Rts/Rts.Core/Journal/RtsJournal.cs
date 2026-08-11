namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// A bounded record of what the match did, with one named method per kind of news.
    /// </summary>
    /// <remarks>
    /// The named writers are the point: a system says <c>journal.ResearchBought(...)</c> and does not know or
    /// care what the entry looks like, let alone how it will be worded. Backed by a fixed ring buffer, so a
    /// long match neither grows the log forever nor allocates while writing to it.
    /// </remarks>
    public sealed class RtsJournal
    {
        private readonly RtsJournalEntry[] _entries;

        private int _start;
        private int _count;

        public RtsJournal(int capacity = 128)
        {
            _entries = new RtsJournalEntry[capacity < 8 ? 8 : capacity];
        }

        /// <summary>The match clock, so the writers below do not all take a time argument.</summary>
        public float Now { get; set; }

        public int Count => _count;

        /// <summary>Bumped on every write, so a view can redraw only when something happened.</summary>
        public int Version { get; private set; }

        /// <summary>Index 0 is the oldest entry still kept.</summary>
        public RtsJournalEntry this[int index] => _entries[(_start + index) % _entries.Length];

        public void Clear()
        {
            _start = 0;
            _count = 0;
            Version++;
        }

        // ---- news --------------------------------------------------------------------------------

        public void MatchStarted(int owners, int units)
            => Add(new RtsJournalEntry(Now, RtsJournalKind.MatchStarted, Count: owners, Secondary: units));

        public void GraphNote(string subject, int count = 0, int secondary = 0)
            => Add(new RtsJournalEntry(Now, RtsJournalKind.GraphNote, Subject: subject, Count: count
                , Secondary: secondary));

        public void UnitSpawned(int teamIndex, string archetype)
            => Add(new RtsJournalEntry(Now, RtsJournalKind.UnitSpawned, teamIndex, archetype));

        /// <summary>A hero took the field: <paramref name="terms"/> aura modifiers on the team node.</summary>
        public void HeroArrived(int teamIndex, string hero, int terms, int statsChanged)
            => Add(new RtsJournalEntry(Now, RtsJournalKind.HeroArrived, teamIndex, hero, terms, statsChanged));

        public void ResearchBought(
              int teamIndex
            , string research
            , int level
            , int statsChanged
            , float armyAttackBefore
            , float armyAttackAfter
        )
            => Add(new RtsJournalEntry(Now, RtsJournalKind.ResearchBought, teamIndex, research, level
                , statsChanged, armyAttackBefore, armyAttackAfter));

        public void SpellCast(
              int teamIndex
            , string spell
            , int unitsAffected
            , int modifiersAdded
            , int refreshed
            , int statsChanged
        )
            => Add(new RtsJournalEntry(Now, RtsJournalKind.SpellCast, teamIndex, spell, unitsAffected
                , modifiersAdded, refreshed, statsChanged));

        /// <summary>An effect ran out: how many units, and how many modifiers came back.</summary>
        public void EffectExpired(string spell, int units, int returned, int total)
            => Add(new RtsJournalEntry(Now, RtsJournalKind.EffectExpired, Subject: spell, Count: units
                , Secondary: returned, Before: total));

        public void HeroLevelled(
              int teamIndex
            , string hero
            , int level
            , int statsChanged
            , float auraBefore
            , float auraAfter
        )
            => Add(new RtsJournalEntry(Now, RtsJournalKind.HeroLevelled, teamIndex, hero, level, statsChanged
                , auraBefore, auraAfter));

        public void HeroFell(int teamIndex, string hero, float armyAttackBefore, float armyAttackAfter)
            => Add(new RtsJournalEntry(Now, RtsJournalKind.HeroFell, teamIndex, hero, Before: armyAttackBefore
                , After: armyAttackAfter));

        public void UnitDied(int teamIndex, string unit, int modifiersLostWithIt)
            => Add(new RtsJournalEntry(Now, RtsJournalKind.UnitDied, teamIndex, unit, modifiersLostWithIt));

        public void UnitsCulled(int teamIndex, int count, bool sloppy)
            => Add(new RtsJournalEntry(Now, RtsJournalKind.UnitsCulled, teamIndex, Count: count
                , Secondary: sloppy ? 1 : 0));

        public void StrongholdFell(int teamIndex, int winnerTeam)
            => Add(new RtsJournalEntry(Now, RtsJournalKind.StrongholdFell, teamIndex, Count: winnerTeam));

        public void CycleRefused(int teamIndex, string hero, bool accepted)
            => Add(new RtsJournalEntry(Now, RtsJournalKind.CycleRefused, teamIndex, hero
                , Count: accepted ? 1 : 0));

        public void ModifiersPruned(int removed, int reported)
            => Add(new RtsJournalEntry(Now, RtsJournalKind.ModifiersPruned, Count: removed
                , Secondary: reported));

        public void Recalculated(int stats)
            => Add(new RtsJournalEntry(Now, RtsJournalKind.Recalculated, Count: stats));

        public void SloppyDeathsToggled(bool on)
            => Add(new RtsJournalEntry(Now, RtsJournalKind.SloppyDeathsToggled, Count: on ? 1 : 0));

        public void ModifierRefused(int teamIndex, string subject)
            => Add(new RtsJournalEntry(Now, RtsJournalKind.ModifierRefused, teamIndex, subject));

        private void Add(in RtsJournalEntry entry)
        {
            if (_count < _entries.Length)
            {
                _entries[(_start + _count) % _entries.Length] = entry;
                _count++;
            }
            else
            {
                _entries[_start] = entry;
                _start = (_start + 1) % _entries.Length;
            }

            Version++;
        }
    }
}
