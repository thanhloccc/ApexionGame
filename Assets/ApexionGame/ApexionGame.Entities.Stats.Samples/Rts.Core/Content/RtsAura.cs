namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// A hero's contribution to its team's shared node, as fractions of its own <c>AuraPower</c>.
    /// </summary>
    /// <remarks>
    /// <see cref="Power"/> is the base value written into <c>UnitStats.AuraPower</c>; the fractions become
    /// one <c>AddFractionOfStat</c> modifier each, on the <b>team node</b> rather than on the hero.
    /// <para>
    /// The aura reads <c>AuraPower</c> and not the hero's <c>Attack</c> on purpose: the hero is a unit, so
    /// its Attack already reads the team's Attack bonus, and pointing the aura at it would close a cycle
    /// the runtime refuses at insert time. See <see cref="UnitStats.AuraPower"/>.
    /// </para>
    /// </remarks>
    public readonly record struct RtsAura(
          float Power
        , float AttackFraction = 0f
        , float ArmorFraction = 0f
        , float MoveSpeedFraction = 0f
    )
    {
        public static RtsAura None => default;

        /// <summary>True when this aura contributes at least one term to the team node.</summary>
        public bool HasTerms => AttackFraction != 0f || ArmorFraction != 0f || MoveSpeedFraction != 0f;

        public RtsAura WithPower(float power) => this with { Power = power };
    }
}
