namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// A kind of unit: one set of numbers.
    /// </summary>
    /// <remarks>
    /// Footmen, heroes and strongholds are all built from the same <see cref="UnitStats"/> collection — the
    /// difference between them is this record, plus (for a hero) one modifier on the team node. Immutable,
    /// so nothing can tune a unit type at runtime by accident.
    /// </remarks>
    public sealed record RtsUnitArchetype(
          string Name
        , RtsUnitRole Role
        , float MaxHp
        , float Attack
        , float Armor
        , float MoveSpeed
        , float AttackInterval
        , float AttackRange
        , int SupplyCost
        , RtsAura Aura = default
        , float Radius = 0.45f
        , float Height = 1.4f
    )
    {
        public bool IsHero => Role == RtsUnitRole.Hero;

        public bool IsStructure => Role == RtsUnitRole.Stronghold;

        /// <summary>True when spawning this unit hangs aura terms on its team's node.</summary>
        public bool HasAura => Aura.HasTerms;

        public override string ToString() => Name;
    }
}
