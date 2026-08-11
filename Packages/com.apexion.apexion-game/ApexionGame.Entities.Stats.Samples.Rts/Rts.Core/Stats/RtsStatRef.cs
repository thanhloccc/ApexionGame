namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// Turns a generated stat <c>Type</c> into a <see cref="StatHandle"/>.
    /// </summary>
    /// <remarks>
    /// The whole sample used to carry hand-written <c>RtsUnitStat</c> / <c>RtsTeamStat</c> enums that
    /// duplicated <see cref="UnitStats.Type"/> and <see cref="TeamStats.Type"/> — two lists to keep in sync,
    /// for nothing. The generated enums are the single source of truth; this file is the only place that
    /// maps them onto handles, and it is the only <c>switch</c> over a stat in the project.
    /// <para>
    /// Where the stat is known at compile time, skip this entirely and use the typed accessor:
    /// <c>handles.GetStatHandleFor&lt;UnitStats.Attack&gt;()</c>.
    /// </para>
    /// </remarks>
    public static class RtsStatRef
    {
        public static StatHandle Of(this in UnitStats.StatHandles handles, UnitStats.Type type)
            => type switch {
                UnitStats.Type.Hp => handles.hp,
                UnitStats.Type.MaxHp => handles.maxHp,
                UnitStats.Type.Attack => handles.attack,
                UnitStats.Type.Armor => handles.armor,
                UnitStats.Type.MoveSpeed => handles.moveSpeed,
                UnitStats.Type.AttackInterval => handles.attackInterval,
                UnitStats.Type.AttackRange => handles.attackRange,
                UnitStats.Type.AuraPower => handles.auraPower,
                _ => StatHandle.Null,
            };

        public static StatHandle Of(this in TeamStats.StatHandles handles, TeamStats.Type type)
            => type switch {
                TeamStats.Type.AttackBonus => handles.attackBonus,
                TeamStats.Type.ArmorBonus => handles.armorBonus,
                TeamStats.Type.MoveSpeedBonus => handles.moveSpeedBonus,
                _ => StatHandle.Null,
            };
    }
}
