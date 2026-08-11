using System;
using EncosyTower.Collections;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// One spell, on one unit, for as long as it lasts.
    /// </summary>
    /// <remarks>
    /// The runtime has no concept of duration: a modifier lives until something removes it. This record is
    /// the "something". One per (spell, unit) pair rather than one per cast, which makes recasting on the
    /// same unit a duration refresh instead of a second stack.
    /// </remarks>
    public sealed class RtsEffect
    {
        private readonly FasterList<StatModifierHandle> _modifiers = new(2);

        public readonly RtsSpell Spell;
        public readonly RtsUnit Unit;
        public readonly int CasterTeam;

        public float Remaining;
        public float UntilNextTick;

        public RtsEffect(RtsSpell spell, RtsUnit unit, int casterTeam)
        {
            Spell = spell;
            Unit = unit;
            CasterTeam = casterTeam;
            Remaining = spell.Duration;
            UntilNextTick = spell.TickInterval;
        }

        /// <summary>
        /// One handle per term. <b>Losing these means losing the ability to remove the modifiers</b> — the
        /// handle an insertion returned is the only way back.
        /// </summary>
        public ReadOnlySpan<StatModifierHandle> Modifiers => _modifiers.AsReadOnlySpan();

        public int ModifierCount => _modifiers.Count;

        public void AddModifier(in StatModifierHandle handle) => _modifiers.Add(handle);

        public void Forget(in StatModifierHandle handle) => _modifiers.Remove(handle);

        public void Refresh() => Remaining = Spell.Duration;
    }
}
