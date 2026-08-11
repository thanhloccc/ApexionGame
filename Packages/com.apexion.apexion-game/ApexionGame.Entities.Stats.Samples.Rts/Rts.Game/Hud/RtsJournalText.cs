using UnityEngine;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>
    /// Puts a journal entry into words. The only file in the project that knows how the log sounds.
    /// </summary>
    /// <remarks>
    /// The simulation records numbers — units affected, modifiers returned, stats changed. Wording, plurals and
    /// number formats all live here, which is why no system had to spell "modifier(s)".
    /// </remarks>
    public static class RtsJournalText
    {
        public static string Describe(in RtsJournalEntry entry, RtsMatch match)
        {
            var team = entry.TeamIndex >= 0 ? match.TeamAt(entry.TeamIndex).Name : string.Empty;

            return entry.Kind switch {
                RtsJournalKind.MatchStarted
                    => $"match start — {entry.Count} owners in one store "
                        + $"(2 team nodes + {entry.Secondary} units)",

                RtsJournalKind.GraphNote
                    => entry.Subject,

                RtsJournalKind.UnitSpawned
                    => $"{team}: +1 {entry.Subject}",

                RtsJournalKind.HeroArrived
                    => $"{team}: {entry.Subject} takes command — {Plural(entry.Count, "aura term")} "
                        + $"on the team node, {Plural(entry.Secondary, "stat")} changed for that one modifier",

                RtsJournalKind.ResearchBought
                    => $"{team}: {entry.Subject} level {entry.Count} — one write, "
                        + $"{Plural(entry.Secondary, "stat")} changed"
                        + (entry.Before > 0f
                            ? $" (a unit's attack {entry.Before:0.#} → {entry.After:0.#})"
                            : string.Empty),

                RtsJournalKind.SpellCast
                    => $"{team} casts {entry.Subject} on {Plural(entry.Count, "unit")} — "
                        + $"{Plural(entry.Secondary, "modifier")} added"
                        + (entry.Before > 0f ? $", {(int)entry.Before} refreshed" : string.Empty)
                        + $", {(int)entry.After} stats changed",

                RtsJournalKind.EffectExpired
                    => $"{entry.Subject} expired on {Plural(entry.Count, "unit")}: "
                        + $"{entry.Secondary} of {(int)entry.Before} modifiers given back"
                        + (entry.Secondary < (int)entry.Before ? " (the rest died with their units)" : string.Empty),

                RtsJournalKind.HeroLevelled
                    => $"{entry.Subject} reaches level {entry.Count} — AuraPower {entry.Before:0.#} → "
                        + $"{entry.After:0.#}, one write, {Plural(entry.Secondary, "stat")} changed",

                RtsJournalKind.HeroFell
                    => $"{entry.Subject} falls — army attack {entry.Before:0.#} → {entry.After:0.#} in that "
                        + "propagation, nobody looped over the army",

                RtsJournalKind.UnitDied
                    => $"{team}: {entry.Subject} dies with {Plural(entry.Count, "buff modifier")} still on it",

                RtsJournalKind.UnitsCulled
                    => $"{team}: {Plural(entry.Count, "unit")} removed "
                        + (entry.Secondary == 1
                            ? "the sloppy way — their observer entries stay on the node"
                            : "cleanly — their observer entries went with them"),

                RtsJournalKind.StrongholdFell
                    => $"{team}'s Stronghold falls — {match.TeamAt(entry.Count).Name} wins",

                RtsJournalKind.CycleRefused
                    => entry.Count == 1
                        ? $"team.attackBonus += 25% of {entry.Subject}.attack → ACCEPTED, which is a bug"
                        : $"team.attackBonus += 25% of {entry.Subject}.attack → refused: it would close a "
                            + "cycle, and the store is untouched",

                RtsJournalKind.ModifiersPruned
                    => entry.Secondary == 0
                        ? "no dangling modifiers reported"
                        : $"pruned {entry.Count} of {Plural(entry.Secondary, "dangling modifier")}",

                RtsJournalKind.Recalculated
                    => $"{Plural(entry.Count, "stat")} recalculated through DeferredUpdateStatListJob",

                RtsJournalKind.SloppyDeathsToggled
                    => entry.Count == 1
                        ? "sloppy deaths ON — owners are destroyed without removing their modifiers first"
                        : "sloppy deaths off — deaths clean up after themselves again",

                RtsJournalKind.ModifierRefused
                    => $"{team}: a modifier was refused ({entry.Subject})",

                _ => string.Empty,
            };
        }

        public static Color ColorOf(in RtsJournalEntry entry)
        {
            if (entry.IsWarning)
            {
                return RtsHudTheme.WarnInk;
            }

            if (entry.IsGraphNews)
            {
                return RtsHudTheme.GraphInk;
            }

            return entry.Kind switch {
                RtsJournalKind.SpellCast or RtsJournalKind.EffectExpired => RtsHudTheme.SpellInk,
                RtsJournalKind.UnitDied or RtsJournalKind.StrongholdFell => RtsHudTheme.WarnInk,
                RtsJournalKind.UnitSpawned => RtsHudTheme.InkMuted,
                _ => RtsHudTheme.Ink,
            };
        }

        public static string TimeOf(in RtsJournalEntry entry)
        {
            var minutes = (int)(entry.Time / 60f);
            var seconds = (int)(entry.Time - minutes * 60f);

            return $"{minutes:00}:{seconds:00}";
        }

        private static string Plural(int count, string noun)
            => count == 1 ? $"1 {noun}" : $"{count} {noun}s";
    }
}
