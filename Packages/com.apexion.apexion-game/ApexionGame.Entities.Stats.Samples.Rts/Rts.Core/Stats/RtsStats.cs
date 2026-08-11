namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// One faction's shared bonuses — the single most important idea in this sample.
    /// </summary>
    /// <remarks>
    /// Every unit on a team carries exactly one modifier per stat, reading this node. Two very different
    /// things feed it:
    /// <list type="bullet">
    /// <item>the <b>base value</b> is research — <c>+3 attack from Weapon Smithing</c>, written once;</item>
    /// <item>the <b>modifiers</b> are hero auras — <c>+25% of the Warchief's AuraPower</c>, added when the
    /// hero spawns and removed when it dies.</item>
    /// </list>
    /// Which means a unit does not care how many sources exist, or whether there are two heroes or none —
    /// including units that spawn <i>after</i> the research finished. Point every unit at N heroes
    /// directly and each spawn costs N modifiers, each death costs N removals, and a hero dying touches
    /// every unit's modifier buffer. Point them at one node and spawn cost is constant, and a hero dying
    /// is a single modifier removal that propagates outward on its own.
    /// <para>
    /// The trade is one extra hop of depth in the graph. At a hundred units that is unmeasurable; the
    /// bookkeeping it removes is not.
    /// </para>
    /// </remarks>
    [StatCollection(typeof(RtsStatSystem), 4100)]
    public partial struct TeamStats
    {
        [StatData(StatVariantType.Float)] public partial struct AttackBonus { }

        [StatData(StatVariantType.Float)] public partial struct ArmorBonus { }

        [StatData(StatVariantType.Float)] public partial struct MoveSpeedBonus { }
    }

    /// <summary>
    /// One unit's sheet. Footmen, archers, heroes and strongholds are all built from this — an archetype
    /// is a set of numbers, not a set of stats.
    /// </summary>
    /// <remarks>
    /// A hero is a unit like any other. What makes it a hero is the modifier it adds to its team's
    /// <see cref="TeamStats"/> node, which lives on the team, not here. A stronghold is a unit whose
    /// <c>MoveSpeed</c> and <c>Attack</c> happen to be zero.
    /// </remarks>
    [StatCollection(typeof(RtsStatSystem), 4200)]
    public partial struct UnitStats
    {
        /// <summary>Current health. Combat writes the base value; the cap is a modifier.</summary>
        [StatData(StatVariantType.Float)] public partial struct Hp { }

        [StatData(StatVariantType.Float)] public partial struct MaxHp { }

        [StatData(StatVariantType.Float)] public partial struct Attack { }

        [StatData(StatVariantType.Float)] public partial struct Armor { }

        [StatData(StatVariantType.Float)] public partial struct MoveSpeed { }

        /// <summary>
        /// Seconds between swings — lower is faster, so a haste buff is <c>Multiply(0.8)</c> and a slow is
        /// <c>Multiply(1.5)</c>.
        /// </summary>
        /// <remarks>
        /// Carries a <c>ClampMin</c>, because combat swings once per interval and stacked haste drives this
        /// towards zero geometrically — which would mean an unbounded number of swings in one tick. Stacked
        /// slows are harmless by comparison; they only make a unit useless.
        /// </remarks>
        [StatData(StatVariantType.Float)] public partial struct AttackInterval { }

        /// <summary>
        /// How far this unit can hit from. Archers reach 7 metres, footmen 1.6.
        /// </summary>
        /// <remarks>
        /// Deliberately the one unit stat with <b>no link to the team node</b>: not every stat has to live
        /// in the graph. It is still a stat rather than a constant on the archetype, so a future
        /// "Eagle Eye" buff is one <c>Add</c> modifier away — but it costs nothing until then.
        /// </remarks>
        [StatData(StatVariantType.Float)] public partial struct AttackRange { }

        /// <summary>
        /// How strong this unit's aura is. Zero for everything that is not a hero.
        /// </summary>
        /// <remarks>
        /// <b>This stat exists to break a cycle</b>, and the reason is worth understanding before copying
        /// the pattern. The obvious way to write a hero aura is "the team's Attack bonus is 25% of the
        /// hero's Attack" — but the hero is a unit, so its Attack already reads the team's Attack bonus.
        /// That closes a loop, and <c>TryAddStatModifier</c> refuses it at insert time: the graph is kept a
        /// DAG so propagation cannot hang.
        /// <para>
        /// Refusal is the good outcome — the alternative is a hang. But it means one of the two edges
        /// silently does not exist, so the fix belongs in the design: the aura reads a stat that is not
        /// downstream of the aura. <c>AuraPower</c> is that stat. Levelling a hero writes it, and the whole
        /// army follows.
        /// </para>
        /// <para>
        /// <i>Try it</i>: the HUD's <c>cycle</c> button attempts the cyclic version and logs the refusal.
        /// </para>
        /// </remarks>
        [StatData(StatVariantType.Float)] public partial struct AuraPower { }
    }
}
