using System;
using System.Collections.Generic;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// Every number in the game, in one place. Tune here, nowhere else.
    /// </summary>
    /// <remarks>
    /// Immutable all the way down, and handed out as <see cref="IReadOnlyList{T}"/> rather than arrays, so
    /// no caller can quietly re-balance the game from the other side of the project.
    /// </remarks>
    public static class RtsContent
    {
        // ---- units ---------------------------------------------------------------------------------

        public static readonly RtsUnitArchetype Footman = new(
              Name: "Footman"
            , Role: RtsUnitRole.Footman
            , MaxHp: 220f
            , Attack: 14f
            , Armor: 6f
            , MoveSpeed: 3.2f
            , AttackInterval: 1.2f
            , AttackRange: 1.6f
            , SupplyCost: 20
            , Radius: 0.42f
            , Height: 1.3f
        );

        public static readonly RtsUnitArchetype Archer = new(
              Name: "Archer"
            , Role: RtsUnitRole.Archer
            , MaxHp: 130f
            , Attack: 18f
            , Armor: 2f
            , MoveSpeed: 3.6f
            , AttackInterval: 1.5f
            , AttackRange: 7f
            , SupplyCost: 25
            , Radius: 0.36f
            , Height: 1.2f
        );

        public static readonly RtsUnitArchetype Knight = new(
              Name: "Knight"
            , Role: RtsUnitRole.Knight
            , MaxHp: 340f
            , Attack: 26f
            , Armor: 10f
            , MoveSpeed: 4.4f
            , AttackInterval: 1.6f
            , AttackRange: 1.8f
            , SupplyCost: 45
            , Radius: 0.5f
            , Height: 1.6f
        );

        /// <summary>Team A's hero: turns its own levelling into army-wide Attack.</summary>
        public static readonly RtsUnitArchetype Warchief = new(
              Name: "Warchief"
            , Role: RtsUnitRole.Hero
            , MaxHp: 900f
            , Attack: 40f
            , Armor: 14f
            , MoveSpeed: 3.8f
            , AttackInterval: 1.4f
            , AttackRange: 2f
            , SupplyCost: 120
            , Aura: new RtsAura(Power: 60f, AttackFraction: 0.25f)
            , Radius: 0.62f
            , Height: 2f
        );

        /// <summary>Team B's hero: armour and a little speed instead of raw damage.</summary>
        public static readonly RtsUnitArchetype Paladin = new(
              Name: "Paladin"
            , Role: RtsUnitRole.Hero
            , MaxHp: 1000f
            , Attack: 34f
            , Armor: 18f
            , MoveSpeed: 3.6f
            , AttackInterval: 1.5f
            , AttackRange: 2f
            , SupplyCost: 120
            , Aura: new RtsAura(Power: 60f, ArmorFraction: 0.2f, MoveSpeedFraction: 0.02f)
            , Radius: 0.62f
            , Height: 2f
        );

        /// <summary>
        /// The thing you have to destroy to win — and a <see cref="UnitStats"/> owner like everything else.
        /// </summary>
        public static readonly RtsUnitArchetype Stronghold = new(
              Name: "Stronghold"
            , Role: RtsUnitRole.Stronghold
            , MaxHp: 3000f
            , Attack: 0f
            , Armor: 20f
            , MoveSpeed: 0f
            , AttackInterval: 99f
            , AttackRange: 0f
            , SupplyCost: 0
            , Radius: 1.8f
            , Height: 2.6f
        );

        private static readonly RtsUnitArchetype[] s_teamAMenu = { Footman, Archer, Knight, Warchief };
        private static readonly RtsUnitArchetype[] s_teamBMenu = { Footman, Archer, Knight, Paladin };

        /// <summary>What a team can buy, in button order. The last entry is that team's hero.</summary>
        public static IReadOnlyList<RtsUnitArchetype> SpawnMenuOf(int teamIndex)
            => teamIndex == 0 ? s_teamAMenu : s_teamBMenu;

        public static RtsUnitArchetype HeroOf(int teamIndex)
            => teamIndex == 0 ? Warchief : Paladin;

        // ---- research ------------------------------------------------------------------------------

        public static readonly RtsResearch WeaponSmithing = new(
              name: "Weapons"
            , target: TeamStats.Type.AttackBonus
            , amountPerLevel: 3f
            , costs: new[] { 60, 90, 130 }
        );

        public static readonly RtsResearch PlateArmor = new(
              name: "Plating"
            , target: TeamStats.Type.ArmorBonus
            , amountPerLevel: 2f
            , costs: new[] { 60, 90, 130 }
        );

        public static readonly RtsResearch Boots = new(
              name: "Boots"
            , target: TeamStats.Type.MoveSpeedBonus
            , amountPerLevel: 0.4f
            , costs: new[] { 45, 70, 100 }
        );

        private static readonly RtsResearch[] s_researches = { WeaponSmithing, PlateArmor, Boots };

        public static IReadOnlyList<RtsResearch> Researches => s_researches;

        // ---- spells --------------------------------------------------------------------------------

        /// <summary>Flat-out damage buff, and the reason <c>AttackInterval</c> needs a floor.</summary>
        public static readonly RtsSpell Bloodlust = new(
              name: "Bloodlust"
            , hotkey: "1"
            , target: RtsSpellTarget.Allies
            , radius: 5f
            , cooldown: 16f
            , supplyCost: 30
            , duration: 8f
            , terms: new[] {
                RtsSpellTerm.Multiply(UnitStats.Type.Attack, 1.4f),
                RtsSpellTerm.Multiply(UnitStats.Type.AttackInterval, 0.8f),
            }
        );

        public static readonly RtsSpell HolyShield = new(
              name: "Holy Shield"
            , hotkey: "2"
            , target: RtsSpellTarget.Allies
            , radius: 5f
            , cooldown: 16f
            , supplyCost: 25
            , duration: 8f
            , terms: new[] { RtsSpellTerm.Add(UnitStats.Type.Armor, 15f) }
        );

        public static readonly RtsSpell Slow = new(
              name: "Slow"
            , hotkey: "3"
            , target: RtsSpellTarget.Enemies
            , radius: 5f
            , cooldown: 14f
            , supplyCost: 25
            , duration: 6f
            , terms: new[] {
                RtsSpellTerm.Multiply(UnitStats.Type.AttackInterval, 1.5f),
                RtsSpellTerm.Multiply(UnitStats.Type.MoveSpeed, 0.6f),
            }
        );

        public static readonly RtsSpell Weaken = new(
              name: "Weaken"
            , hotkey: "4"
            , target: RtsSpellTarget.Enemies
            , radius: 5f
            , cooldown: 14f
            , supplyCost: 25
            , duration: 6f
            , terms: new[] { RtsSpellTerm.Multiply(UnitStats.Type.Attack, 0.7f) }
        );

        /// <summary>Damage over time: the tick writes <c>Hp</c>, the modifier softens armour.</summary>
        public static readonly RtsSpell Plague = new(
              name: "Plague"
            , hotkey: "5"
            , target: RtsSpellTarget.Enemies
            , radius: 4f
            , cooldown: 20f
            , supplyCost: 35
            , duration: 6f
            , terms: new[] { RtsSpellTerm.Multiply(UnitStats.Type.Armor, 0.8f) }
            , damagePerTick: 12f
            , tickInterval: 1f
        );

        /// <summary>No modifier at all: one write per unit, and <c>ClampMax</c> does the rest.</summary>
        public static readonly RtsSpell Rally = new(
              name: "Rally"
            , hotkey: "6"
            , target: RtsSpellTarget.Allies
            , radius: 5f
            , cooldown: 18f
            , supplyCost: 30
            , instantHeal: 200f
        );

        private static readonly RtsSpell[] s_spells = {
            Bloodlust, HolyShield, Slow, Weaken, Plague, Rally,
        };

        public static IReadOnlyList<RtsSpell> Spells => s_spells;

        /// <summary>Spans for the hot paths that must not allocate an enumerator.</summary>
        public static ReadOnlySpan<RtsSpell> SpellSpan => s_spells;

        public static ReadOnlySpan<RtsResearch> ResearchSpan => s_researches;
    }
}
