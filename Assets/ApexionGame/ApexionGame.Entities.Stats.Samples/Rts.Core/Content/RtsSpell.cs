using System;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// A spell: a set of stat terms applied to every unit inside a circle, for a while.
    /// </summary>
    /// <remarks>
    /// <b>The runtime has no concept of duration</b> — a modifier lives until something removes it. So
    /// everything past <see cref="Terms"/> here is game state, and <see cref="RtsEffectSystem"/> is what
    /// turns it into a timed buff.
    /// <para>
    /// The term array is private and only handed out as a span, so a spell cannot be re-tuned at runtime by
    /// whoever happens to hold a reference to it.
    /// </para>
    /// </remarks>
    public sealed class RtsSpell
    {
        private static readonly RtsSpellTerm[] NoTerms = Array.Empty<RtsSpellTerm>();

        private readonly RtsSpellTerm[] _terms;

        public RtsSpell(
              string name
            , string hotkey
            , RtsSpellTarget target
            , float radius
            , float cooldown
            , int supplyCost
            , float duration = 0f
            , RtsSpellTerm[] terms = null
            , float damagePerTick = 0f
            , float tickInterval = 1f
            , float instantHeal = 0f
        )
        {
            Name = name;
            Hotkey = hotkey;
            Target = target;
            Radius = radius;
            Cooldown = cooldown;
            SupplyCost = supplyCost;
            Duration = duration;
            DamagePerTick = damagePerTick;
            TickInterval = tickInterval;
            InstantHeal = instantHeal;

            _terms = terms ?? NoTerms;
        }

        public string Name { get; }

        /// <summary>Shown on the button, and the keyboard hint.</summary>
        public string Hotkey { get; }

        public RtsSpellTarget Target { get; }

        /// <summary>Radius around the cast point, in metres.</summary>
        public float Radius { get; }

        public float Duration { get; }

        public float Cooldown { get; }

        public int SupplyCost { get; }

        /// <summary>One modifier per term, per affected unit.</summary>
        public ReadOnlySpan<RtsSpellTerm> Terms => _terms;

        public float DamagePerTick { get; }

        public float TickInterval { get; }

        /// <summary>
        /// Health restored on cast, written straight into the <c>Hp</c> base value.
        /// </summary>
        /// <remarks>
        /// No clamping code anywhere: every unit carries a <c>ClampMaxFromStat</c> against its own
        /// <c>MaxHp</c>, so over-healing is the graph's problem, not the spell's.
        /// </remarks>
        public float InstantHeal { get; }

        public bool IsDebuff => Target == RtsSpellTarget.Enemies;

        public bool IsDamageOverTime => DamagePerTick > 0f && TickInterval > 0f;

        /// <summary>True when the spell leaves nothing behind that has to expire.</summary>
        public bool IsInstant => _terms.Length == 0 && IsDamageOverTime == false;

        public string TermsText => _terms.Length == 0 ? string.Empty : string.Join(", ", _terms);

        public override string ToString() => Name;
    }
}
