namespace ApexionGame.Entities.Stats.Samples.Rts
{
    public enum RtsSpellTarget : byte
    {
        Allies,
        Enemies,
    }

    public enum RtsTermOp : byte
    {
        Add,
        Multiply,
    }

    /// <summary>
    /// One stat term of a spell, which becomes exactly one modifier per affected unit.
    /// </summary>
    public readonly record struct RtsSpellTerm(UnitStats.Type Stat, RtsTermOp Op, float Amount)
    {
        public static RtsSpellTerm Add(UnitStats.Type stat, float amount)
            => new(stat, RtsTermOp.Add, amount);

        public static RtsSpellTerm Multiply(UnitStats.Type stat, float factor)
            => new(stat, RtsTermOp.Multiply, factor);

        public RtsStatSystem.StatModifier ToModifier()
            => Op == RtsTermOp.Add
                ? RtsStatSystem.StatModifier.Add(Amount)
                : RtsStatSystem.StatModifier.Multiply(Amount);

        public override string ToString()
            => Op == RtsTermOp.Add
                ? $"{Stat} {(Amount >= 0f ? "+" : "")}{Amount:0.##}"
                : $"{Stat} ×{Amount:0.##}";
    }
}
